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
	
	public class AntiCrasher4 : PRoConPluginAPI, IPRoConPluginInterface
	{
        public string bf;
		public string nick;
		public string proxies;
		public string whitelist;
		public List<string> proxies_list;
		public string last_worked;

		public AntiCrasher4()
        { this.bf = "bf3";
		  this.nick = "";
		this.whitelist = "";
		this.proxies = "";
		this.proxies_list = new List<string>();
		this.last_worked = "";
		}
		
		
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
			lstReturn.Add(new CPluginVariable("Main|Ban Nickname", typeof(string), this.nick));
			lstReturn.Add(new CPluginVariable("Main|Proxies", typeof(string), this.proxies));
			lstReturn.Add(new CPluginVariable("Main|Whitelist", typeof(string), this.whitelist));
			return lstReturn;
		}
		
		public List<CPluginVariable> GetPluginVariables() 
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();
			lstReturn.Add(new CPluginVariable("Game version (bf3 or bf4)", typeof(string), this.bf));
			lstReturn.Add(new CPluginVariable("Ban Nickname", typeof(string), this.nick));
			lstReturn.Add(new CPluginVariable("Proxies", typeof(string), this.proxies));
			lstReturn.Add(new CPluginVariable("Whitelist", typeof(string), this.whitelist));
			return lstReturn;
		}

		public void SetPluginVariable(string strVariable, string strValue)
		{
			if (strVariable.CompareTo("Game version (bf3 or bf4)") == 0)
			{
				this.bf = strValue;
			}
			else if (strVariable.CompareTo("Ban Nickname") == 0)
			{
				this.nick = strValue;
			}
			else if (strVariable.CompareTo("Whitelist") == 0)
			{
				this.whitelist = strValue;
			}
			else if (strVariable.CompareTo("Proxies") == 0)
			{
				this.proxies = strValue;
				if (strValue.Length > 0)
					this.proxies_list = strValue.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
			}
		}


		public string SendRequest(string method, string url, string data = "", string proxy = "")
		{
			var request = (HttpWebRequest)WebRequest.Create(url);

			if (proxy.Length > 0)
			{
				WebProxy myproxy = new WebProxy(proxy, true);
				request.Proxy = myproxy;
			}
			request.Method = method;
			byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(data);
			request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0";
			//         try { request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0"; }
			//catch { }		
			//request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:74.0) Gecko/20100101 Firefox/68.0");
			request.Headers.Add("Upgrade-Insecure-Requests", "1");
			request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			request.ContentLength = byteArray.Length;
			if (data.Length > 0)
				using (Stream dataStream = request.GetRequestStream())
					dataStream.Write(byteArray, 0, byteArray.Length);

			//WebResponse response = request.GetResponse();

			if (proxy.Length > 0)
			{
				last_worked = proxy;
			}

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

		public string SendRequestWithProxy(string method, string url, string data = "")
		{
			List<string> tmp = this.proxies_list;

			if (this.last_worked.Length > 0)
			{
				try
				{
					return SendRequest(method, url, data, this.last_worked);
				}
				catch (Exception ex)
				{
					this.last_worked = "";
					return SendRequestWithProxy(method, url, data);
				}
			}
			else
			{
				foreach (string prx in tmp)
				{
					try
					{
						return SendRequest(method, url, data, prx);
					}
					catch (Exception ex)
					{
						try
						{
							this.proxies_list.Remove(prx);
						}
						catch
						{ }
					}
				}
			}
			return SendRequest(method, url, data);
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

		public Dictionary<K, V> HashtableToDictionary<K, V>(Hashtable table)
		{
			return table
			  .Cast<DictionaryEntry>()
			  .ToDictionary(kvp => (K)kvp.Key, kvp => (V)kvp.Value);
		}

		public override void OnPlayerJoin(string soldierName) {
			if (this.whitelist.Contains(soldierName))
			{
				this.ExecuteCommand("procon.protected.pluginconsole.write", "whitelisted soldier: " + soldierName);
				return;
			}

			if (soldierName.Length > 16)
			{
				this.ExecuteCommand("procon.protected.send", "banList.add", "name", soldierName.ToString(), "perm", "Crasher " + soldierName.ToString());
				this.ExecuteCommand("procon.protected.send", "banList.save");
				this.ExecuteCommand("procon.protected.send", "banList.list");
				this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", "name", soldierName.ToString(), "get out");
				return;
			}
			string data = SendRequestWithProxy("POST", string.Format("https://battlelog.battlefield.com/{0}/search/query/", this.bf), string.Format("query={0}", soldierName));
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
			else{
				return;
				
			}
				
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
			data = SendRequestWithProxy("GET", url);
			var stats = JSON.JsonDecode(data);
			var statsa = HashtableToDictionary<string, object>(stats as Hashtable);
			statsa = HashtableToDictionary<string, object>(statsa["data"] as Hashtable);

			if (statsa["overviewStats"] != null)
			{
				
				statsa = HashtableToDictionary<string, object>(statsa["overviewStats"] as Hashtable);
				if (soldierName == this.nick){
					ConsoleWrite("Debug " + this.nick);
					this.ExecuteCommand("procon.protected.send", "banList.add", "name", soldierName.ToString(), "perm", "Crasher " +soldierName.ToString());
					this.ExecuteCommand("procon.protected.send", "banList.save");
					this.ExecuteCommand("procon.protected.send", "banList.list");
					return;
				}
				foreach (KeyValuePair<string, object> key in statsa)
					if (key.Value != null)
					{
						string name = key.Value.GetType().Name;
						//this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAntiCrasher  | GOTCHA!");
                        switch (name)
                        {
                            default:
                                continue;
                            case "Int32":
                                continue;
                            case "Int64":
                                bool ban = Int32.TryParse(key.Value.ToString(), out int value);
                                if (!ban)
                                    break;
                                else
                                    continue;
                            case "Double":
                                bool i = Int32.TryParse(key.Value.ToString(), out int ii);
                                if (i)
                                    continue;
                                else
                                {
                                    i = Double.TryParse(key.Value.ToString(), out double db);
                                    if (db < int.MaxValue && db > int.MinValue)
                                        continue;
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", soldierName.ToString(), "perm", "Crasher " + soldierName.ToString());
                                        this.ExecuteCommand("procon.protected.send", "banList.save");
                                        this.ExecuteCommand("procon.protected.send", "banList.list");
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


