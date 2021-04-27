import unittest
from bf_anticrasher import app


class BL(unittest.TestCase):
    def test_Session(self):
        s = app.Session()
        self.assertIsNotNone(s.get("http://ident.me"))
    def test_GetUserInfo(self):        
        personaId, status, username = app.GetUserInfo("ResupinePuma")
        self.assertEqual(personaId, 914070650)