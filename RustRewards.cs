using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.CSharp;
using Oxide.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
	[Info("RustRewards", "MSpeedie", "2.0.07")]
	[Description("Rewards players for activities using Economic or ServerRewards")]
	// Big Tahnk you to Tarek the original author of this plugin!
	public class RustRewards : RustPlugin
	{
		[PluginReference]
		Plugin Economics, ServerRewards, Friends, Clans;

		private CultureInfo CurrencyCulture = CultureInfo.CreateSpecificCulture("en-US");  // change this to change the currency symbol

		private bool IsFriendsLoaded = false;
		private bool IsEconomicsLoaded = false;
		private bool IsServerRewardsLoaded = false;
		private bool IsClansLoaded = false;
		//private bool IsNPCLoaded = false;
		private BasePlayer heliLastHitPlayer;
		private BasePlayer chinookLastHitPlayer;
		private BasePlayer bradleyLastHitPlayer;

		// private List<uint> EntityCollectionCache = new List<uint>();
		private Dictionary<uint, BasePlayer> EntityCollectionCache = new Dictionary<uint, BasePlayer>();
		readonly DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RustRewards");
		private Dictionary<string, string> playerPrefs = new Dictionary<string, string>();

		private bool HappyHourActive = false;
		private bool HappyHourCross24 = false;
		private Timer timeCheck;

		StoredData storedData;

		RewardRates rr;
		Multipliers m;
		Options o;
		Rewards_Version rv; //Strings str;
		public List<string> Options_itemList = new List<string> {
				"NPCReward_Enabled",
				"VIPMultiplier_Enabled",
				"ActivityReward_Enabled",
				"WelcomeMoney_Enabled",
				"WeaponMultiplier_Enabled",
				"DistanceMultiplier_Enabled",
				"UseEconomicsPlugin",
				"UseServerRewardsPlugin",
				"UseFriendsPlugin",
				"UseClansPlugin",
				"Economics_TakeMoneyFromVictim",
				"ServerRewards_TakeMoneyFromVictim",
				"PrintToConsole",
				"HappyHour_Enabled"
				};
		public List<string> Multipliers_itemList = new List<string> {
				"AssaultRifle",
				"BeancanGrenade",
				"BoltActionRifle",
				"BoneClub",
				"BoneKnife",
				"CandyCaneClub",
				"CompoundBow",
				"Crossbow",
				"CustomSMG",
				"DoubleBarrelShotgun",
				"EokaPistol",
				"F1Grenade",
				"HandmadeFishingRod",
				"HuntingBow",
				"LR300",
				"Longsword",
				"M249",
				"M92Pistol",
				"MP5A4",
				"Mace",
				"Machete",
				"NailGun",
				"PumpShotgun",
				"PythonRevolver",
				"Revolver",
				"RocketLauncher",
				"SalvagedCleaver",
				"SalvagedSword",
				"SatchelCharge",
				"SemiAutomaticPistol",
				"SemiAutomaticRifle",
				"Snowball",
				"Spas12Shotgun",
				"StoneSpear",
				"Thompson",
				"TimedExplosiveCharge",
				"WaterpipeShotgun",
				"WoodenSpear",
				"VIPMultiplier",
				"HappyHourMultiplier",
				"distance_100",
				"distance_200",
				"distance_300",
				"distance_400",
				"distance_50"

				};
		public List<string> Rewards_itemList = new List<string> {
				"autoturret",
				"barrel",
				"bear",
				"boar",
				"bradley",
				"cactus",
				"chicken",
				"chinook",
				"corn",
				"crate",
				"foodbox",
				"helicopter",
				"hemp",
				"horse",
				"human",
				"minecart",
				"mushrooms",
				"ore",
				"pumpkin",
				"murderer",
				"scientist",
				"stag",
				"stones",
				"supplysignal",
				"wolf",
				"wood",
				"NPCKill_Reward",
				"ActivityRewardRate_minutes",
				"ActivityReward",
				"WelcomeMoney",
				"HappyHour_BeginHour",
				"HappyHour_EndHour"
				};
		//public List<string> Strings_itemList = new List<string> { "CustomPermissionName" };
		//private Strings strings = new Strings();
		private Rewards_Version rewardsversion = new Rewards_Version();
		private RewardRates rewardrates = new RewardRates();
		private Options options = new Options();
		private Multipliers multipliers = new Multipliers();

		private Dictionary<BasePlayer, int> LastReward = new Dictionary<BasePlayer, int>();

		private void OnServerInitialized()
		{
			if (options.UseEconomicsPlugin)
			{
				if (Economics != null)
					IsEconomicsLoaded = true;
				else
				{
					IsEconomicsLoaded = false;
					PrintWarning("Economics plugin was not found! Can't reward players using Economics.");
				}
			}

			if (options.UseServerRewardsPlugin)
			{
				if(ServerRewards != null)
					IsServerRewardsLoaded = true;
				else
				{
					IsServerRewardsLoaded = false;
					PrintWarning("ServerRewards plugin was not found! Can't reward players using ServerRewards.");
				}
			}
			if (options.UseFriendsPlugin)
			{
				if (Friends != null)
					IsFriendsLoaded = true;
				else
				{
					IsFriendsLoaded = false;
					PrintWarning("Friends plugin was not found! Can't check if victim is friend to killer.");
				}
			}

			if (options.UseClansPlugin)
			{
				if (Clans != null)
					IsClansLoaded = true;
				else
				{
					IsClansLoaded = false;
					PrintWarning("Clans plugin was not found! Can't check if victim is in the same clan of killer.");
				}
			}
		}

		protected override void LoadDefaultConfig()
		{
			PrintWarning("Creating a new configuration file");
			Config["Rewards_Version"] = rv;
			Config["Rewards"] = rr;
			Config["Multipliers"] = m;
			Config["Options"] = o;
			SaveConfig();
			LoadConfig();
		}

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["CollectReward"] = "You received {0}. Reward for collecting {1}",
				["KillReward"] = "You received {0}. Reward for killing {1}",
				["ActivityReward"] = "You received {0}. Reward for activity",
				["WelcomeReward"] = "Welcome to server! You received {0} as a welcome reward",
				["VictimNoMoney"] = "{0} doesn't have enough money.",
				["SetRewards"] = "Varaibles you can set:",
				["RewardSet"] = "Reward was set",
				["cactus"] = "Cactus",
				["ore"] = "Ore",
				["wood"] = "Wood",
				["stones"] = "Stones",
				["corn"] = "Corn",
				["hemp"] = "Hemp",
				["mushrooms"] = "Mushrooms",
				["pumpkin"] = "Pumpkin",
				["stag"] = "a stag",
				["boar"] = "a boar",
				["horse"] = "a horse",
				["bear"] = "a bear",
				["wolf"] = "a wolf",
				["chicken"] = "a chicken",
				["autoturret"] = "an autoturret",
				["helicopter"] = "a helicopter",
				["chinook"] = "a chinook CH47",
				["murderer"] = "a zombie (murderer)",
				["scientist"] = "a scientist",
				["npc"] = "a npc",
				["bradley"] = "a Bradley APC",
				["Prefix"] = "Rewards",
				["HappyHourStart"] = "Happy Hour(s) started",
				["HappyHourEnd"] = "Happy Hour(s) ended",
				["BarrelReward"] = "You received {0} for destroying a barrel!",
				["FoodBoxReward"] = "You received {0} for looting a food box!",
				["CrateReward"] = "You received {0} for looting a crate!",
				["SupplySignalReward"] = "You received {0} for looting a supply signal!",
				["MineCartReward"] = "You received {0} for looting a minecart!",
				["MsgOn"] = "Rewards Messages On.",
				["MsgOff"] = "Rewards Messages Off."
			}, this);
		}

		private void SetDefaultConfigValues()
		{
			//str = new Strings
			//{
			//    CustomPermissionName = "null"
			//};
			rv = new Rewards_Version
			{
				Version = this.Version.ToString()
			};
			rr = new RewardRates
			{
				autoturret = 10,
				barrel = 2,
				bear = 7,
				boar = 3,
				bradley = 50,
				cactus = 1,
				chicken = 1,
				chinook = 50,
				corn = 1,
				crate = 2,
				foodbox = 1,
				helicopter = 50,
				hemp = 1,
				horse = 2,
				human = 10,
				minecart = 2,
				mushrooms = 2,
				ore = 1,
				pumpkin = 1,
				murderer = 7,
				scientist = 8,
				stag = 2,
				stones = 1,
				supplysignal = 5,
				wolf = 8,
				wood = 1,
				NPCKill_Reward = 8,
				ActivityRewardRate_minutes = 30,
				ActivityReward = 15,
				WelcomeMoney = 50,
				HappyHour_BeginHour = 17,
				HappyHour_EndHour = 20
			};
			m = new Multipliers
			{
				AssaultRifle = 1.0,
				BeancanGrenade = 1.0,
				BoltActionRifle = 1.0,
				BoneClub = 1.0,
				BoneKnife = 1.0,
				CandyCaneClub = 1.0,
				CompoundBow = 1.0,
				Crossbow = 1.0,
				CustomSMG = 1.0,
				DoubleBarrelShotgun = 1.0,
				EokaPistol = 1.0,
				F1Grenade = 1.0,
				HandmadeFishingRod = 1.0,
				HuntingBow = 1,
				LR300 = 1.0,
				Longsword = 1.5,
				M249 = 1.0,
				M92Pistol = 1.0,
				MP5A4 = 1.0,
				Mace = 1.5,
				Machete = 1.5,
				NailGun = 1.1,
				PumpShotgun = 1,
				PythonRevolver = 1.0,
				Revolver = 1.0,
				RocketLauncher = 1.0,
				SalvagedCleaver = 1.5,
				SalvagedSword = 1.5,
				SatchelCharge = 1.0,
				SemiAutomaticPistol = 1.0,
				SemiAutomaticRifle = 1.0,
				Snowball = 2.0,
				Spas12Shotgun = 1.0,
				StoneSpear = 1.5,
				Thompson = 1.0,
				TimedExplosiveCharge = 1.0,
				WaterpipeShotgun = 1.0,
				WoodenSpear = 1.2,
				distance_50 = 1,
				distance_100 = 1.0,
				distance_200 = 1.25,
				distance_300 = 1.5,
				distance_400 = 2,
				HappyHourMultiplier = 2,
				VIPMultiplier = 2
			};

			o = new Options
			{
				ActivityReward_Enabled = true,
				WelcomeMoney_Enabled = true,
				UseEconomicsPlugin = true,
				UseServerRewardsPlugin = false,
				UseFriendsPlugin = true,
				UseClansPlugin = true,
				Economics_TakeMoneyFromVictim = false,
				ServerRewards_TakeMoneyFromVictim = false,
				WeaponMultiplier_Enabled = true,
				DistanceMultiplier_Enabled = true,
				PrintToConsole = true,
				HappyHour_Enabled = true,
				VIPMultiplier_Enabled = true,
				NPCReward_Enabled = true
			};
		}

		private void FixConfig()
		{
			try
			{
				Dictionary<string, object> temp;
				Dictionary<string, object> temp2;
				Dictionary<string, object> temp3;
				Dictionary<string, object> temp4;
				try { temp = (Dictionary<string, object>)Config["Rewards"]; } catch { Config["Rewards"] = rr; SaveConfig(); temp = (Dictionary<string, object>)Config["Rewards"]; }
				try { temp2 = (Dictionary<string, object>)Config["Options"]; } catch { Config["Options"] = o; SaveConfig(); temp2 = (Dictionary<string, object>)Config["Options"]; }
				try { temp3 = (Dictionary<string, object>)Config["Multipliers"]; } catch { Config["Multipliers"] = m; SaveConfig(); temp3 = (Dictionary<string, object>)Config["Multipliers"]; }
				//try { temp4 = (Dictionary<string, object>)Config["Strings"]; } catch { Config["Strings"] = str; SaveConfig(); temp4 = (Dictionary<string, object>)Config["Strings"]; Puts(temp4["CustomPermissionName"].ToString()); }
				foreach (var s in Rewards_itemList)
				{
					if (!temp.ContainsKey(s))
					{
						Config["Rewards", s] = rr.GetItemByString(s);
						SaveConfig();
					}
				}
				foreach (var s in Options_itemList)
				{
					if (!temp2.ContainsKey(s))
					{
						Config["Options", s] = o.GetItemByString(s);
						SaveConfig();
					}
				}
				foreach (var s in Multipliers_itemList)
				{
					if (!temp3.ContainsKey(s))
					{
						Config["Multipliers", s] = m.GetItemByString(s);
						SaveConfig();
					}
				}
				Config["Rewards_Version", "Version"] = this.Version.ToString();
				SaveConfig();
			}
			catch (Exception ex)
			{ Puts(ex.Message); Puts("Couldn't fix. Creating new config file"); Config.Clear(); LoadDefaultConfig(); Loadcfg(); }
		}

		private void Loadcfg()
		{
			SetDefaultConfigValues();
			try
			{
				Dictionary<string, object> temp = (Dictionary<string, object>)Config["Rewards_Version"];
				if (this.Version.ToString() != temp["Version"].ToString())
				{
					Puts("Outdated config file. Fixing");
					FixConfig();
				}
			}
			catch (Exception e)
			{
				Puts("Outdated config file. Fixing");
				FixConfig();
			}
			try
			{
				Dictionary<string, object> temp = (Dictionary<string, object>)Config["Rewards"];
				rewardrates.ActivityReward = Convert.ToDouble(temp["ActivityReward"]);
				rewardrates.ActivityRewardRate_minutes = Convert.ToDouble(temp["ActivityRewardRate_minutes"]);
				rewardrates.autoturret = Convert.ToDouble(temp["autoturret"]);
				rewardrates.barrel = Convert.ToDouble(temp["barrel"]);
				rewardrates.foodbox = Convert.ToDouble(temp["foodbox"]);
				rewardrates.crate = Convert.ToDouble(temp["crate"]);
				rewardrates.supplysignal = Convert.ToDouble(temp["supplysignal"]);
				rewardrates.minecart = Convert.ToDouble(temp["minecart"]);
				rewardrates.bear = Convert.ToDouble(temp["bear"]);
				rewardrates.boar = Convert.ToDouble(temp["boar"]);
				rewardrates.chicken = Convert.ToDouble(temp["chicken"]);
				rewardrates.corn = Convert.ToDouble(temp["corn"]);
				rewardrates.helicopter = Convert.ToDouble(temp["helicopter"]);
				rewardrates.chinook = Convert.ToDouble(temp["chinook"]);
				rewardrates.murderer = Convert.ToDouble(temp["murderer"]);
				rewardrates.scientist = Convert.ToDouble(temp["scientist"]);
				rewardrates.bradley = Convert.ToDouble(temp["bradley"]);
				rewardrates.hemp = Convert.ToDouble(temp["hemp"]);
				rewardrates.horse = Convert.ToDouble(temp["horse"]);
				rewardrates.human = Convert.ToDouble(temp["human"]);
				rewardrates.mushrooms = Convert.ToDouble(temp["mushrooms"]);
				rewardrates.cactus = Convert.ToDouble(temp["cactus"]);
				rewardrates.ore = Convert.ToDouble(temp["ore"]);
				rewardrates.pumpkin = Convert.ToDouble(temp["pumpkin"]);
				rewardrates.stag = Convert.ToDouble(temp["stag"]);
				rewardrates.stones = Convert.ToDouble(temp["stones"]);
				rewardrates.WelcomeMoney = Convert.ToDouble(temp["WelcomeMoney"]);
				rewardrates.wolf = Convert.ToDouble(temp["wolf"]);
				rewardrates.wood = Convert.ToDouble(temp["wood"]);
				rewardrates.HappyHour_BeginHour = Convert.ToDouble(temp["HappyHour_BeginHour"]);
				rewardrates.HappyHour_EndHour = Convert.ToDouble(temp["HappyHour_EndHour"]);
				rewardrates.NPCKill_Reward = Convert.ToDouble(temp["NPCKill_Reward"]);

				Dictionary<string, object> temp2 = (Dictionary<string, object>)Config["Options"];
				options.ActivityReward_Enabled = (bool)temp2["ActivityReward_Enabled"];
				options.DistanceMultiplier_Enabled = (bool)temp2["DistanceMultiplier_Enabled"];
				options.Economics_TakeMoneyFromVictim = (bool)temp2["Economics_TakeMoneyFromVictim"];
				options.ServerRewards_TakeMoneyFromVictim = (bool)temp2["ServerRewards_TakeMoneyFromVictim"];
				options.UseClansPlugin = (bool)temp2["UseClansPlugin"];
				options.UseEconomicsPlugin = (bool)temp2["UseEconomicsPlugin"];
				options.UseFriendsPlugin = (bool)temp2["UseFriendsPlugin"];
				options.UseServerRewardsPlugin = (bool)temp2["UseServerRewardsPlugin"];
				options.WeaponMultiplier_Enabled = (bool)temp2["WeaponMultiplier_Enabled"];
				options.WelcomeMoney_Enabled = (bool)temp2["WelcomeMoney_Enabled"];
				options.PrintToConsole = (bool)temp2["PrintToConsole"];
				options.VIPMultiplier_Enabled = (bool)temp2["VIPMultiplier_Enabled"];
				options.NPCReward_Enabled = (bool)temp2["NPCReward_Enabled"];
				options.HappyHour_Enabled = (bool)temp2["HappyHour_Enabled"];

				Dictionary<string, object> temp3 = (Dictionary<string, object>)Config["Multipliers"];
				multipliers.AssaultRifle = Convert.ToDouble(temp3["AssaultRifle"]);
				multipliers.BeancanGrenade = Convert.ToDouble(temp3["BeancanGrenade"]);
				multipliers.BoltActionRifle = Convert.ToDouble(temp3["BoltActionRifle"]);
				multipliers.BoneClub = Convert.ToDouble(temp3["BoneClub"]);
				multipliers.BoneKnife = Convert.ToDouble(temp3["BoneKnife"]);
				multipliers.CandyCaneClub = Convert.ToDouble(temp3["CandyCaneClub"]);
				multipliers.CompoundBow = Convert.ToDouble(temp3["CompoundBow"]);
				multipliers.Crossbow = Convert.ToDouble(temp3["Crossbow"]);
				multipliers.CustomSMG = Convert.ToDouble(temp3["CustomSMG"]);
				multipliers.DoubleBarrelShotgun = Convert.ToDouble(temp3["DoubleBarrelShotgun"]);
				multipliers.EokaPistol = Convert.ToDouble(temp3["EokaPistol"]);
				multipliers.F1Grenade = Convert.ToDouble(temp3["F1Grenade"]);
				multipliers.HandmadeFishingRod = Convert.ToDouble(temp3["HandmadeFishingRod"]);
				multipliers.HuntingBow = Convert.ToDouble(temp3["HuntingBow"]);
				multipliers.LR300 = Convert.ToDouble(temp3["LR300"]);
				multipliers.Longsword = Convert.ToDouble(temp3["Longsword"]);
				multipliers.M249 = Convert.ToDouble(temp3["M249"]);
				multipliers.M92Pistol = Convert.ToDouble(temp3["M92Pistol"]);
				multipliers.MP5A4 = Convert.ToDouble(temp3["MP5A4"]);
				multipliers.Mace = Convert.ToDouble(temp3["Mace"]);
				multipliers.Machete = Convert.ToDouble(temp3["Machete"]);
				multipliers.NailGun = Convert.ToDouble(temp3["NailGun"]);
				multipliers.PumpShotgun = Convert.ToDouble(temp3["PumpShotgun"]);
				multipliers.PythonRevolver = Convert.ToDouble(temp3["PythonRevolver"]);
				multipliers.Revolver = Convert.ToDouble(temp3["Revolver"]);
				multipliers.RocketLauncher = Convert.ToDouble(temp3["RocketLauncher"]);
				multipliers.SalvagedCleaver = Convert.ToDouble(temp3["SalvagedCleaver"]);
				multipliers.SalvagedSword = Convert.ToDouble(temp3["SalvagedSword"]);
				multipliers.SatchelCharge = Convert.ToDouble(temp3["SatchelCharge"]);
				multipliers.SemiAutomaticPistol = Convert.ToDouble(temp3["SemiAutomaticPistol"]);
				multipliers.SemiAutomaticRifle = Convert.ToDouble(temp3["SemiAutomaticRifle"]);
				multipliers.Snowball = Convert.ToDouble(temp3["Snowball"]);
				multipliers.Spas12Shotgun = Convert.ToDouble(temp3["Spas12Shotgun"]);
				multipliers.StoneSpear = Convert.ToDouble(temp3["StoneSpear"]);
				multipliers.Thompson = Convert.ToDouble(temp3["Thompson"]);
				multipliers.TimedExplosiveCharge = Convert.ToDouble(temp3["TimedExplosiveCharge"]);
				multipliers.WaterpipeShotgun = Convert.ToDouble(temp3["WaterpipeShotgun"]);
				multipliers.WoodenSpear = Convert.ToDouble(temp3["WoodenSpear"]);

				multipliers.HappyHourMultiplier = Convert.ToDouble(temp3["HappyHourMultiplier"]);
				multipliers.distance_50 = Convert.ToDouble(temp3["distance_50"]);
				multipliers.distance_100 = Convert.ToDouble(temp3["distance_100"]);
				multipliers.distance_200 = Convert.ToDouble(temp3["distance_200"]);
				multipliers.distance_300 = Convert.ToDouble(temp3["distance_300"]);
				multipliers.distance_400 = Convert.ToDouble(temp3["distance_400"]);
				multipliers.VIPMultiplier = Convert.ToDouble(temp3["VIPMultiplier"]);

				//Dictionary<string, object> temp4 = (Dictionary<string, object>)Config["Strings"];
				//str.CustomPermissionName = temp4["CustomPermissionName"].ToString();
				if (rewardrates.HappyHour_EndHour < rewardrates.HappyHour_BeginHour)
					HappyHourCross24 = true;
				else
					HappyHourCross24 = false;
			}
			catch
			{
				FixConfig();
				Loadcfg();
			}
		}

		private void Init()
		{
			permission.RegisterPermission("rustrewards.admin", this);
			permission.RegisterPermission("rustrewards.vip", this);
			permission.RegisterPermission("rustrewards.showrewards", this);

			playerPrefs = dataFile.ReadObject<Dictionary<string, string>>();

			Loadcfg();
			heliLastHitPlayer = null;
			chinookLastHitPlayer = null;
			bradleyLastHitPlayer = null;

			#region Activity Check
			if (options.ActivityReward_Enabled || options.HappyHour_Enabled)
			{
			  timeCheck = timer.Once(20, CheckCurrentTime);
			}
			#endregion
		}

		private void CheckCurrentTime()
		{
			var gtime = TOD_Sky.Instance.Cycle.Hour;
			if (options.ActivityReward_Enabled)
			{
				foreach (var p in BasePlayer.activePlayerList)
				{
					if (Convert.ToDouble(p.secondsConnected) / 60 > rewardrates.ActivityRewardRate_minutes)
					{
						if (LastReward.ContainsKey(p))
						{
							if (Convert.ToDouble(p.secondsConnected - LastReward[p]) / 60 > rewardrates.ActivityRewardRate_minutes)
							{
								RewardPlayer(p, rewardrates.ActivityReward);
								LastReward[p] = p.secondsConnected;
							}
						}
						else
						{
							RewardPlayer(p, rewardrates.ActivityReward);
							LastReward.Add(p, p.secondsConnected);
						}
					}
				}
			}
			if (options.HappyHour_Enabled)
			{
				if (!HappyHourActive)
				{
					if ((HappyHourCross24 == false &&   gtime >= rewardrates.HappyHour_BeginHour && gtime < rewardrates.HappyHour_EndHour) ||
						(HappyHourCross24 == true  && ((gtime >= rewardrates.HappyHour_BeginHour && gtime < 24) || gtime < rewardrates.HappyHour_EndHour))
						)
					{
						HappyHourActive = true;
						Puts("Happy hour(s) started.  Ending at " + rewardrates.HappyHour_EndHour);
						BroadcastMessage(Lang("HappyHourStart"), Lang("Prefix"));
					}
				}
				else
				{
					if ((HappyHourCross24 == false &&  gtime >= rewardrates.HappyHour_EndHour) ||
						(HappyHourCross24 == true  && (gtime <  rewardrates.HappyHour_BeginHour && gtime >= rewardrates.HappyHour_EndHour))
						)
					{
						HappyHourActive = false;
						Puts("Happy Hour(s) ended.  Next Happy Hour(s) starts at " + rewardrates.HappyHour_BeginHour);
						BroadcastMessage(Lang("HappyHourEnd"), Lang("Prefix"));
					}
				}
			}
			timeCheck = timer.Once(20, CheckCurrentTime);
		}

		private void OnPlayerInit(BasePlayer player)
		{
			if (options.WelcomeMoney_Enabled == false || player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;
			else
			{
				if (!storedData.Players.Contains(player.UserIDString))
				{
					RewardPlayer(player, rewardrates.WelcomeMoney, 1, null, true);
					storedData.Players.Add(player.UserIDString);
					playerPrefs[player.UserIDString] = "true";
					dataFile.WriteObject(playerPrefs);
				}
			}
		}

		private void Unload()
		{
			if (timeCheck != null)
				timeCheck.Destroy();
		}

		string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

		bool HasPerm(BasePlayer p, string pe) => permission.UserHasPermission(p.userID.ToString(), pe);

		private void SendChatMessage(BasePlayer player, string msg, string prefix = null, object uid = null) => rust.SendChatMessage(player, prefix == null ? msg : "<color=#C4FF00>" + prefix + "</color>: ", msg, uid?.ToString() ?? "0");

		private void BroadcastMessage(string msg, string prefix = null, object uid = null) => rust.BroadcastChat(prefix == null ? msg : "<color=#C4FF00>" + prefix + "</color>: ", msg);

		private void MessagePlayer(BasePlayer player, string msg, string prefix)
		{
			string message = null;

			if (player == null || msg == null || player.UserIDString == null || player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;
			else
			try
			{
				playerPrefs.TryGetValue(player.UserIDString, out message);
			}
			catch
			{
				message = null;
			}
			if (message == null || (message != "true" && message != "false"))
			{
				message = "true";
				playerPrefs[player.UserIDString] = message;
				dataFile.WriteObject(playerPrefs);
			}

			if (message == "true")
				SendChatMessage(player, msg, prefix);
		}

		private void OnKillNPC(BasePlayer victim, HitInfo info)
		{
			if (options.NPCReward_Enabled && victim != null && (victim is BaseNpc || victim is NPCPlayerApex || victim is NPCPlayer || victim is NPCMurderer))
			{
				if (info?.Initiator?.ToPlayer() == null) return;
				double totalmultiplier = 1;

				if (options.DistanceMultiplier_Enabled || options.WeaponMultiplier_Enabled)
					totalmultiplier = (options.DistanceMultiplier_Enabled ? multipliers.GetDistanceM(victim.Distance2D(info?.Initiator?.ToPlayer())) : 1) *
				                      (options.WeaponMultiplier_Enabled ? multipliers.GetWeaponM(info?.Weapon?.GetItem()?.info?.displayName?.english) : 1) *
				                      (HappyHourActive ? multipliers.HappyHourMultiplier : 1) *
									  ((options.VIPMultiplier_Enabled && HasPerm(info?.Initiator?.ToPlayer(), "rewards.vip")) ? multipliers.VIPMultiplier : 1);

				RewardPlayer(info?.Initiator?.ToPlayer(), rewardrates.NPCKill_Reward, totalmultiplier, victim.displayName);
			}
		}

		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
			if (!Economics && !ServerRewards) return;

			BasePlayer player = entity.ToPlayer();
			if (player == null && !player.IsConnected) return;
			if (player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;

			double rewardmoney = 0;
			double totalmultiplier = 1;
			string shortName = item.info.shortname;
			string resource = null;

			if (shortName != null)
			{
				if (shortName.Contains(".ore"))
				{
					rewardmoney = rewardrates.ore;
					resource = "ore";
				}
				else if (shortName.Contains("stones"))
				{
					rewardmoney = rewardrates.stones;
					resource = "stones";
				}
				else if (shortName.Contains("cactus"))
				{
					rewardmoney = rewardrates.cactus;
					resource = "cactus";
				}
				else if (dispenser.GetComponent<BaseEntity>() is TreeEntity ||
						 shortName.Contains("driftwood") ||
						 shortName.Contains("douglas") ||
						 shortName.Contains("fir") ||
						 shortName.Contains("birch") ||
						 shortName.Contains("oak") ||
						 shortName.Contains("pine") ||
						 shortName.Contains("juniper") ||
						 shortName.Contains("deadtree") ||
						 shortName.Contains("log") ||
						 shortName.Contains("tree_marking") ||
						 shortName.Contains("palm"))
				{
					rewardmoney = rewardrates.wood;
					resource = "wood";
				}

				if (resource != null && rewardmoney != 0 && dispenser.gameObject.ToBaseEntity().net.ID != null &&
				    !EntityCollectionCache.ContainsKey(dispenser.gameObject.ToBaseEntity().net.ID))
				{
					EntityCollectionCache.Add(dispenser.gameObject.ToBaseEntity().net.ID, player);
					//Puts (resource + " : " + player.UserIDString);
					//totalmultiplier = ((HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(player, "rewards.vip")) ? multipliers.VIPMultiplier : 1);
					//RewardPlayer(player, rewardmoney, totalmultiplier, Lang(resource, player.UserIDString));
				}
			}
		}

		private void OnCollectiblePickup(Item item, BasePlayer player)
		{
			if (!Economics && !ServerRewards) return;
			if (player == null && !player.IsConnected) return;
			if (player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;

			double rewardmoney = 0;
			double totalmultiplier = 1;
			string shortName = item.info.shortname;
			string resource = null;

			if (shortName.Contains("stones"))
			{
				rewardmoney = rewardrates.stones;
				resource = "stones";
			}
			else if (shortName.Contains(".ore"))
			{
				rewardmoney = rewardrates.ore;
				resource = "ore";
			}
			else if (shortName.Contains("wood"))
			{
				rewardmoney = rewardrates.wood;
				resource = "wood";
			}
			else if (shortName.Contains("mushroom"))
			{
				rewardmoney = rewardrates.mushrooms;
				resource = "mushrooms";
			}
			else if (shortName.Contains("seed.corn"))
			{
				rewardmoney = rewardrates.corn;
				resource = "corn";
			}
			else if (shortName.Contains("seed.hemp"))
			{
				rewardmoney = rewardrates.hemp;
				resource = "hemp";
			}
			else if (shortName.Contains("seed.pumpkin"))
			{
				rewardmoney = rewardrates.pumpkin;
				resource = "pumpkin";
			}

			if (resource != null && rewardmoney != 0)
			{
				totalmultiplier = (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(player, "rewards.vip")) ? multipliers.VIPMultiplier : 1);
				RewardPlayer(player, rewardmoney, totalmultiplier, Lang(resource, player.UserIDString));
			}
		}

		void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info.Initiator == null) return;
			if (!(info.Initiator is BasePlayer) || info.Initiator is BaseNpc || info.Initiator is NPCPlayerApex || info.Initiator is NPCPlayer || info.Initiator is NPCMurderer) return;

			// used to track who killed it. last hit wins ;-)
			if (entity is BaseHelicopter && info.Initiator is BasePlayer)
				heliLastHitPlayer = info.Initiator.ToPlayer();

			if (entity is CH47HelicopterAIController && info.Initiator is BasePlayer)
				chinookLastHitPlayer = info.Initiator.ToPlayer();

			if (entity is BradleyAPC && info.Initiator is BasePlayer)
				bradleyLastHitPlayer = info.Initiator.ToPlayer();
		}

		private void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			BasePlayer ECEplayer = null;

			if (!Economics && !ServerRewards) return;
			if (entity.net.ID == null || entity.ShortPrefabName == null) return;
			if (player == null || !player.IsConnected || player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;
			if (!(entity.ShortPrefabName.Contains("crate_") || entity.ShortPrefabName.Contains("_crate") ||
				 entity.ShortPrefabName.Contains("foodbox") || entity.ShortPrefabName.Contains("trash-pile") ||
				 entity.ShortPrefabName.Contains("minecart") || entity.ShortPrefabName.Contains("supply")))
				 return;

			if (EntityCollectionCache.ContainsKey(entity.net.ID))
			{
				if (EntityCollectionCache.TryGetValue(entity.net.ID, out ECEplayer))
					{
						if (ECEplayer == null || ECEplayer != player)
							EntityCollectionCache[entity.net.ID] = player;
					}
			}
			else
			{
				EntityCollectionCache.Add(entity.net.ID, player);
			}
		}

		private void OnEntityKill(BaseNetworkable entity)
		{
			BasePlayer player = null;
			double rewardmoney = 0;
			double totalmultiplier = 1;
			string resource = null;
			bool   key_found = false;

			//if (!(entity.ShortPrefabName.Contains("planner") ||
			//	 entity.ShortPrefabName.Contains("junkpile") ||
			//	 entity.ShortPrefabName.Contains("divesite") ||
			//	 entity.ShortPrefabName.Contains("barrel") ||
			//	 entity.ShortPrefabName.Contains("hammer") ||
			//	 entity.ShortPrefabName.Contains("jug") ||
			//	 entity.ShortPrefabName.Contains("salvage") ||
			//	 entity.ShortPrefabName.Contains("generic") ||
			//	 entity.ShortPrefabName.Contains("bow") ||
			//	 entity.ShortPrefabName.Contains("boat") ||
			//	 entity.ShortPrefabName.Contains("rhib") ||
			//	 entity.ShortPrefabName.Contains("fuel") ||
			//	 entity.ShortPrefabName.Contains("foodbox") ||
			//	 entity.ShortPrefabName.Contains("crate") ||
			//	 entity.ShortPrefabName.Contains("supply") ||
			//	 entity.ShortPrefabName.Contains("entity") ||
			//	 entity.ShortPrefabName.Contains("weapon")))
			//{
			//	Puts("OEK:" + entity.ShortPrefabName);
			//}
			if (entity == null || entity.ShortPrefabName == null || entity.net.ID == null)
				return;
			else
			{
				try
				{
					key_found = (EntityCollectionCache.ContainsKey(entity.net.ID));
				}
				catch {key_found = false; }

				if (key_found)
				{
					try
					{
					if (EntityCollectionCache.TryGetValue(entity.net.ID, out player))
					{
						if (player == null || !player.IsConnected) return;
						else if (entity.ShortPrefabName.Contains("-ore") || entity.ShortPrefabName.Contains(".ore"))
						{
							rewardmoney = rewardrates.ore;
							resource = "ore";
						}
						else if (entity.ShortPrefabName.Contains("stones"))
						{
							rewardmoney = rewardrates.stones;
							resource = "stones";
						}
						else if (entity.ShortPrefabName.Contains("cactus"))
						{
							rewardmoney = rewardrates.cactus;
							resource = "cactus";
						}
						else if (entity.ShortPrefabName.Contains("driftwood") ||
								 entity.ShortPrefabName.Contains("douglas_fir") ||
								 entity.ShortPrefabName.Contains("birch") ||
								 entity.ShortPrefabName.Contains("oak") ||
								 entity.ShortPrefabName.Contains("pine") ||
								 entity.ShortPrefabName.Contains("juniper") ||
								 entity.ShortPrefabName.Contains("deadtree") ||
								 entity.ShortPrefabName.Contains("dead_log") ||
								 entity.ShortPrefabName.Contains("palm"))
						{
							rewardmoney = rewardrates.wood;
							resource = "wood";
						}
						else if (entity.ShortPrefabName.Contains("minecart"))
						{
							rewardmoney = rewardrates.minecart;
							resource = "minecart";
						}
						else if (entity.ShortPrefabName.Contains("supply"))
						{
							rewardmoney = rewardrates.supplysignal;
							resource = "supplysignal";
						}
						else if (entity.ShortPrefabName.Contains("crate_") || entity.ShortPrefabName.Contains("_crate"))
						{
							rewardmoney = rewardrates.crate;
							resource = "crate";
						}
						else if (entity.ShortPrefabName.Contains("foodbox") || entity.ShortPrefabName.Contains("trash-pile"))
						{
							rewardmoney = rewardrates.foodbox;
							resource = "foodbox";
						}

						if (player != null && resource != null && rewardmoney != 0)
						{
							totalmultiplier = (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(player, "rewards.vip")) ? multipliers.VIPMultiplier : 1);
							RewardPlayer(player, rewardmoney, totalmultiplier, Lang(resource, player.UserIDString));
						}
					}
					}
					catch {Puts("Tell MSPEEDIE Error on OKE Try Get Value");}
					try
					{
						EntityCollectionCache.Remove(entity.net.ID);
					}
					catch {Puts("Tell MSpeedie Error on OKE Remove");}
				}
			}
		}

		private void OnEntityDeath(BaseCombatEntity victim, HitInfo info)
		{
			var player = info?.Initiator?.ToPlayer();
			double rewardmoney = 0;
			double totalmultiplier = 1;
			string resource = null;

			if (!Economics && !ServerRewards) return;
			if (victim == null) return;

			if (player == null)
			{
				// check is special case
				if (victim is BaseHelicopter && heliLastHitPlayer != null)
				{
					player = heliLastHitPlayer;
					heliLastHitPlayer = null;
				}
				if (victim is CH47HelicopterAIController && chinookLastHitPlayer != null)
				{
					player = chinookLastHitPlayer;
					chinookLastHitPlayer = null;
				}
				if (victim is BradleyAPC && bradleyLastHitPlayer != null)
				{
					player = bradleyLastHitPlayer;
					bradleyLastHitPlayer = null;
				}
				else
					return;  // no one tracked as killing it
			}

			if (player == null || player.UserIDString == null || player.UserIDString == String.Empty || player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer)
				return;

			if (victim.name.Contains("servergibs") || victim.name.Contains("corpse")) return;  // no money for cleaning up the left over crash/corpse

			if (victim.name.Contains("loot-barrel") || victim.name.Contains("loot_barrel") || victim.name.Contains("oil_barrel"))
			{
				rewardmoney = rewardrates.barrel;
				resource = "barrel";
			}
			else if (victim.name.Contains("log"))
			{
				rewardmoney = rewardrates.wood;
				resource = "wood";
			}
			else if (victim.name.Contains("assets/rust.ai/agents/") && !victim.name.Contains("corpse"))
			{
				if ( victim.name.Contains("stag"))
				{
					rewardmoney = rewardrates.stag;
					resource = "stag";
				}
				else if (victim.name.Contains("boar"))
				{
					rewardmoney = rewardrates.boar;
					resource = "boar";
				}
				else if (victim.name.Contains("horse"))
				{
					rewardmoney = rewardrates.horse;
					resource = "horse";
				}
				else if (victim.name.Contains("bear"))
				{
					rewardmoney = rewardrates.bear;
					resource = "bear";
				}
				else if (victim.name.Contains("wolf"))
				{
					rewardmoney = rewardrates.wolf;
					resource = "wolf";
				}
				else if (victim.name.Contains("chicken"))
				{
					rewardmoney = rewardrates.chicken;
					resource = "chicken";
				}
				else if (victim.name.Contains("zombie")) // lumped these in with Murderers
				{
					if (!options.NPCReward_Enabled)
						return;
					rewardmoney = rewardrates.murderer;
					resource = "murderer";
				}
				else
				{
					Puts("Tell Mals: OED missing animal: " + victim.name);
				}
			}
			else if (victim is BaseNpc || victim is NPCPlayerApex || victim is NPCPlayer || victim is Scientist || victim is NPCMurderer)
			{
				if (!options.NPCReward_Enabled)
					return;
				if (victim is Scientist)
				{
					rewardmoney = rewardrates.scientist;
					resource = "scientist";
				}
				else if (victim is NPCMurderer)
				{
					rewardmoney = rewardrates.murderer;
					resource = "murderer";
				}
				else
				{
					rewardmoney = rewardrates.NPCKill_Reward;
					resource = "npc";
				}
			}
			else if (victim.ToPlayer() != null)
			{
				if (player.userID == victim.ToPlayer().userID)
					return;
				else
				{
					RewardForPlayerKill(player, victim.ToPlayer(), totalmultiplier);
					return;
				}
			}
			else if (victim is BaseHelicopter || victim.name.Contains("patrolhelicopter.prefab"))
			{
				rewardmoney = rewardrates.helicopter;
				resource = "helicopter";
			}
			else if (victim is BradleyAPC || victim.name.Contains("bradleyapc.prefab"))
			{
				rewardmoney = rewardrates.bradley;
				resource = "bradley";
			}
			else if (victim is CH47HelicopterAIController || victim.name.Contains("ch47.prefab"))
			{
				rewardmoney = rewardrates.chinook;
				resource = "chinook";
			}
			else if (victim.name == "assets/prefabs/npc/autoturret/autoturret_deployed.prefab")
			{
				rewardmoney = rewardrates.autoturret;
				resource = "autoturret";
			}

			if ((resource != "barrel" && resource != "wood") && (options.DistanceMultiplier_Enabled || options.WeaponMultiplier_Enabled))
				totalmultiplier = (options.DistanceMultiplier_Enabled ? multipliers.GetDistanceM(victim.Distance2D(player)) : 1) *
			                      (options.WeaponMultiplier_Enabled ? multipliers.GetWeaponM(info?.Weapon?.GetItem()?.info?.displayName?.english) : 1) *
			                      (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(player, "rewards.vip")) ? multipliers.VIPMultiplier : 1);
			else
				totalmultiplier = (HappyHourActive ? multipliers.HappyHourMultiplier : 1) * ((options.VIPMultiplier_Enabled && HasPerm(player, "rewards.vip")) ? multipliers.VIPMultiplier : 1);

			if (player != null && rewardmoney >= 0.01 && totalmultiplier > 0.01 && resource != null)
				RewardPlayer(player, rewardmoney, totalmultiplier, Lang(resource, player.UserIDString));
			else
				Puts("Tell Mals to check: V/R/R/T/P: " + victim.name + " : " + resource + " : " + rewardmoney + " : " + totalmultiplier + " : " + player.displayName);

		}

		private void RewardPlayer(BasePlayer player, double amount, double multiplier = 1, string reason = null, bool isWelcomeReward = false)
		{
			// safety checks
			if (player == null || amount == null || multiplier == null || amount < 0.01  || multiplier < 0.001 || player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer) return;

			// if the IsConnected does not work then use these
			if (player.IsConnected)
			{
				amount = amount * multiplier;
				//  these use to be both if but it seems odd to me to pay in two currencies at the same rate
				if (options.UseEconomicsPlugin)
				{
					Economics?.Call("Deposit", player.UserIDString, amount);
				}
				else if (options.UseServerRewardsPlugin)
				{
					amount = Math.Round(amount,0);
					ServerRewards?.Call("AddPoints", player.userID, (int)amount);
				}

				if (isWelcomeReward)
				{
					MessagePlayer(player, Lang("WelcomeReward", player.UserIDString, amount.ToString("C", CurrencyCulture)), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " as a welcome reward", this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " as a welcome reward");
				}
				else if (reason == Lang("ore", player.UserIDString) ||
						 reason == Lang("wood", player.UserIDString) ||
						 reason == Lang("stones", player.UserIDString) ||
						 reason == Lang("corn", player.UserIDString) ||
						 reason == Lang("hemp", player.UserIDString) ||
						 reason == Lang("mushrooms", player.UserIDString) ||
						 reason == Lang("cactus", player.UserIDString) ||
						 reason == Lang("pumpkin", player.UserIDString)
						 )
				{
					MessagePlayer(player, Lang("CollectReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "collecting " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "collecting " + reason));
				}
				else if (reason == Lang("barrel", player.UserIDString))
				{
					MessagePlayer(player, Lang("BarrelReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "breaking " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "breaking " + reason));
				}
				else if (reason == Lang("crate", player.UserIDString))
				{
					MessagePlayer(player, Lang("CrateReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "opening " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "opening " + reason));
				}
				else if (reason == Lang("foodbox", player.UserIDString))
				{
					MessagePlayer(player, Lang("FoodBoxReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "opening " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "opening " + reason));
				}
				else if (reason == Lang("supplysignal", player.UserIDString))
				{
					MessagePlayer(player, Lang("SupplySignalReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "opening " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "opening " + reason));
				}
				else if (reason == Lang("minecart", player.UserIDString))
				{
					MessagePlayer(player, Lang("MineCartReward", player.UserIDString, amount.ToString("C", CurrencyCulture), reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "opening " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "opening " + reason));
				}
				else
				{
					MessagePlayer(player, reason == null ? Lang("ActivityReward", player.UserIDString, amount.ToString("C", CurrencyCulture)) : Lang("KillReward", player.UserIDString, amount, reason), Lang("Prefix"));
					LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + amount.ToString("C", CurrencyCulture) + " for " + (reason == null ? "activity" : "killing " + reason), this);
					if (options.PrintToConsole)
						Puts(player.displayName + " got " + amount + " for " + (reason == null ? "activity" : "killing " + reason));
				}
			}
		}

		private void RewardForPlayerKill(BasePlayer player, BasePlayer victim, double multiplier = 1)
		{

			// safety checks
			if (player == null || victim == null || multiplier == null || rewardrates.human == null || multiplier < 0.001 || rewardrates.human <= 0.01 ||
			    player is BaseNpc || player is NPCPlayerApex || player is NPCPlayer || player is NPCMurderer ||
			    victim is BaseNpc || victim is NPCPlayerApex || victim is NPCPlayer || victim is NPCMurderer) return;

			if (player.IsConnected)
			{
				bool success = true;
				bool isFriend = false;
				if (IsFriendsLoaded)
					isFriend = (bool)Friends?.CallHook("HasFriend", player.userID, victim.userID);
				if (!isFriend && IsClansLoaded)
				{
					string pclan = (string)Clans?.CallHook("GetClanOf", player);
					string vclan = (string)Clans?.CallHook("GetClanOf", victim);
					if (pclan == vclan)
						isFriend = true;
				}
				if (!isFriend)
				{
					if (IsEconomicsLoaded) // Economics
					{
						if (options.Economics_TakeMoneyFromVictim)
						{
							if (!(bool)Economics?.Call("Transfer", victim.UserIDString, player.UserIDString, rewardrates.human * multiplier))
							{
								MessagePlayer(player, Lang("VictimNoMoney", player.UserIDString, victim.displayName), Lang("Prefix"));
								success = false;
							}
						}
						else
							Economics?.Call("Deposit", player.UserIDString, rewardrates.human * multiplier);
					}
					if (IsServerRewardsLoaded) //ServerRewards
					{
						if (options.ServerRewards_TakeMoneyFromVictim)
							ServerRewards?.Call("TakePoints", new object[] { victim.userID, rewardrates.human * multiplier });
						ServerRewards?.Call("AddPoints", player.userID, (int)(rewardrates.human * multiplier));
						success = true;
					}
					if (success) //Send message if transaction was successful
					{
						MessagePlayer(player, Lang("KillReward", player.UserIDString, rewardrates.human * multiplier, victim.displayName), Lang("Prefix"));
						LogToFile(Name, $"[{DateTime.Now}] " + player.displayName + " got " + rewardrates.human * multiplier + " for killing " + victim.displayName, this);
						if (options.PrintToConsole)
							Puts(player.displayName + " got " + rewardrates.human * multiplier + " for killing " + victim.displayName);
					}
				}
			}
		}

		[ConsoleCommand("setrustrewards")]
		private void setreward(ConsoleSystem.Arg arg)
		{
			if (arg.IsAdmin)
			{
				try
				{
					var args = arg.Args;
					Config["Rewards", args[0]] = Convert.ToDouble(args[1]);
					SaveConfig();
					try
					{
						Loadcfg();
					}
					catch
					{
						FixConfig();
					}
					arg.ReplyWith("Reward set");
				}
				catch
				{
					arg.ReplyWith("Varaibles you can set: 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'chinook', 'bradley', 'autoturret', 'ActivityReward' 'ActivityRewardRate_minutes', 'WelcomeMoney'");
				}
			}
		}

		[ConsoleCommand("showrustrewards")]
		private void showrewards(ConsoleSystem.Arg arg)
		{
			if (arg.IsAdmin)
				arg.ReplyWith(String.Format("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, chinook = {8}, bradley = {9}, autoturret = {10} Activity Reward Rate (minutes) = {11}, Activity Reward = {12}, WelcomeMoney = {13}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.chinook, rewardrates.bradley, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney));
		}

		[ChatCommand("rustrewardsmsg")]
		private void ChatCommand(BasePlayer player, string command, string[] args)
		{
			string message = "false";

			try
			{
			playerPrefs.TryGetValue(player.UserIDString, out message);
			}
			catch
			{
				message = "false";
			}

			if (message == "true")
			{
				message = "false";
				playerPrefs[player.UserIDString] = message;
				dataFile.WriteObject(playerPrefs);
				SendChatMessage(player, Lang("MsgOff", player.UserIDString), Lang("Prefix"));
			}
			else
			{
				message = "true";
				playerPrefs[player.UserIDString] = message;
				dataFile.WriteObject(playerPrefs);
				SendChatMessage(player, Lang("MsgOn", player.UserIDString), Lang("Prefix"));
			}
		}

		[ChatCommand("setrustrewards")]
		private void setrewardCommand(BasePlayer player, string command, string[] args)
		{
			if (HasPerm(player, "rewards.admin"))
			{
				try
				{
					Config["Rewards", args[0]] = Convert.ToDouble(args[1]);
					SaveConfig();
					try
					{
						Loadcfg();
					}
					catch
					{
						FixConfig();
					}
					MessagePlayer(player, Lang("RewardSet", player.UserIDString), Lang("Prefix"));
					if (rewardrates.HappyHour_EndHour < rewardrates.HappyHour_BeginHour)
						HappyHourCross24 = true;
					else
						HappyHourCross24 = false;
				}
				catch
				{
					MessagePlayer(player, Lang("SetRewards", player.UserIDString) + " 'human', 'horse', 'wolf', 'chicken', 'bear', 'boar', 'stag', 'helicopter', 'chinook', 'bradley', 'autoturret', 'ActivityReward', 'ActivityRewardRate_minutes', 'WelcomeMoney'", Lang("Prefix"));
				}
			}
		}

		[ChatCommand("showrustrewards")]
		private void showrewardsCommand(BasePlayer player, string command, string[] args)
		{
			if (HasPerm(player, "rewards.showrewards"))
				MessagePlayer(player, String.Format("human = {0}, horse = {1}, wolf = {2}, chicken = {3}, bear = {4}, boar = {5}, stag = {6}, helicopter = {7}, chinook = {8}, bradley = {9}, autoturret = {10} Activity Reward Rate (minutes) = {11}, Activity Reward = {12}, WelcomeMoney = {13}", rewardrates.human, rewardrates.horse, rewardrates.wolf, rewardrates.chicken, rewardrates.bear, rewardrates.boar, rewardrates.stag, rewardrates.helicopter, rewardrates.chinook, rewardrates.bradley, rewardrates.autoturret, rewardrates.ActivityRewardRate_minutes, rewardrates.ActivityReward, rewardrates.WelcomeMoney), Lang("Prefix"));
		}

		class StoredData
		{
			public HashSet<string> Players = new HashSet<string>();
			public StoredData() { }
		}

		class RewardRates
		{
			public double autoturret{ get; set; }
			public double barrel{ get; set; }
			public double bear{ get; set; }
			public double boar{ get; set; }
			public double bradley{ get; set; }
			public double cactus{ get; set; }
			public double chicken{ get; set; }
			public double chinook{ get; set; }
			public double corn{ get; set; }
			public double crate{ get; set; }
			public double foodbox{ get; set; }
			public double helicopter{ get; set; }
			public double hemp{ get; set; }
			public double horse{ get; set; }
			public double human{ get; set; }
			public double minecart{ get; set; }
			public double mushrooms{ get; set; }
			public double ore{ get; set; }
			public double pumpkin{ get; set; }
			public double murderer{ get; set; }
			public double scientist{ get; set; }
			public double stag{ get; set; }
			public double stones{ get; set; }
			public double supplysignal{ get; set; }
			public double wolf{ get; set; }
			public double wood{ get; set; }
			public double NPCKill_Reward{ get; set; }
			public double ActivityRewardRate_minutes{ get; set; }
			public double ActivityReward{ get; set; }
			public double WelcomeMoney{ get; set; }
			public double HappyHour_BeginHour{ get; set; }
			public double HappyHour_EndHour{ get; set; }
			public double GetItemByString(string itemName)
			{
				if (itemName.StartsWith("loot-barrel") || itemName.StartsWith("loot_barrel") || itemName.StartsWith("oil-barrel"))
					return this.barrel;
				if (itemName.Contains("crate_") || itemName.Contains("_crate"))
					return this.crate;
				if (itemName.Contains("foodbox"))
					return this.foodbox;
				if (itemName.Contains("supplysignal"))
					return this.supplysignal;
				if (itemName.Contains("minecart"))
					return this.minecart;
				if (itemName == "human")
					return this.human;
				else if (itemName == "bear")
					return this.bear;
				else if (itemName == "wolf")
					return this.wolf;
				else if (itemName == "chicken")
					return this.chicken;
				else if (itemName == "horse")
					return this.horse;
				else if (itemName == "boar")
					return this.boar;
				else if (itemName == "stag")
					return this.stag;
				else if (itemName.Contains("cactus"))
					return this.cactus;
				else if (itemName == "ore")
					return this.ore;
				else if (itemName == "wood")
					return this.wood;
				else if (itemName == "stones")
					return this.stones;
				else if (itemName == "corn")
					return this.corn;
				else if (itemName == "hemp")
					return this.hemp;
				else if (itemName == "mushrooms")
					return this.mushrooms;
				else if (itemName == "pumpkin")
					return this.pumpkin;
				else if (itemName == "helicopter")
					return this.helicopter;
				else if (itemName == "chinook")
					return this.chinook;
				else if (itemName == "murderer")
					return this.murderer;
				else if (itemName == "scientist")
					return this.scientist;
				else if (itemName == "bradley")
					return this.bradley;
				else if (itemName == "autoturret")
					return this.autoturret;
				else if (itemName == "ActivityRewardRate_minutes")
					return this.ActivityRewardRate_minutes;
				else if (itemName == "ActivityReward")
					return this.ActivityReward;
				else if (itemName == "WelcomeMoney")
					return this.WelcomeMoney;
				else if (itemName == "HappyHour_BeginHour")
					return this.HappyHour_BeginHour;
				else if (itemName == "HappyHour_EndHour")
					return this.HappyHour_EndHour;
				else if (itemName == "NPCKill_Reward")
					return this.NPCKill_Reward;
				else
					return 0;
			}
		}

		class Multipliers
		{
			public double AssaultRifle{ get; set; }
			public double BeancanGrenade{ get; set; }
			public double BoltActionRifle{ get; set; }
			public double BoneClub{ get; set; }
			public double BoneKnife{ get; set; }
			public double CandyCaneClub{ get; set; }
			public double CompoundBow{ get; set; }
			public double Crossbow{ get; set; }
			public double CustomSMG{ get; set; }
			public double DoubleBarrelShotgun{ get; set; }
			public double EokaPistol{ get; set; }
			public double F1Grenade{ get; set; }
			public double HandmadeFishingRod{ get; set; }
			public double HuntingBow{ get; set; }
			public double LR300{ get; set; }
			public double Longsword{ get; set; }
			public double M249{ get; set; }
			public double M92Pistol{ get; set; }
			public double MP5A4{ get; set; }
			public double Mace{ get; set; }
			public double Machete{ get; set; }
			public double NailGun{ get; set; }
			public double PumpShotgun{ get; set; }
			public double PythonRevolver{ get; set; }
			public double Revolver{ get; set; }
			public double RocketLauncher{ get; set; }
			public double SalvagedCleaver{ get; set; }
			public double SalvagedSword{ get; set; }
			public double SatchelCharge{ get; set; }
			public double SemiAutomaticPistol{ get; set; }
			public double SemiAutomaticRifle{ get; set; }
			public double Snowball{ get; set; }
			public double Spas12Shotgun{ get; set; }
			public double StoneSpear{ get; set; }
			public double Thompson{ get; set; }
			public double TimedExplosiveCharge{ get; set; }
			public double WaterpipeShotgun{ get; set; }
			public double WoodenSpear{ get; set; }

			public double distance_50{ get; set; }
			public double distance_100{ get; set; }
			public double distance_200{ get; set; }
			public double distance_300{ get; set; }
			public double distance_400{ get; set; }
			public double HappyHourMultiplier{ get; set; }
			public double VIPMultiplier{ get; set; }

			// public double CustomPermissionMultiplier{ get; set; }

			public double GetWeaponM(string wn)
			{
				if (wn == "Assault Rifle") return this.AssaultRifle;
				else if (wn == "Beancan Grenade") return this.BeancanGrenade;
				else if (wn == "Bolt Action Rifle") return this.BoltActionRifle;
				else if (wn == "Bone Club") return this.BoneClub;
				else if (wn == "Bone Knife") return this.BoneKnife;
				else if (wn == "Cand Cane Club") return this.CandyCaneClub;
				else if (wn == "Compound Bow") return this.CompoundBow;
				else if (wn == "Crossbow") return this.Crossbow;
				else if (wn == "Custom SMG") return this.CustomSMG;
				else if (wn == "Double Barrel Shotgun") return this.DoubleBarrelShotgun;
				else if (wn == "Eoka Pistol") return this.EokaPistol;
				else if (wn == "Explosivesatchel") return this.SatchelCharge;
				else if (wn == "Explosivetimed") return this.TimedExplosiveCharge;
				else if (wn == "F1 Grenade") return this.F1Grenade;
				else if (wn == "Handmade Fishing Rod") return this.HandmadeFishingRod;
				else if (wn == "Hunting Bow") return this.HuntingBow;
				else if (wn == "LR-300 Assault Rifle") return this.LR300;
				else if (wn == "Longsword") return this.Longsword;
				else if (wn == "M249") return this.M249;
				else if (wn == "M92 Pistol") return this.M92Pistol;
				else if (wn == "MP5A4") return this.MP5A4;
				else if (wn == "Mace") return this.Mace;
				else if (wn == "Machete") return this.Machete;
				else if (wn == "Nail Gun") return this.NailGun;
				else if (wn == "Pump Shotgun") return this.PumpShotgun;
				else if (wn == "Python Revolver") return this.PythonRevolver;
				else if (wn == "Revolver") return this.Revolver;
				else if (wn == "Rocket Launcher") return this.RocketLauncher;
				else if (wn == "Salvaged Cleaver") return this.SalvagedCleaver;
				else if (wn == "Salvaged Sword") return this.SalvagedSword;
				else if (wn == "Semi-Automatic Pistol") return this.SemiAutomaticPistol;
				else if (wn == "Semi-Automatic Rifle") return this.SemiAutomaticRifle;
				else if (wn == "Snowball") return this.Snowball;
				else if (wn == "Spas-12 Shotgun") return this.Spas12Shotgun;
				else if (wn == "Stone Spear") return this.StoneSpear;
				else if (wn == "Thompson") return this.Thompson;
				else if (wn == "Waterpipe Shotgun") return this.WaterpipeShotgun;
				else if (wn == "Wooden Spear") return this.WoodenSpear;
				else return 1;
			}

			public double GetDistanceM(float distance)
			{
				if (distance >= 400) return this.distance_400;
				else if (distance >= 300) return this.distance_300;
				else if (distance >= 200) return this.distance_200;
				else if (distance >= 100) return this.distance_100;
				else if (distance >= 50) return this.distance_50;
				else return 1;
			}

			public double GetItemByString(string itemName)
			{
				if (itemName == "AssaultRifle") return this.AssaultRifle;
				else if (itemName == "Beancan Grenade") return this.BeancanGrenade;
				else if (itemName == "BoltActionRifle") return this.BoltActionRifle;
				else if (itemName == "Bone Club") return this.BoneClub;
				else if (itemName == "Bone Knife") return this.BoneKnife;
				else if (itemName == "Candy Cane Club") return this.CandyCaneClub;
				else if (itemName == "Crossbow") return this.Crossbow;
				else if (itemName == "CustomSMG") return this.CustomSMG;
				else if (itemName == "DoubleBarrelShotgun") return this.DoubleBarrelShotgun;
				else if (itemName == "EokaPistol") return this.EokaPistol;
				else if (itemName == "F1 Grenade") return this.F1Grenade;
				else if (itemName == "Handmade Fishing Rod") return this.HandmadeFishingRod;
				else if (itemName == "HuntingBow") return this.HuntingBow;
				else if (itemName == "LR300") return this.LR300;
				else if (itemName == "Longsword") return this.Longsword;
				else if (itemName == "M249") return this.M249;
				else if (itemName == "M92 Pistol") return this.M92Pistol;
				else if (itemName == "MP5A4") return this.MP5A4;
				else if (itemName == "Mace") return this.Mace;
				else if (itemName == "Machete") return this.Machete;
				else if (itemName == "Nail Gun") return this.NailGun;
				else if (itemName == "PumpShotgun") return this.PumpShotgun;
				else if (itemName == "Python Revolver") return this.PythonRevolver;
				else if (itemName == "Revolver") return this.Revolver;
				else if (itemName == "Rocket Launcher") return this.RocketLauncher;
				else if (itemName == "Salvaged Cleaver") return this.SalvagedCleaver;
				else if (itemName == "Salvaged Sword") return this.SalvagedSword;
				else if (itemName == "SatchelCharge") return this.SatchelCharge;
				else if (itemName == "SemiAutomaticPistol") return this.SemiAutomaticPistol;
				else if (itemName == "SemiAutomaticRifle") return this.SemiAutomaticRifle;
				else if (itemName == "Snowball") return this.Snowball;
				else if (itemName == "Spas-12 Shotgun") return this.Spas12Shotgun;
				else if (itemName == "Stone Spear") return this.StoneSpear;
				else if (itemName == "Thompson") return this.Thompson;
				else if (itemName == "TimedExplosiveCharge") return this.TimedExplosiveCharge;
				else if (itemName == "WaterpipeShotgun") return this.WaterpipeShotgun;
				else if (itemName == "Wooden Spear") return this.WoodenSpear;

				else if (itemName == "distance_50") return this.distance_50;
				else if (itemName == "distance_100") return this.distance_100;
				else if (itemName == "distance_200") return this.distance_200;
				else if (itemName == "distance_300") return this.distance_300;
				else if (itemName == "distance_400") return this.distance_400;
				else if (itemName == "HappyHourMultiplier") return this.HappyHourMultiplier;
				else if (itemName == "VIPMultiplier") return this.VIPMultiplier;

				// else if (itemName == "CustomPermissionMultiplier")
				//    return this.CustomPermissionMultiplier;
				else return 1;
			}
		}

		class Options
		{
			public bool ActivityReward_Enabled{ get; set; }
			public bool WelcomeMoney_Enabled{ get; set; }
			public bool WeaponMultiplier_Enabled{ get; set; }
			public bool DistanceMultiplier_Enabled{ get; set; }
			public bool HappyHour_Enabled{ get; set; }
			public bool VIPMultiplier_Enabled{ get; set; }
			public bool UseEconomicsPlugin{ get; set; }
			public bool UseServerRewardsPlugin{ get; set; }
			public bool UseFriendsPlugin{ get; set; }
			public bool UseClansPlugin{ get; set; }
			public bool Economics_TakeMoneyFromVictim{ get; set; }
			public bool ServerRewards_TakeMoneyFromVictim{ get; set; }
			public bool PrintToConsole{ get; set; }
			// public bool CustomPermissionMultiplier_Enabled{ get; set; }
			public bool NPCReward_Enabled{ get; set; }

			public bool GetItemByString(string itemName)
			{
				if (itemName == "ActivityReward_Enabled")
					return this.ActivityReward_Enabled;
				else if (itemName == "WelcomeMoney_Enabled")
					return this.WelcomeMoney_Enabled;
				else if (itemName == "WeaponMultiplier_Enabled")
					return this.WeaponMultiplier_Enabled;
				else if (itemName == "DistanceMultiplier_Enabled")
					return this.DistanceMultiplier_Enabled;
				else if (itemName == "UseEconomicsPlugin")
					return this.UseEconomicsPlugin;
				else if (itemName == "UseServerRewardsPlugin")
					return this.UseServerRewardsPlugin;
				else if (itemName == "UseFriendsPlugin")
					return this.UseFriendsPlugin;
				else if (itemName == "UseClansPlugin")
					return this.UseClansPlugin;
				else if (itemName == "Economics_TakeMoneyFromVictim")
					return this.Economics_TakeMoneyFromVictim;
				else if (itemName == "ServerRewards_TakeMoneyFromVictim")
					return this.ServerRewards_TakeMoneyFromVictim;
				else if (itemName == "PrintToConsole")
					return this.PrintToConsole;
				else if (itemName == "HappyHour_Enabled")
					return this.HappyHour_Enabled;
				else if (itemName == "VIPMultiplier_Enabled")
					return this.VIPMultiplier_Enabled;
				else if (itemName == "NPCReward_Enabled")
					return this.NPCReward_Enabled;
				else
					return false;
			}
		}

		class Rewards_Version
		{
			public string Version{ get; set; }
		}

		//class Strings
		//{
		//    public string CustomPermissionName { get; set; }
		//    public string GetItemByString(string itemName)
		//    {
		//        if (itemName == "CustomPermissionName")
		//            return this.CustomPermissionName;
		//        else
		//            return null;
		//    }
		//}
	}
}