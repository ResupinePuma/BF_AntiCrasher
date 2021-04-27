########################
# made by ResupinePuma
#  for bf4 community
########################

import json, requests, re, random
import socket
from threading import Thread
from bf_anticrasher.frostbite_rcon_utils import *
from config import *
from bf_anticrasher.config import *
import datetime


class RCon():
    def __init__(self, address, password):
        self.ip = address[0]
        self.port = address[1]
        self.password = password    
        self.connection = None
        self.events = {}
        self.listener = None


    def connect(self):
        try:
            self.connection = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.connection.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            self.connection.settimeout(1)
            self.connection.connect((self.ip, self.port))
            self.connection.setblocking(1)

            packet_to_send = encode_packet(create_packet(0, False, False, ['login.plainText', password]))
            self.connection.send(packet_to_send)
            data_buffer = bytes()
            while not contains_complete_packet(data_buffer):
                data_buffer += self.connection.recv(2048)

            packet_to_send = encode_packet(create_packet(0, False, False, ['admin.eventsEnabled', 'True']))
            self.connection.send(packet_to_send)
            data_buffer = bytes()
            while not contains_complete_packet(data_buffer):
                data_buffer += self.connection.recv(2048)
            packet = decode_packet(data_buffer)
            #print(packet)
            if b"OK" in packet['words']:
                return True
            else:
                raise Exception(packet['words'][0].decode("utf-8"))
        except Exception as ex:
            raise ex

    def Command(self, command:str):
        packet_to_send = encode_packet(create_packet(0, False, False, command.split(" ")))
        self.connection.send(packet_to_send)
        data_buffer = bytes()
        while not contains_complete_packet(data_buffer):
            data_buffer += self.connection.recv(2048)
        packet = decode_packet(data_buffer)
        return [w.decode("utf-8") for w in packet['words'] if not type(w) == int]


    def __DecodeEvent(self, event):
        event['words'] = [w.decode("utf-8") for w in event['words'] if not type(w) == int]
        event = event['words']
        if event[0] in self.events.keys():
            funk = self.events[event[0]]
            event.pop(0)
            funk(event)

    def StartEventListener(self):
        def listen():
            while True:
                data_buffer = bytes()
                while not contains_complete_packet(data_buffer):
                    # if b'punkBuster' in self.connection.recv(2048):
                    #     continue
                    #print(self.connection.recv(2048))
                    data_buffer += self.connection.recv(16384)
                packet = decode_packet(data_buffer)                
                Thread(target=self.__DecodeEvent, args=[packet]).start()
                #self.__DecodeEvent(packet['words'])
        self.listener = Thread(name="Listener", target=listen)
        self.listener.start()


    def CloseConnection(self):
        self.listener._delete()
        self.connection.close()

class Session(requests.Session):
    def __init__(self, **kwargs):
        self._proxylist = self.gen_proxylist()
        super().__init__(**kwargs)
        self.proxies.update(random.choice(self._proxylist))

    def gen_proxylist(self):
        #text = requests.get("https://spys.me/proxy.txt").text.replace("\n","=")
        #return [{"http" : f"http://{u}", "https" : f"http://{u}"} for u in re.findall(r"=([0-9.:]+)", text)]
        return [{"http" : None, "https" : None}]

    def request(self, method, url, **kwargs):
        while True:
            try:
                response = super().request(method, url, **kwargs)
                if response.status_code == 200:
                    return response
                else:
                    raise Exception()
            except Exception as ex:
                print(ex)
                self._proxylist.remove(self.proxies)
                if len(self._proxylist) == 0:
                    self._proxylist = self.gen_proxylist()
                self.proxies.update(random.choice(self._proxylist))

        raise Exception("Retries limit exceeded")

session = Session()
session.headers.update({
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0",
    "Accept-Encoding": "gzip, deflate, br",
    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
    "Connection":"keep-alive",
    "Upgrade-Insecure-Requests": "1"    
})

def GetUserInfo(username, debug=False):
    """
    Returns
    ---------
    personaId : int
        id of person
    
    good : bool
        good or bad user

    username : str
        username
    """
    res = session.post(url=f"https://battlelog.battlefield.com/{bf}/search/query/", data=f"query={username}").json()
    #a = json.loads(res.text)
    personaId = [d["personaId"] for d in res.get('data', []) if d.get('personaName', "") == username][-1]
    if not personaId:
        return Exception("User not found")
    else:
        personaId = int(personaId)
    bflist = {
        "4" : f"https://battlelog.battlefield.com/bf4/warsawoverviewpopulate/{personaId}/1/",
        "3" : f"https://battlelog.battlefield.com/bf3/overviewPopulateStats/{personaId}/None/1/"
    }
    url = bflist[re.findall(r"([34])", bf)[0]]
    res = session.get(url).json()

    if res["data"].get("overviewStats"):
        stats = res["data"]["overviewStats"]
        try:
            for d in stats.values():
                if type(d) == int:
                    if not (d < 2147483647 or d > -2147483647) or debug:
                        raise ValueError("Bad person")
        except ValueError:
            return personaId, False, username
        else:
            return personaId, True, username
    
    return personaId, True, username
        
def CheckParams(guid, *args):
    if (args[1] == False):
        print(f"{args[2]} banned " + str(rcon.Command(f"banList.add guid {guid} perm Crasher({player[2]} {player[0]})")))
        f = open("ban.log", "w")
        f.write(f"{str(datetime.datetime.now())} | {guid} | {args[0]} | {args[2]}\n")
        f.close()
    else:
        print(f"{args[2]} joined")

def OnJoin(event):
    try:
        username, guid = event[0], event[1]
        personaId, status, username = GetUserInfo(username)
        CheckParams(guid, personaId, status, username)
    except Exception as ex:
        print(str(ex) + " " + str(event))

if __name__ == "__main__":
    print("i'm alive")
    rcon = RCon((ip,port), password)
    rcon.connect()
    rcon.events.update({"player.onJoin" : OnJoin})
    rcon.StartEventListener()
    while True:
        pass
