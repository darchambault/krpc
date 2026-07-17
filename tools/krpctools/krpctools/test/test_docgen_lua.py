from types import SimpleNamespace
import unittest
from krpctools.test.docgentest import DocGenTestCase
from krpctools.docgen.lua import LuaDomain
from krpctools.docgen.nodes import ClassProperty


class TestDocGenLua(DocGenTestCase, unittest.TestCase):
    language = "lua"
    domain = LuaDomain
    maxDiff = None

    def test_cross_service_class_property_reference(self):
        getter = SimpleNamespace(
            return_type=None,
            game_scenes=None,
            documentation="",
            deprecated=False,
            deprecated_reason="",
        )
        prop = ClassProperty(
            "SpaceCenter", "CelestialBody", "NonRotatingReferenceFrame", getter=getter
        )
        domain = LuaDomain(__file__)

        self.assertEqual(
            ":attr:`SpaceCenter.CelestialBody.non_rotating_reference_frame "
            "<SpaceCenter.SpaceCenter.CelestialBody.non_rotating_reference_frame>`",
            domain.see(prop),
        )


if __name__ == "__main__":
    unittest.main()
