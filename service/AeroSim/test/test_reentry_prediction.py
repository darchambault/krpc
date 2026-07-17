"""Tests for AeroSim 6-DOF reentry prediction sessions.

KSPAeroSim is not a managed test mod: install it manually by copying the
mod's GameData contents into the KSP GameData folder. The tests skip
themselves when the mod is not installed.
"""

import math
import unittest

import krpctest

SAMPLE_STRIDE = 18


class TestReentryPrediction(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        conn = cls.connect()
        if not conn.aero_sim.available:
            raise unittest.SkipTest("KSPAeroSim is not installed")
        cls.aero_sim = conn.aero_sim
        cls.space_center = conn.space_center
        cls.vessel = cls.space_center.active_vessel

    def create_prediction(self, maximum_time=20):
        # A synthetic entry state high in Kerbin's atmosphere, in the
        # non-rotating body frame: an equatorial position at 60 km altitude
        # descending at 2200 m/s. The launchpad vessel provides the model;
        # the prediction propagates the hypothetical state only.
        body = self.vessel.orbit.body
        ref = body.non_rotating_reference_frame
        try:
            model = self.aero_sim.create_model(
                self.vessel, self.vessel.control.current_stage
            )
        except RuntimeError as error:
            if "aerodynamic provider" in str(error):
                self.skipTest("no compatible aerodynamic provider")
            raise
        radius = body.equatorial_radius + 60000
        position = (radius, 0.0, 0.0)
        velocity = (-300.0, 0.0, 2200.0)
        rotation = (0.0, 0.0, 0.0, 1.0)
        zero = (0.0, 0.0, 0.0)
        inertia = (1000.0, 0.0, 0.0, 2000.0, 0.0, 1500.0)
        return model.create_reentry_prediction(
            body,
            ref,
            position,
            velocity,
            rotation,
            zero,
            self.space_center.ut,
            inertia,
            stop_altitude=0,
            atmospheric_time_step=0.1,
            vacuum_time_step=2,
            record_interval=0.5,
            maximum_time=maximum_time,
        )

    def run_to_completion(self, prediction):
        for _ in range(100):
            if prediction.step(200):
                return
        self.fail("prediction did not complete")

    def test_prediction(self):
        prediction = self.create_prediction()
        self.assertFalse(prediction.is_complete)
        self.assertIsNone(prediction.result)

        self.run_to_completion(prediction)
        self.assertTrue(prediction.is_complete)
        self.assertGreater(prediction.evaluation_count, 100)
        self.assertLessEqual(
            prediction.equivalent_aerodynamic_endpoint_call_count,
            prediction.evaluation_count,
        )
        self.assertAlmostEqual(20, prediction.elapsed_time, delta=1)
        self.assertLess(prediction.current_altitude, 60000)

        result = prediction.result
        self.assertIsNotNone(result)
        terminations = self.aero_sim.ReentryPredictionTermination
        self.assertEqual(terminations.maximum_time_reached, result.termination)
        self.assertFalse(result.reached_stop_altitude)
        self.assertGreaterEqual(result.peak_dynamic_pressure, 0)
        self.assertGreaterEqual(result.maximum_angle_of_attack, 0)
        self.assertGreaterEqual(result.maximum_body_rate, 0)
        self.assertTrue(-90 <= result.latitude <= 90)
        self.assertTrue(-180 <= result.longitude <= 180)
        self.assertGreater(result.surface_downrange, 0)
        self.assertGreater(result.wall_time, 0)
        self.assertEqual(prediction.evaluation_count, result.evaluation_count)

    def test_samples(self):
        prediction = self.create_prediction()
        self.run_to_completion(prediction)

        count = prediction.sample_count
        # 20 s of simulated time at a 0.5 s record interval
        self.assertGreaterEqual(count, 10)
        samples = prediction.get_samples(0, count)
        self.assertEqual(SAMPLE_STRIDE * count, len(samples))
        self.assertTrue(all(math.isfinite(x) for x in samples))
        # Elapsed time (offset 1) must be increasing and altitude (offset 2)
        # decreasing on this steep entry
        elapsed = samples[1::SAMPLE_STRIDE]
        self.assertEqual(sorted(elapsed), list(elapsed))
        self.assertLess(samples[-SAMPLE_STRIDE + 2], samples[2])

        # ArgumentOutOfRangeException maps to ValueError in the Python client
        with self.assertRaises(ValueError):
            prediction.get_samples(0, count + 1)

        result = prediction.result
        self.assertEqual(count, result.sample_count)
        self.assertEqual(samples, result.get_samples(0, count))
        final = result.final_sample
        self.assertEqual(SAMPLE_STRIDE, len(final))
        self.assertAlmostEqual(prediction.elapsed_time, final[1], delta=1e-6)

    def test_release(self):
        prediction = self.create_prediction(maximum_time=5)
        self.run_to_completion(prediction)
        result = prediction.result
        prediction.release()
        with self.assertRaises(RuntimeError):
            prediction.step(1)
        # The result remains valid after the session release
        self.assertGreater(result.sample_count, 0)
        result.release()
        with self.assertRaises(RuntimeError):
            result.sample_count  # pylint: disable=pointless-statement
        # Scalars snapshotted at completion remain readable
        self.assertGreaterEqual(result.peak_dynamic_pressure, 0)
        self.assertEqual(SAMPLE_STRIDE, len(result.final_sample))


if __name__ == "__main__":
    unittest.main()
