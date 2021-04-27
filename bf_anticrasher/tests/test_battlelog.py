import unittest
from bf_anticrasher import app


class BL(unittest.TestCase):
    def test_Session(self):
        s = app.Session()
        self.assertIsNotNone(s.get("http://ident.me"))
    def test_GetUserInfo(self):        
        personaId, status, username = app.GetUserInfo("ResupinePuma")
        self.assertEqual(personaId, 914070650)

    def test_GetUserInfo_x100(self):
        def rpt():
            for i in range(100):
                try:
                    personaId, status, username = app.GetUserInfo("ResupinePuma")
                except:
                    return i
            return 100
        self.assertEqual(rpt(), 100)