import unittest

import krpctest


class TestMechJebUnavailable(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.mech_jeb = cls.connect().mech_jeb

    def test_unavailable_without_mechjeb(self):
        self.assertFalse(self.mech_jeb.available)


class TestMechJeb(krpctest.TestCase):
    mods = ["MechJeb"]

    @classmethod
    def setUpClass(cls):
        cls.new_save(always_load=True)
        cls.mech_jeb = cls.connect().mech_jeb

    def wait_until_ready(self):
        def api_ready():
            try:
                return self.mech_jeb.api_ready
            except RuntimeError:
                return False

        self.wait_until(
            api_ready,
            timeout=30,
            message="MechJeb API readiness",
        )

    def test_available_and_ready(self):
        self.assertTrue(self.mech_jeb.available)
        self.wait_until_ready()

    def test_representative_modules(self):
        self.wait_until_ready()
        self.assertIsInstance(self.mech_jeb.ascent_autopilot.enabled, bool)
        self.assertIsInstance(self.mech_jeb.smart_ass.force_roll, bool)
        self.assertIsNotNone(self.mech_jeb.maneuver_planner.operation_circularize)
        self.assertIsInstance(self.mech_jeb.node_executor.enabled, bool)
        self.assertIsInstance(self.mech_jeb.staging_controller.enabled, bool)
        self.assertIsInstance(
            self.mech_jeb.target_controller.normal_target_exists, bool
        )
        self.assertIsInstance(self.mech_jeb.thrust_controller.enabled, bool)


class TestMechJebPersistence(krpctest.TestCase):
    mods = ["MechJeb"]

    def setUp(self):
        self.new_save(always_load=True)
        self.launch_vessel_from_vab(
            "AutoPilot",
            directory="service/SpaceCenter/test/craft",
        )
        self.mech_jeb = self.connect().mech_jeb

    def api_ready(self):
        try:
            return self.mech_jeb.api_ready
        except RuntimeError:
            return False

    def wait_until_ready(self):
        self.wait_until(
            self.api_ready,
            timeout=60,
            message="MechJeb API readiness after scene reload",
        )

    def wait_for_reload(self, previous_vessel):
        conn = self.connect()

        def reloaded():
            try:
                return (
                    conn.krpc.game_scene == conn.krpc.GameScene.flight
                    and conn.space_center.active_vessel is not None
                    and conn.space_center.active_vessel != previous_vessel
                    and conn.space_center.active_vessel.parts.root is not None
                )
            except RuntimeError:
                return False

        self.wait_until(
            reloaded,
            timeout=60,
            message="flight scene after reload",
        )

    def test_reinitializes_after_quickload(self):
        self.wait_until_ready()
        smart_ass = self.mech_jeb.smart_ass
        space_center = self.connect().space_center
        previous_vessel = space_center.active_vessel
        space_center.quicksave()
        self.wait(1)
        space_center.quickload()
        self.wait_for_reload(previous_vessel)
        self.wait_until_ready()
        self.assertIsInstance(smart_ass.force_roll, bool)

    def test_reinitializes_after_revert(self):
        self.wait_until_ready()
        smart_ass = self.mech_jeb.smart_ass
        space_center = self.connect().space_center
        previous_vessel = space_center.active_vessel
        self.assertTrue(space_center.can_revert_to_launch)
        space_center.revert_to_launch()
        self.wait_for_reload(previous_vessel)
        self.wait_until_ready()
        self.assertIsInstance(smart_ass.force_roll, bool)


if __name__ == "__main__":
    unittest.main()
