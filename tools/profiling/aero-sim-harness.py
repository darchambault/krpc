#!/usr/bin/env python3
"""
Investigation harness for Flight.SimulateAerodynamicForceAt (the
simulate_aerodynamic_force_at endpoint).

This is a pure kRPC *client* script: it needs no server rebuild and uses only
the stock API present on the main branch. It turns the open questions about the
endpoint into runnable measurements so the behaviour can be characterised
against a live KSP + kRPC session.

Background: the endpoint takes (body, position, velocity) but no attitude. The
effective angle of attack is therefore locked to angle(velocity, current nose),
and the velocity argument is converted to air-relative internally (planet
rotation is subtracted). This harness measures the consequences:

  env          Report the vessel/atmosphere state and whether FAR is loaded.
  frames       Frame-consistency + the "air-relative velocity in a non-rotating
               frame" double-subtraction trap (H2). Shows that the correct call
               agrees across reference frames, and how far the common mistake is
               off.
  density      Compare the model density (CelestialBody.density_at, the
               equatorial-average-temperature approximation) to the live
               Flight.atmosphere_density the real vessel sees (H3).
  determinism  Repeatability of identical calls, contamination across calls, and
               whether calling the endpoint mutates observable live state (H4 /
               FAR statefulness).
  sweep        Build the altitude-vs-AoA drag/lift polar by rotating the wind
               vector in the vessel body frame (the ckfinite workaround, H1).
               Optionally written to CSV. This is the lookup-table artefact.
  snapshot     Print the body-frame force + drag/lift for one fixed
               (speed, AoA, sideslip). Run it, manually re-orient the craft, run
               it again, and diff to check absolute-orientation leakage (H1).

The next three automate the KSP side. setup and attitude-sweep need the
TestingTools plugin (tools/TestingTools) loaded in KSP; validate does not.

  setup           Reproducibly place the craft via TestingTools: load a save,
                  set a circular orbit / ballistic arc / landed spot, optionally
                  clear rotation. Run once, then run the experiments.
  attitude-sweep  Automated orientation-leak test (H1): hold a fixed relative
                  AoA while rotating the craft through a series of absolute
                  attitudes, and report whether the body-frame force drifts.
  validate        Ground truth: compare simulate_aerodynamic_force_at at the
                  vessel's real state against the live Flight.aerodynamic_force
                  KSP is actually applying. --descend samples it down an arc.

By default (no subcommand) it runs env, frames, density, determinism and a short
sweep preview. The non-setup experiments work with the craft sitting on the pad
inside an atmosphere; nothing needs the vessel to actually be moving.

Usage:
    tools/profiling/aero-sim-harness.py [--address ADDR] [--rpc-port PORT]
        [--stream-port PORT] [SUBCOMMAND] [options]

    tools/profiling/aero-sim-harness.py sweep --speed 300 \
        --aoa-min -20 --aoa-max 20 --aoa-step 1 --csv polar.csv
    tools/profiling/aero-sim-harness.py snapshot --speed 300 --aoa 5
"""

from __future__ import annotations

import argparse
import math
import sys
import time

import krpc

# --- tiny tuple-based vector helpers (no numpy dependency) -------------------


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def scale(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def mag(a):
    return math.sqrt(dot(a, a))


def normalize(a):
    m = mag(a)
    return (0.0, 0.0, 0.0) if m == 0 else scale(a, 1.0 / m)


def fmt(v):
    return f"({v[0]:+.3e}, {v[1]:+.3e}, {v[2]:+.3e})"


# --- wind construction -------------------------------------------------------
#
# The vessel reference frame is body-fixed to the craft:
#   +x = right, +y = forward (nose / roll axis), +z = down (out the belly).
# Zero-AoA, zero-sideslip relative wind is the craft flying straight along the
# nose, so the air-relative velocity points along +y. Positive AoA pitches the
# velocity toward the belly (+z); positive sideslip yaws it toward -x.


def wind_in_vessel_frame(speed, aoa_deg, sideslip_deg):
    a = math.radians(aoa_deg)
    b = math.radians(sideslip_deg)
    direction = (
        -math.sin(b) * math.cos(a),
        math.cos(b) * math.cos(a),
        math.sin(a),
    )
    return scale(direction, speed)


class Harness:
    def __init__(self, conn):
        self.conn = conn
        self.sc = conn.space_center
        self.vessel = self.sc.active_vessel
        self.body = self.vessel.orbit.body
        self.rf_rot = self.body.reference_frame  # rotates with the body (surface)
        self.rf_nr = self.body.non_rotating_reference_frame
        self.rf_vessel = self.vessel.reference_frame
        self.flight_rot = self.vessel.flight(self.rf_rot)
        self.flight_nr = self.vessel.flight(self.rf_nr)
        self.has_far = self._detect_far()
        # The TestingTools service (HyperEdit-style cheats used by the
        # integration suite) is built dynamically by the client only if its
        # plugin is loaded in KSP. It provides set_orbit / set_circular_orbit /
        # set_landed / apply_rotation / clear_rotation / load_save.
        self.tt = getattr(conn, "testing_tools", None)

    def _detect_far(self):
        # reynolds_number is FAR-only and throws under stock aero.
        try:
            _ = self.vessel.flight(self.rf_rot).reynolds_number
            return True
        except Exception:
            return False

    def _require_testing_tools(self):
        if self.tt is None:
            raise RuntimeError(
                "The TestingTools service is not available. Build "
                "tools/TestingTools and drop the plugin into KSP's GameData, "
                "then restart KSP. Scenario setup needs it; the 'validate' and "
                "pad-based experiments do not."
            )
        return self.tt

    # Core call: force for a relative wind given as (speed, AoA, sideslip),
    # using the *correct* path (air-relative velocity in the rotating frame).
    # Returns the force expressed in the requested frame ("rot", "nr", "vessel").
    def force_for_wind(self, speed, aoa_deg, sideslip_deg, out="vessel"):
        wind_v = wind_in_vessel_frame(speed, aoa_deg, sideslip_deg)
        vel_rot = self.sc.transform_direction(wind_v, self.rf_vessel, self.rf_rot)
        pos_rot = self.vessel.position(self.rf_rot)
        f_rot = self.flight_rot.simulate_aerodynamic_force_at(
            self.body, pos_rot, vel_rot
        )
        if out == "rot":
            return f_rot
        target = {"nr": self.rf_nr, "vessel": self.rf_vessel}[out]
        return self.sc.transform_direction(f_rot, self.rf_rot, target)

    # -- env -----------------------------------------------------------------
    def report_env(self):
        v, b = self.vessel, self.body
        f = self.vessel.flight()  # default surface frame
        print("=== Environment ===")
        print(f"  vessel            {v.name!r} ({len(v.parts.all)} parts)")
        print(f"  body              {b.name}  has_atmosphere={b.has_atmosphere}")
        print(f"  atmosphere_depth  {b.atmosphere_depth:,.0f} m")
        print(f"  mean_altitude     {f.mean_altitude:,.1f} m")
        if b.has_atmosphere and f.mean_altitude > b.atmosphere_depth:
            print("  WARNING: vessel is ABOVE the atmosphere; forces will be ~0.")
        print(f"  aero model        {'FAR' if self.has_far else 'stock'}")
        for label, attr in (
            ("atmosphere_density", "atmosphere_density"),
            ("dynamic_pressure", "dynamic_pressure"),
            ("static_pressure", "static_pressure"),
            ("speed_of_sound", "speed_of_sound"),
            ("mach", "mach"),
        ):
            try:
                print(f"  {label:<18}{getattr(f, attr):,.4f}")
            except Exception as exc:
                print(f"  {label:<18}<unavailable: {exc}>")
        # Surface rotation speed at this position: difference between the
        # orbital velocity and the surface (air-relative) velocity.
        pos_rot = self.vessel.position(self.rf_rot)
        rot_vel = self.sc.transform_velocity(
            pos_rot, (0.0, 0.0, 0.0), self.rf_rot, self.rf_nr
        )
        print(f"  surface rotation  {mag(rot_vel):,.2f} m/s at this position")
        print()

    # -- frames --------------------------------------------------------------
    def test_frames(self, speed):
        print("=== Frame consistency + double-subtraction trap (H2) ===")
        print(f"  test wind: {speed:.0f} m/s straight down the nose (AoA 0)\n")
        wind_v = wind_in_vessel_frame(speed, 0.0, 0.0)
        vel_rot = self.sc.transform_direction(wind_v, self.rf_vessel, self.rf_rot)
        pos_rot = self.vessel.position(self.rf_rot)
        pos_nr = self.vessel.position(self.rf_nr)

        # Correct call A: air-relative velocity in the rotating frame.
        f_rot = self.flight_rot.simulate_aerodynamic_force_at(
            self.body, pos_rot, vel_rot
        )
        f_rot_in_nr = self.sc.transform_direction(f_rot, self.rf_rot, self.rf_nr)

        # Correct call B: orbital velocity in the non-rotating frame. Derive the
        # orbital velocity by transforming the air-relative velocity (adds the
        # planet-rotation contribution).
        vel_nr_orbital = self.sc.transform_velocity(
            pos_rot, vel_rot, self.rf_rot, self.rf_nr
        )
        f_nr = self.flight_nr.simulate_aerodynamic_force_at(
            self.body, pos_nr, vel_nr_orbital
        )

        # Trap: the SAME air-relative velocity, but handed to the non-rotating
        # frame (a common mistake). The endpoint subtracts rotation again.
        vel_nr_wrong = self.sc.transform_direction(vel_rot, self.rf_rot, self.rf_nr)
        f_trap = self.flight_nr.simulate_aerodynamic_force_at(
            self.body, pos_nr, vel_nr_wrong
        )

        rot_speed = mag(sub(vel_nr_orbital, vel_nr_wrong))
        print(f"  A  rotating frame, air-rel vel     |F| = {mag(f_rot):,.1f} N")
        print(f"  B  non-rotating frame, orbital vel  |F| = {mag(f_nr):,.1f} N")
        agree = mag(sub(f_rot_in_nr, f_nr))
        rel = agree / mag(f_nr) * 100 if mag(f_nr) else float("nan")
        print(f"     A vs B disagreement            {agree:,.2f} N ({rel:.2f}%)")
        print(f"        -> should be ~0 if frame handling is consistent\n")
        print(f"  TRAP  air-rel vel in non-rot frame  |F| = {mag(f_trap):,.1f} N")
        trap_err = mag(sub(f_trap, f_rot_in_nr))
        trel = trap_err / mag(f_rot_in_nr) * 100 if mag(f_rot_in_nr) else float("nan")
        print(f"     trap error vs correct          {trap_err:,.1f} N ({trel:.1f}%)")
        print(
            f"     (planet rotation here = {rot_speed:,.1f} m/s vs {speed:.0f} m/s wind)"
        )
        print()

    # -- density -------------------------------------------------------------
    def test_density(self):
        print("=== Model density vs live density (H3) ===")
        f = self.vessel.flight()
        alt = f.mean_altitude
        try:
            model_rho = self.body.density_at(alt)
            live_rho = f.atmosphere_density
            print(f"  altitude           {alt:,.1f} m")
            print(f"  model density_at   {model_rho:.6f} kg/m^3 (equatorial-avg temp)")
            print(f"  live atmosphere    {live_rho:.6f} kg/m^3 (true local)")
            if live_rho > 0:
                print(f"  model / live       {model_rho / live_rho:.4f}")
                print("  (a persistent !=1 ratio is a systematic drag bias)")
            else:
                print("  live density is 0 (vessel not in atmosphere?)")
        except Exception as exc:
            print(f"  <unavailable: {exc}>")
        print()

    # -- determinism ---------------------------------------------------------
    def test_determinism(self, speed, samples):
        print("=== Determinism / contamination / state mutation (H4) ===")
        before = self._live_state()

        baseline = self.force_for_wind(speed, 5.0, 0.0, out="nr")
        repeats = [
            self.force_for_wind(speed, 5.0, 0.0, out="nr") for _ in range(samples)
        ]
        max_dev = max(mag(sub(r, baseline)) for r in repeats) if repeats else 0.0
        print(f"  identical call x{samples}: max deviation {max_dev:.3e} N")
        print("     -> nonzero under FAR would indicate nondeterminism")

        # Contamination: hammer a spread of conditions, then re-measure baseline.
        for aoa in (-15, -5, 0, 10, 20):
            for s in (50, speed, speed * 2):
                self.force_for_wind(s, aoa, 0.0, out="nr")
        after_spread = self.force_for_wind(speed, 5.0, 0.0, out="nr")
        drift = mag(sub(after_spread, baseline))
        print(f"  baseline drift after varied calls: {drift:.3e} N")
        print("     -> nonzero means earlier calls contaminate later ones")

        after = self._live_state()
        print("  live-state mutation across the battery:")
        for k in before:
            if before[k] is None or after[k] is None:
                print(f"     {k:<18}<unavailable>")
            else:
                print(f"     {k:<18}{before[k]:.6f} -> {after[k]:.6f}")
        print()

    def _live_state(self):
        f = self.vessel.flight()
        state = {}
        for k in ("mach", "dynamic_pressure", "atmosphere_density"):
            try:
                state[k] = float(getattr(f, k))
            except Exception:
                state[k] = None
        return state

    # -- sweep ---------------------------------------------------------------
    def sweep(self, speed, aoa_min, aoa_max, aoa_step, sideslip, csv_path):
        print("=== AoA drag/lift polar (H1 workaround / lookup table) ===")
        print(
            f"  speed={speed:.0f} m/s  sideslip={sideslip:.1f} deg  "
            f"AoA {aoa_min:.0f}..{aoa_max:.0f} step {aoa_step:.0f}\n"
        )
        rows = []
        # Inclusive range without float drift surprises.
        n = int(round((aoa_max - aoa_min) / aoa_step)) + 1
        header = "aoa_deg,speed,drag_N,lift_N,F_mag_N,Fx,Fy,Fz"
        print(f"  {header}")
        for i in range(n):
            aoa = aoa_min + i * aoa_step
            f_vessel = self.force_for_wind(speed, aoa, sideslip, out="vessel")
            wind_v = wind_in_vessel_frame(speed, aoa, sideslip)
            v_hat = normalize(wind_v)
            drag = -dot(f_vessel, v_hat)  # positive opposes motion
            lift_vec = sub(f_vessel, scale(v_hat, dot(f_vessel, v_hat)))
            lift = mag(lift_vec)
            row = (
                aoa,
                speed,
                drag,
                lift,
                mag(f_vessel),
                f_vessel[0],
                f_vessel[1],
                f_vessel[2],
            )
            rows.append(row)
            print(
                f"  {aoa:7.1f},{speed:7.0f},{drag:12.1f},{lift:12.1f},"
                f"{mag(f_vessel):12.1f},{f_vessel[0]:12.1f},"
                f"{f_vessel[1]:12.1f},{f_vessel[2]:12.1f}"
            )
        if csv_path:
            with open(csv_path, "w", encoding="utf-8") as fh:
                fh.write(header + "\n")
                for r in rows:
                    fh.write(",".join(f"{x:.6f}" for x in r) + "\n")
            print(f"\n  wrote {len(rows)} rows to {csv_path}")
        print()

    # -- snapshot ------------------------------------------------------------
    def snapshot(self, speed, aoa, sideslip):
        print("=== Snapshot (orientation-leak check, H1) ===")
        nose_nr = self.vessel.direction(self.rf_nr)
        f_vessel = self.force_for_wind(speed, aoa, sideslip, out="vessel")
        wind_v = wind_in_vessel_frame(speed, aoa, sideslip)
        v_hat = normalize(wind_v)
        drag = -dot(f_vessel, v_hat)
        lift = mag(sub(f_vessel, scale(v_hat, dot(f_vessel, v_hat))))
        print(f"  nose dir (non-rot frame)  {fmt(nose_nr)}")
        print(f"  force (vessel frame)      {fmt(f_vessel)}")
        print(f"  drag {drag:,.1f} N   lift {lift:,.1f} N   |F| {mag(f_vessel):,.1f} N")
        print(
            "  Re-run this after manually re-orienting the craft at the SAME\n"
            "  AoA. If the body-frame force changes, absolute orientation is\n"
            "  leaking into the result."
        )
        print()

    # -- setup (TestingTools): reproducible scenario placement ---------------
    def setup_scenario(self, args):
        tt = self._require_testing_tools()
        print("=== Scenario setup (TestingTools) ===")
        if args.load:
            print(f"  loading save {args.load!r}...")
            tt.load_save(args.save_dir, args.load)
            # Active vessel may have changed; rebind everything derived from it.
            self.vessel = self.sc.active_vessel
            self.body = self.vessel.orbit.body
            self.rf_rot = self.body.reference_frame
            self.rf_nr = self.body.non_rotating_reference_frame
            self.rf_vessel = self.vessel.reference_frame
            self.flight_rot = self.vessel.flight(self.rf_rot)
            self.flight_nr = self.vessel.flight(self.rf_nr)
        body_name = self.body.name
        if args.circular is not None:
            print(f"  circular orbit: {body_name} @ {args.circular:,.0f} m MSL")
            tt.set_circular_orbit(body_name, args.circular)
        elif args.apoapsis is not None:
            ra = self.body.equatorial_radius + args.apoapsis
            rp = self.body.equatorial_radius + args.periapsis
            sma = (ra + rp) / 2.0
            ecc = (ra - rp) / (ra + rp)
            epoch = self.sc.ut
            # mean anomaly pi => start at apoapsis, so the craft then descends.
            print(
                f"  ballistic arc: {body_name} apo {args.apoapsis:,.0f} / "
                f"per {args.periapsis:,.0f} m (sma {sma:,.0f}, ecc {ecc:.4f}), "
                f"starting at apoapsis"
            )
            tt.set_orbit(body_name, sma, ecc, 0.0, 0.0, 0.0, math.pi, epoch)
        elif args.landed is not None:
            lat, lon = args.landed
            print(f"  landed at {body_name} lat {lat} lon {lon}")
            tt.set_landed(body_name, lat, lon, 0.0)
        if args.clear_rotation:
            tt.clear_rotation(self.vessel)
            print("  cleared rotational velocity")
        # Let physics settle after teleport before any measurement.
        time.sleep(0.5)
        print("  done.\n")

    # -- attitude-leak sweep (automated H1) ----------------------------------
    # Hold the SAME relative wind (fixed AoA in the vessel frame) while rotating
    # the craft to a series of absolute attitudes via TestingTools.apply_rotation.
    # The wind is rebuilt in the (rotated) vessel frame each time, so the airflow
    # over the body is identical; only absolute orientation changes. If the
    # body-frame force moves, absolute orientation is leaking in (expected for
    # FAR, not for stock, where the only position dependence is density).
    def attitude_leak(self, speed, aoa, axis, steps, step_deg):
        tt = self._require_testing_tools()
        print("=== Automated orientation-leak sweep (H1) ===")
        print(
            f"  wind {speed:.0f} m/s @ AoA {aoa:.1f}; rotating {step_deg:.0f} deg/"
            f"step about vessel axis {axis} x{steps}\n"
        )
        baseline = None
        for i in range(steps + 1):
            f_vessel = self.force_for_wind(speed, aoa, 0.0, out="vessel")
            if baseline is None:
                baseline = f_vessel
            dev = mag(sub(f_vessel, baseline))
            rel = dev / mag(baseline) * 100 if mag(baseline) else float("nan")
            print(
                f"  step {i:2d}  body-frame F = {fmt(f_vessel)}  "
                f"|F| {mag(f_vessel):,.1f} N  drift {dev:,.2f} N ({rel:.2f}%)"
            )
            if i < steps:
                tt.apply_rotation(float(step_deg), axis, self.vessel)
                if hasattr(tt, "clear_rotation"):
                    tt.clear_rotation(self.vessel)
                time.sleep(0.2)
        print("  -> stock: drift ~0 expected. FAR: nonzero exposes state leak.\n")

    # -- validate: simulated force vs live ground-truth force ----------------
    def validate_current(self):
        # Compare the endpoint against the force KSP is ACTUALLY applying right
        # now, at the vessel's real state and orientation. This is the cleanest
        # correctness check and needs no reference mod.
        pos = self.vessel.position(self.rf_rot)
        vel = self.vessel.velocity(self.rf_rot)
        f_sim = self.flight_rot.simulate_aerodynamic_force_at(self.body, pos, vel)
        f_live = self.flight_rot.aerodynamic_force
        err = mag(sub(f_sim, f_live))
        denom = mag(f_live)
        rel = err / denom * 100 if denom else float("nan")
        ang = angle_between(f_sim, f_live)
        alt = self.vessel.flight().mean_altitude
        spd = mag(vel)
        print(
            f"  alt {alt:9,.0f} m  spd {spd:7.1f}  "
            f"|sim| {mag(f_sim):11,.1f}  |live| {denom:11,.1f}  "
            f"err {err:10,.1f} N ({rel:5.1f}%)  ang {ang:5.2f} deg"
        )
        return alt, spd, mag(f_sim), denom, err, rel, ang

    def validate(self, descend, duration, interval, warp, csv_path):
        print("=== Validate: simulated vs live aerodynamic force ===")
        if mag(self.flight_rot.aerodynamic_force) == 0:
            print(
                "  live aerodynamic_force is 0 (vessel out of atmosphere or on\n"
                "  rails). Put the craft in atmosphere first, e.g. setup\n"
                "  --apoapsis 70000 --periapsis -10000.\n"
            )
        rows = []
        if not descend:
            rows.append(self.validate_current())
        else:
            print(f"  sampling every {interval:.1f}s for {duration:.0f}s, warp {warp}x")
            if warp and warp > 1:
                try:
                    self.sc.physics_warp_factor = int(warp) - 1
                except Exception as exc:
                    print(f"  (could not set physics warp: {exc})")
            start = time.time()
            while time.time() - start < duration:
                try:
                    rows.append(self.validate_current())
                except Exception as exc:
                    print(f"  sample failed: {exc}")
                if self.vessel.situation.name in ("landed", "splashed"):
                    print("  vessel landed/splashed; stopping.")
                    break
                time.sleep(interval)
            try:
                self.sc.physics_warp_factor = 0
            except Exception:
                pass
        if csv_path and rows:
            with open(csv_path, "w", encoding="utf-8") as fh:
                fh.write("alt_m,speed,sim_N,live_N,err_N,err_pct,angle_deg\n")
                for r in rows:
                    fh.write(",".join(f"{x:.6f}" for x in r) + "\n")
            print(f"  wrote {len(rows)} samples to {csv_path}")
        print()


def angle_between(a, b):
    na, nb = mag(a), mag(b)
    if na == 0 or nb == 0:
        return float("nan")
    c = max(-1.0, min(1.0, dot(a, b) / (na * nb)))
    return math.degrees(math.acos(c))


def connect(args):
    print(f"Connecting to kRPC at {args.address}:{args.rpc_port}...")
    try:
        conn = krpc.connect(
            name="aero-sim-harness",
            address=args.address,
            rpc_port=args.rpc_port,
            stream_port=args.stream_port,
        )
    except Exception as exc:
        print(f"Failed to connect: {exc}", file=sys.stderr)
        print("Is KSP running with the kRPC server enabled?", file=sys.stderr)
        sys.exit(1)
    try:
        h = Harness(conn)
    except Exception as exc:
        print(f"Could not set up against the active vessel: {exc}", file=sys.stderr)
        print("Load a craft in the flight scene inside an atmosphere.", file=sys.stderr)
        conn.close()
        sys.exit(1)
    return conn, h


def run_section(name, fn):
    try:
        fn()
    except Exception as exc:
        print(f"  [{name}] failed: {exc}\n", file=sys.stderr)


def main():
    p = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    p.add_argument("--address", default="127.0.0.1")
    p.add_argument("--rpc-port", type=int, default=50000)
    p.add_argument("--stream-port", type=int, default=50001)
    sub = p.add_subparsers(dest="cmd")

    sub.add_parser("env")
    sp_frames = sub.add_parser("frames")
    sp_frames.add_argument("--speed", type=float, default=200.0)
    sub.add_parser("density")
    sp_det = sub.add_parser("determinism")
    sp_det.add_argument("--speed", type=float, default=200.0)
    sp_det.add_argument("--samples", type=int, default=10)
    sp_sweep = sub.add_parser("sweep")
    sp_sweep.add_argument("--speed", type=float, default=300.0)
    sp_sweep.add_argument("--aoa-min", type=float, default=-20.0)
    sp_sweep.add_argument("--aoa-max", type=float, default=20.0)
    sp_sweep.add_argument("--aoa-step", type=float, default=2.0)
    sp_sweep.add_argument("--sideslip", type=float, default=0.0)
    sp_sweep.add_argument("--csv", default=None)
    sp_snap = sub.add_parser("snapshot")
    sp_snap.add_argument("--speed", type=float, default=300.0)
    sp_snap.add_argument("--aoa", type=float, default=5.0)
    sp_snap.add_argument("--sideslip", type=float, default=0.0)

    sp_setup = sub.add_parser("setup", help="place the craft (needs TestingTools)")
    sp_setup.add_argument("--load", default=None, help="save name to load first")
    sp_setup.add_argument("--save-dir", default="default", help="save folder")
    sp_setup.add_argument("--circular", type=float, default=None, help="MSL altitude")
    sp_setup.add_argument("--apoapsis", type=float, default=None, help="arc apo (m)")
    sp_setup.add_argument(
        "--periapsis", type=float, default=-10000.0, help="arc per (m, may be <0)"
    )
    sp_setup.add_argument(
        "--landed", type=float, nargs=2, default=None, metavar=("LAT", "LON")
    )
    sp_setup.add_argument("--clear-rotation", action="store_true")

    sp_att = sub.add_parser(
        "attitude-sweep", help="automated orientation-leak test (needs TestingTools)"
    )
    sp_att.add_argument("--speed", type=float, default=300.0)
    sp_att.add_argument("--aoa", type=float, default=5.0)
    sp_att.add_argument(
        "--axis", type=float, nargs=3, default=[1.0, 0.0, 0.0], metavar=("X", "Y", "Z")
    )
    sp_att.add_argument("--steps", type=int, default=6)
    sp_att.add_argument("--step-deg", type=float, default=30.0)

    sp_val = sub.add_parser("validate", help="simulated vs live aero force")
    sp_val.add_argument("--descend", action="store_true", help="sample over time")
    sp_val.add_argument("--duration", type=float, default=120.0)
    sp_val.add_argument("--interval", type=float, default=2.0)
    sp_val.add_argument("--warp", type=int, default=1, help="physics warp factor")
    sp_val.add_argument("--csv", default=None)

    args = p.parse_args()
    conn, h = connect(args)
    try:
        if args.cmd == "env":
            h.report_env()
        elif args.cmd == "frames":
            h.report_env()
            h.test_frames(args.speed)
        elif args.cmd == "density":
            h.test_density()
        elif args.cmd == "determinism":
            h.test_determinism(args.speed, args.samples)
        elif args.cmd == "sweep":
            h.sweep(
                args.speed,
                args.aoa_min,
                args.aoa_max,
                args.aoa_step,
                args.sideslip,
                args.csv,
            )
        elif args.cmd == "snapshot":
            h.snapshot(args.speed, args.aoa, args.sideslip)
        elif args.cmd == "setup":
            h.setup_scenario(args)
        elif args.cmd == "attitude-sweep":
            h.attitude_leak(
                args.speed, args.aoa, tuple(args.axis), args.steps, args.step_deg
            )
        elif args.cmd == "validate":
            h.validate(args.descend, args.duration, args.interval, args.warp, args.csv)
        else:  # default battery
            run_section("env", h.report_env)
            run_section("frames", lambda: h.test_frames(200.0))
            run_section("density", h.test_density)
            run_section("determinism", lambda: h.test_determinism(200.0, 10))
            run_section(
                "sweep",
                lambda: h.sweep(300.0, -20.0, 20.0, 5.0, 0.0, None),
            )
    finally:
        conn.close()
    print("Done.")


if __name__ == "__main__":
    main()
