import os
import shutil
import tempfile
from unittest import mock

import krpctest
from krpctest.install import (
    MODS,
    _ALL_MOD_SUBDIRS,
    _mod_archive_src,
    _reconcile_mods,
    _requested_mod_subdirs,
    _validate_gamedata,
)

# A clean no-mod GameData: the stock game, kRPC, the ModuleManager assembly and its
# runtime-generated cache files.
_CLEAN = [
    "Squad",
    "SquadExpansion",
    "kRPC",
    "ModuleManager.4.2.3.dll",
    "ModuleManager.ConfigCache",
    "ModuleManager.ConfigSHA",
    "ModuleManager.Physics",
    "ModuleManager.TechTree",
]


class TestValidateGamedata(krpctest.TestCase):
    game_required = False

    def setUp(self):
        self.gamedata = tempfile.mkdtemp()

    def tearDown(self):
        shutil.rmtree(self.gamedata, ignore_errors=True)

    def _populate(self, entries):
        for entry in entries:
            with open(os.path.join(self.gamedata, entry), "w", encoding="utf-8"):
                pass

    def test_clean_baseline_passes(self):
        self._populate(_CLEAN)
        _validate_gamedata(self.gamedata, set())

    def test_partial_baseline_passes(self):
        # The ModuleManager cache files do not exist until the first KSP launch, so a
        # fresh install must still validate without them.
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.4.2.3.dll"])
        _validate_gamedata(self.gamedata, set())

    def test_any_module_manager_version_passes(self):
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.9.9.9.dll"])
        _validate_gamedata(self.gamedata, set())

    def test_unexpected_mod_raises(self):
        self._populate(_CLEAN + ["KSPCommunityPartModules"])
        with self.assertRaises(RuntimeError) as cm:
            _validate_gamedata(self.gamedata, set())
        self.assertIn("KSPCommunityPartModules", str(cm.exception))

    def test_requested_mod_allowed(self):
        self._populate(_CLEAN + ["RealChute", "000_Harmony"])
        _validate_gamedata(self.gamedata, {"RealChute", "000_Harmony"})

    def test_requested_mod_missing_from_set_raises(self):
        # A managed mod present in GameData but not among the requested subdirs is still
        # unexpected — reconcile should have removed it.
        self._populate(_CLEAN + ["RealChute"])
        with self.assertRaises(RuntimeError) as cm:
            _validate_gamedata(self.gamedata, set())
        self.assertIn("RealChute", str(cm.exception))

    def test_root_level_mod_archive_path(self):
        self.assertEqual(
            os.path.join("bazel-krpc", "external", "+http_archive+mechjeb", "MechJeb2"),
            _mod_archive_src("mechjeb", "MechJeb2"),
        )

    def test_mechjeb_destination(self):
        self.assertEqual({"MechJeb2"}, _requested_mod_subdirs(["MechJeb"]))

    def test_existing_mod_archive_paths_unchanged(self):
        for mod, components in MODS.items():
            if mod == "MechJeb":
                continue
            for _, source, destination in components:
                self.assertEqual("GameData/" + destination, source)

    def test_reconcile_removes_every_unrequested_mod(self):
        for destination in _ALL_MOD_SUBDIRS:
            os.makedirs(os.path.join(self.gamedata, destination), exist_ok=True)

        _reconcile_mods([], self.gamedata, self.gamedata)

        for destination in _ALL_MOD_SUBDIRS:
            self.assertFalse(os.path.exists(os.path.join(self.gamedata, destination)))

    def test_reconcile_installs_mechjeb_from_archive_root(self):
        archive = os.path.join(
            self.gamedata,
            _mod_archive_src("mechjeb", "MechJeb2"),
        )
        os.makedirs(archive)
        with open(os.path.join(archive, "MechJeb2.dll"), "w", encoding="utf-8"):
            pass
        gamedata = os.path.join(self.gamedata, "ksp", "GameData")
        os.makedirs(gamedata)

        with mock.patch("krpctest.install.subprocess.check_call") as build:
            _reconcile_mods(["MechJeb"], self.gamedata, gamedata)

        build.assert_called_once_with(
            ["bazel", "build", "//tools/mods:mechjeb"],
            cwd=self.gamedata,
        )
        self.assertTrue(
            os.path.isfile(os.path.join(gamedata, "MechJeb2", "MechJeb2.dll"))
        )
