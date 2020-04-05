/* LANmode.cs

v1.0.0.0
by Despo

*/

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Web;
using System.Data;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Reflection;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
	
	//Aliases
	using EventType = PRoCon.Core.Events.EventType;
	
	public class Huinya : PRoConPluginAPI, IPRoConPluginInterface
	{
        public string bf;

        public Huinya()
        { this.bf = "bf3"; }
		
		
		public void ConsoleWrite(string msg)
		{
			this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
		}


		public void ServerCommand(params String[] args)
		{
			List<string> list = new List<string>();
			list.Add("procon.protected.send");
			list.AddRange(args);
			this.ExecuteCommand(list.ToArray());
		}

        public string GetPluginName()
        {
            return "AntiCrasher";
        }

        public string GetPluginVersion()
        {
            return "0.3";
        }

        public string GetPluginAuthor()
        {
            return "ResupinePuma";
        }

        public string GetPluginWebsite()
        {
            return "https://github.com/ResupinePuma/BF_AntiCrasher/tree/procon";
        }

        public string GetPluginDescription()
        {
            return "Plugin to prevent people crash your server\nSet game version ang forget about plugin";
        }


		public List<CPluginVariable> GetDisplayPluginVariables() 
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();
			lstReturn.Add(new CPluginVariable("Main|Game version (bf3 or bf4)", typeof(string), this.bf));
			return lstReturn;
		}
		
		public List<CPluginVariable> GetPluginVariables() 
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();
			lstReturn.Add(new CPluginVariable("Game version (bf3 or bf4)", typeof(string), this.bf));
			return lstReturn;
		}

		public void SetPluginVariable(string strVariable, string strValue)
		{
			if (strVariable.CompareTo("Game version (bf3 or bf4)") == 0)
			{
				this.bf = strValue;
			}
		}


		public string SendRequest(string method, string url, string data)
		{
			var request = WebRequest.Create(url);
			request.Method = method;
			byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
			// устанавливаем тип содержимого - параметр ContentType
			request.Headers.Add ("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0");
			request.Headers.Add("Upgrade-Insecure-Requests", "1");
			request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			// Устанавливаем заголовок Content-Length запроса - свойство ContentLength
			request.ContentLength = byteArray.Length;
			using (Stream dataStream = request.GetRequestStream())
				dataStream.Write(byteArray, 0, byteArray.Length);

			WebResponse response = request.GetResponse();

			data = "";
			using (Stream s = request.GetResponse().GetResponseStream())
			{
				using (StreamReader sr = new StreamReader(s))
				{
					var jsonData = sr.ReadToEnd();
					return jsonData.ToString();

				}
			}

		}

		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
			this.RegisterEvents(this.GetType().Name, "OnPlayerJoin");
		}
		
		public void OnPluginEnable() {
			//fIsEnabled = true;
			this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAntiCrasher | ^2Enabled");
		}
		
		public void OnPluginDisable() {
			//fIsEnabled = false;
			this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAntiCrasher  | ^1Disabled!");
		}
		
		public override void OnVersion(string serverType, string version) {}
		
		public override void OnServerInfo(CServerInfo serverInfo) {}
		
		public override void OnResponseError(List<string> requestWords, string error) { }
		
		public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset) {}

		public static Dictionary<K, V> HashtableToDictionary<K, V>(Hashtable table)
		{
			return table
			  .Cast<DictionaryEntry>()
			  .ToDictionary(kvp => (K)kvp.Key, kvp => (V)kvp.Value);
		}

		public override void OnPlayerJoin(string soldierName) {
			this.ExecuteCommand("procon.protected.pluginconsole.write", soldierName);
			string data = SendRequest("POST", string.Format("https://battlelog.battlefield.com/{0}/search/query/", "bf4"), string.Format("query={0}", soldierName));
			var ja = JSON.JsonDecode(data);
			string id = "";
			var dict = HashtableToDictionary<string, object>(ja as Hashtable);
			if (dict["data"] != null)
			{
				foreach (Hashtable key in dict["data"] as ArrayList)
				{
					var info = HashtableToDictionary<string, object>(key as Hashtable);
					if (info["personaName"].ToString() == soldierName)
						id = info["personaId"].ToString();
					break;
				}

			}
			else
				return;
			string url = "";
			switch (bf)
			{
				case "bf4":
					url = string.Format("https://battlelog.battlefield.com/bf4/warsawoverviewpopulate/{0}/1/", id);
					break;
				case "bf3":
					url = string.Format("https://battlelog.battlefield.com/bf3/overviewPopulateStats/{0}/None/1/", id);
					break;
			}
			data = SendRequest("GET", url, null);
			var stats = JSON.JsonDecode(data);
			var statsa = HashtableToDictionary<string, object>(stats as Hashtable);
			statsa = HashtableToDictionary<string, object>(statsa["data"] as Hashtable);

			if (statsa["overviewStats"] != null)
			{
				statsa = HashtableToDictionary<string, object>(statsa["overviewStats"] as Hashtable);
				foreach (KeyValuePair<string, object> key in statsa)
					if (key.Value != null)
					{
						string name = key.Value.GetType().Name;
						switch (name)
						{
							default:
								continue;
							case "Int32":
								continue;
							case "Int64":
								int value = 0;
								bool ban = Int32.TryParse(key.Value.ToString(), out value);
								if (!ban)
									break;
								else
									continue;
							case "Double":
								int ii = 0;
								bool i = Int32.TryParse(key.Value.ToString(), out ii);
								if (i)
									continue;
								else
								{
									double db = 0;
									i = Double.TryParse(key.Value.ToString(), out db);
									if (db < int.MaxValue && db > int.MinValue)
										continue;										
									else
									{
										this.ExecuteCommand("procon.protected.send", "admin.banPlayer", soldierName, soldierName);
										break;
									}
								}
						}
					}

			}

		}
		
		public override void OnPlayerLeft(CPlayerInfo playerInfo) {}
		
		public override void OnPlayerKilled(Kill kKillerVictimDetails) {}
		
		public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) {}
		
		public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId) {}
		
		public override void OnGlobalChat(string speaker, string message) {}
		
		public override void OnTeamChat(string speaker, string message, int teamId) {}
		
		public override void OnSquadChat(string speaker, string message, int teamId, int squadId) {}
		
		public override void OnRoundOverPlayers(List<CPlayerInfo> players) {}
		
		public override void OnRoundOverTeamScores(List<TeamScore> teamScores) {}
		
		public override void OnRoundOver(int winningTeamId) {}
		
		public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) {}
		
		public override void OnLevelStarted() {}
		
		public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal) {} // BF3
		
		
	}

}



