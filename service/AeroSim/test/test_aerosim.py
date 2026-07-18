"""Tests for the AeroSim service.

KSPAeroSim is not a managed test mod: install it manually by copying the
mod's GameData contents into the KSP GameData folder, and pass
--skip-gamedata-check to krpc-install so the GameData validation accepts the
unmanaged mod. The test classes skip themselves when the game state does not
match (mod installed or not), so the file passes in both states.
"""

import math
import unittest

import krpctest
from krpctest.geometry import (
    cross,
    norm,
    normalize,
    quaternion_axis_angle,
    quaternion_mult,
    vector,
)


class TestAeroSimUnavailable(krpctest.TestCase):
    """The unavailable path: every RPC must raise a helpful error."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        conn = cls.connect()
        cls.aero_sim = conn.aero_sim
        cls.vessel = conn.space_center.active_vessel

    def setUp(self):
        if self.aero_sim.available:
            self.skipTest("KSPAeroSim is installed")

    def test_available(self):
        self.assertFalse(self.aero_sim.available)

    def test_create_model_raises(self):
        with self.assertRaises(RuntimeError) as error:
            self.aero_sim.create_model(self.vessel, self.vessel.control.current_stage)
        self.assertIn("KSPAeroSim is not installed", str(error.exception))

    def test_api_version_raises(self):
        with self.assertRaises(RuntimeError) as error:
            _ = self.aero_sim.api_version
        self.assertIn("KSPAeroSim is not installed", str(error.exception))

    def test_flight_simulate_raises(self):
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        with self.assertRaises(RuntimeError) as error:
            flight.simulate_aerodynamic_force_at(
                body,
                self.vessel.position(ref),
                (0.0, 0.0, 0.0),
                self.vessel.rotation(ref),
            )
        self.assertIn("KSPAeroSim is not installed", str(error.exception))


class TestAeroSim(krpctest.TestCase):
    """Model capture, evaluation, decomposition and trim analysis.

    The tests are self-consistent: they compare simulated values against
    each other under a synthetic 300 m/s wind, so they need no live airflow
    and run on the launchpad.
    """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Aero")
        cls.remove_other_vessels()
        conn = cls.connect()
        if not conn.aero_sim.available:
            raise unittest.SkipTest("KSPAeroSim is not installed")
        cls.aero_sim = conn.aero_sim
        cls.space_center = conn.space_center
        cls.vessel = cls.space_center.active_vessel

    def create_model(self):
        try:
            return self.aero_sim.create_model(
                self.vessel, self.vessel.control.current_stage
            )
        except RuntimeError as error:
            if "aerodynamic provider" in str(error):
                self.skipTest("no compatible aerodynamic provider")
            raise

    def head_on_wind(self, ref):
        # 300 m/s wind along the nose (angle of attack 0 at the current
        # attitude)
        return tuple(300 * vector(self.vessel.direction(ref)))

    def pitch_axis(self, ref):
        nose = vector(self.vessel.direction(ref))
        axis = cross(nose, (0, 1, 0))
        if norm(axis) < 0.1:
            axis = cross(nose, (1, 0, 0))
        return normalize(axis)

    def test_api_version(self):
        self.assertRegex(self.aero_sim.api_version, r"^0\.2\.")

    def test_providers(self):
        providers = self.aero_sim.providers
        self.assertGreaterEqual(len(providers), 1)
        for provider in providers:
            self.assertTrue(provider.id)
            self.assertTrue(provider.version)
            self.assertIsInstance(provider.priority, int)
            self.assertIsInstance(provider.supports_editor, bool)

    def test_model_properties(self):
        model = self.create_model()
        self.assertIn(model.provider_id, [p.id for p in self.aero_sim.providers])
        self.assertEqual(self.vessel.control.current_stage, model.after_stage)
        self.assertTrue(model.is_reference_component)
        # kRPC vessel mass is in kilograms, as is the model mass
        self.assertAlmostEqual(
            self.vessel.mass, model.mass, delta=0.05 * self.vessel.mass
        )
        self.assertEqual(len(self.vessel.parts.all), len(model.source_part_flight_ids))
        ref = self.vessel.orbit.body.reference_frame
        com = vector(model.captured_center_of_mass(ref))
        position = vector(self.vessel.position(ref))
        self.assertLess(norm(com - position), 50)

    def test_evaluate_matches_flight_endpoint(self):
        # pylint: disable=too-many-locals
        # The deprecated Flight endpoint delegates to the same mod, so a held
        # model and a per-call capture must agree at the same state and UT.
        # Each RPC samples a different physics frame of the unpaused game, so
        # the craft shifts slightly on its launchpad suspension between calls.
        body = self.vessel.orbit.body
        ref = body.reference_frame
        flight = self.vessel.flight(ref)
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        model = self.create_model()
        ut = self.space_center.ut

        force, torque = model.evaluate(
            body, ref, position, velocity, rotation, zero, ut
        )
        legacy_force, legacy_torque = flight.simulate_aerodynamic_wrench_at(
            body, position, velocity, rotation, zero, ut
        )

        force = vector(force)
        legacy_force = vector(legacy_force)
        self.assertGreater(norm(force), 1000)
        tolerance = 1e-3 + 1e-3 * max(norm(force), norm(legacy_force))
        self.assertLessEqual(norm(force - legacy_force), tolerance)
        torque = vector(torque)
        legacy_torque = vector(legacy_torque)
        torque_tolerance = 1e-3 + 1e-2 * max(norm(torque), norm(legacy_torque))
        self.assertLessEqual(norm(torque - legacy_torque), torque_tolerance)

    def test_evaluate_attitude(self):
        # Pitching 90 degrees turns a head-on wind into a broadside wind and
        # must change the wrench of this asymmetric craft.
        body = self.vessel.orbit.body
        ref = body.reference_frame
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        model = self.create_model()
        ut = self.space_center.ut

        head_on, _ = model.evaluate(body, ref, position, velocity, rotation, zero, ut)
        pitched = quaternion_mult(
            quaternion_axis_angle(self.pitch_axis(ref), math.radians(90)),
            rotation,
        )
        broadside, broadside_torque = model.evaluate(
            body, ref, position, velocity, pitched, zero, ut
        )
        head_on = vector(head_on)
        broadside = vector(broadside)
        self.assertGreater(norm(broadside - head_on), 0.1 * norm(head_on))
        self.assertGreater(norm(vector(broadside_torque)), 1)

    def test_create_models(self):
        try:
            models = self.aero_sim.create_models(
                self.vessel, self.vessel.control.current_stage
            )
        except RuntimeError as error:
            if "aerodynamic provider" in str(error):
                self.skipTest("no compatible aerodynamic provider")
            raise
        self.assertGreaterEqual(len(models), 1)
        self.assertTrue(models[0].is_reference_component)

    def test_decompose_force(self):
        velocity = (300.0, 0.0, 0.0)
        drag, lift, ratio = self.aero_sim.decompose_force((-1000.0, 0.0, 0.0), velocity)
        self.assertAlmostEqual(1000, drag, places=3)
        self.assertAlmostEqual(0, lift, places=3)
        self.assertAlmostEqual(0, ratio, places=3)
        drag, lift, _ = self.aero_sim.decompose_force((-1000.0, 500.0, 0.0), velocity)
        self.assertAlmostEqual(1000, drag, places=3)
        self.assertAlmostEqual(500, lift, places=3)

    def test_release(self):
        model = self.create_model()
        self.assertGreater(model.mass, 0)
        model.release()
        with self.assertRaises(RuntimeError) as error:
            _ = model.mass
        self.assertIn("released", str(error.exception))

    def test_trim_session(self):
        # pylint: disable=too-many-locals
        body = self.vessel.orbit.body
        ref = body.reference_frame
        position = self.vessel.position(ref)
        velocity = self.head_on_wind(ref)
        rotation = self.vessel.rotation(ref)
        zero = (0.0, 0.0, 0.0)
        model = self.create_model()
        ut = self.space_center.ut

        session = model.create_pitch_trim_session(
            body, ref, position, velocity, rotation, zero, ut
        )
        self.assertFalse(session.is_complete)
        self.assertIsNone(session.result)
        for _ in range(100):
            if session.step(50):
                break
        else:
            self.fail("trim session did not complete")
        self.assertTrue(session.is_complete)
        self.assertGreater(session.evaluation_count, 31)

        result = session.result
        self.assertIsNotNone(result)
        angles = result.sample_angles
        torques = result.sample_torques
        self.assertEqual(len(angles), len(torques))
        self.assertGreaterEqual(len(angles), 10)
        self.assertAlmostEqual(-30, angles[0], places=3)
        self.assertAlmostEqual(30, angles[-1], places=3)
        stabilities = self.aero_sim.TrimStability
        for root in result.roots:
            self.assertGreaterEqual(root.angle, angles[0])
            self.assertLessEqual(root.angle, angles[-1])
            self.assertIn(
                root.stability,
                [
                    stabilities.restoring,
                    stabilities.neutral,
                    stabilities.unstable,
                ],
            )
            self.assertGreaterEqual(root.drag, 0)
            self.assertGreaterEqual(root.lift, 0)
            force = vector(root.force(ref))
            self.assertTrue(all(math.isfinite(x) for x in force))
        if result.has_stable_root:
            self.assertIsNotNone(result.selected_root)
            self.assertEqual(stabilities.restoring, result.selected_root.stability)
        else:
            self.assertIsNone(result.selected_root)

        session.release()
        with self.assertRaises(RuntimeError):
            session.step(1)
        # A result obtained before the release remains readable
        self.assertEqual(len(angles), len(result.sample_angles))


if __name__ == "__main__":
    unittest.main()
