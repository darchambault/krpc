import unittest

import krpctest


class TestMechJebUnavailable(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.mech_jeb = cls.connect().mech_jeb

    def test_unavailable_without_mechjeb(self):
        self.assertFalse(self.mech_jeb.available)


if __name__ == "__main__":
    unittest.main()
