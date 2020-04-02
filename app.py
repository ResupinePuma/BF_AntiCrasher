########################
# made by ResupinePuma
#  for bf4 community
########################

import json, requests, re
import socket
from threading import Thread
from frostbite_rcon_utils import *
from config import *

headers ={
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0",
    "Accept-Encoding": "gzip, deflate, br",
    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
    "Connection":"keep-alive",
    "Upgrade-Insecure-Requests": "1"    
}

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
                    data_buffer += self.connection.recv(4096)
                packet = decode_packet(data_buffer)                
                Thread(target=self.__DecodeEvent, args=[packet]).start()
                #self.__DecodeEvent(packet['words'])
        self.listener = Thread(name="Listener", target=listen)
        self.listener.start()


    def CloseConnection(self):
        self.listener._delete()
        self.connection.close()

def GetUserInfo(username):
    res = requests.post(url=f"https://battlelog.battlefield.com/{bf}/search/query/", headers=headers, data=f"query={username}")
    if not res.status_code == 200:
        raise Exception()
    a = json.loads(res.text)
    find = False
    for d in a['data']:
        if d['personaName'] == username:
            find = True
            personaId = a['data'][0]["personaId"]
    if find == False:
        return Exception("User not find")
    bf4 = f"https://battlelog.battlefield.com/bf4/warsawoverviewpopulate/{personaId}/1/"
    bf3 = f"https://battlelog.battlefield.com/bf3/overviewPopulateStats/{personaId}/None/1/"
    if bf.lower() == "bf4":
        url = bf4
    elif bf.lower() == "bf3" :
        url = bf3
    res = requests.get(url, headers=headers)
    if not res.status_code == 200:
        raise Exception()
    if '"overviewStats":null,' in res.text:
        return (personaId, 0, 0, username)
    score = re.findall(r'''\"scorePerMinute\":(.*?),''', res.text)[0]
    time = re.findall(r'''\"timePlayed\":(.*?),''', res.text)[0]
    return (personaId, score, time, username)
        
def CheckParams(player, guid):
    if (int(player[1]) > score_per_minute or int(player[2]) > played_time):
        print(f"{player[3]} banned " + str(rcon.Command(f"banList.add guid {guid} perm Crasher({player[3]})")))
    else:
        print(f"{player[3]} joined")


def OnJoin(event):
    try:
        player = GetUserInfo(event[0])
        guid = event[1]
        CheckParams(player, guid)
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
