using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Lights On", "mspeedie", "1.3.4")]
    [Description("Toggle lights on/off either as configured or by name.")]
    public class LightsOn : CovalencePlugin
	//RustPlugin
    {

        const string perm_lightson     = "lightson.allowed";
        const string perm_freelights   = "lightson.freelights";
		private bool InitialPass       = true;
		private bool NightToggleactive = false;
		private bool nightcross24      = false;
        private Timer lotimer;
        private Configuration config;

        public class Configuration
        {
			// True means turn them on
			[JsonProperty(PropertyName = "Hats do not use fuel (true/false)")]
			public bool Hats { get; set; } = true;

			[JsonProperty(PropertyName = "BBQs (true/false)")]
			public bool BBQ { get; set; } = false;

			[JsonProperty(PropertyName = "Campfires (true/false)")]
			public bool Campfires { get; set; } = false;

			[JsonProperty(PropertyName = "Candles (true/false)")]
			public bool Candles { get; set; } = true;

			[JsonProperty(PropertyName = "Cauldron (true/false)")]
			public bool Cauldron { get; set; } = false;

			[JsonProperty(PropertyName = "Ceiling lights (true/false)")]
			public bool CeilingLights { get; set; } = true;

			[JsonProperty(PropertyName = "Fire pits (true/false)")]
			public bool FirePits { get; set; } = false;

			[JsonProperty(PropertyName = "Fireplaces (true/false)")]
			public bool Fireplaces { get; set; } = true;

			[JsonProperty(PropertyName = "Fog (true/false)")]
			public bool Fog { get; set; } = true;

			[JsonProperty(PropertyName = "Furnaces (true/false)")]
			public bool Furnaces { get; set; } = false;

			[JsonProperty(PropertyName = "Hobo Barrels (true/false)")]
			public bool Hobo { get; set; } = true;

			[JsonProperty(PropertyName = "Lanterns (true/false)")]
			public bool Lanterns { get; set; } = true;

			[JsonProperty(PropertyName = "Refineries (true/false)")]
			public bool Refineries { get; set; } = false;

			[JsonProperty(PropertyName = "Search lights (true/false)")]
			public bool SearchLights { get; set; } = true;

			[JsonProperty(PropertyName = "SpookySpeaker (true/false)")]
			public bool Speaker { get; set; } = false;

			[JsonProperty(PropertyName = "Strobe (true/false)")]
			public bool Strobe { get; set; } = false;

			[JsonProperty(PropertyName = "Protect BBQs (true/false)")]
			public bool ProtectBBQ { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Campfires (true/false)")]
			public bool ProtectCampfires { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Cauldron (true/false)")]
			public bool ProtectCauldron { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Fire pits (true/false)")]
			public bool ProtectFirePits { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Fireplaces (true/false)")]
			public bool ProtectFireplaces { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Furnaces (true/false)")]
			public bool ProtectFurnaces { get; set; } = true;

			[JsonProperty(PropertyName = "Protect Hobo Barrels (true/false)")]
			public bool ProtectHobo { get; set; } = false;

			[JsonProperty(PropertyName = "Protect Refineries (true/false)")]
			public bool ProtectRefineries { get; set; } = true;

			[JsonProperty(PropertyName = "Always On (true/false)")]
			public bool AlwaysOn { get; set; } = false;

			[JsonProperty(PropertyName = "Night Toggle (true/false)")]
			public bool NightToggle { get; set; } = true;

			[JsonProperty(PropertyName = "Console Output (true/false)")]
			public bool ConsoleMsg { get; set; } = true;

			[JsonProperty(PropertyName = "Check Frequency (in seconds)")]
			public int CheckFrequency { get; set; } = 30;

			[JsonProperty(PropertyName = "Dusk Time (HH in a 24 hour clock)")]
			public float DuskTime { get; set; } = 17.5f;

			[JsonProperty(PropertyName = "Dawn Time (HH in a 24 hour clock)")]
			public float DawnTime { get; set; } = 09.0f;

        }
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["bad check frequency"] = "Check frequency must be between 10 and 600",
                ["bad dusk time"] = "Dusk time must be between 0 and 24",
                ["bad dawn time"] = "Dawn time must be between 0 and 24",
				["dawn=dusk"] = "Dawn can't be the same value as dusk",
				["dawn"] = "Lights going off.  Next lights on at ",
				["default"] = "Loading default config for LightsOn",
                ["dusk"] = "Lights coming on.  Ending at ",
                ["lights off"] = "Lights Off",
                ["lights on"] = "Lights On",
                ["nopermission"] = "You do not have permission to use that command.",
				["one or the other"] = "Please select one (and only one) of Always On or Night Toggle",
                ["prefix"] = "LightsOn: ",
                ["state"] = "unknown state: please use on or off",
                ["syntax"] = "syntax: Lights State (on/off) Optional: prefabshortname (part of the prefab name) to change their state, use all to force all lights' state",
            }, this);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
				{
					if (config.ConsoleMsg)
						Puts(Lang("default"));
                    LoadDefaultConfig();
					SaveConfig();
				}
            }
            catch
            {
				if (config.ConsoleMsg)
					Puts(Lang("default"));
				LoadDefaultConfig();
				SaveConfig();
            }

			// check data is ok because people can make mistakes
			if (config.AlwaysOn == true && config.NightToggle == true)
			{
				Puts(Lang("one or the other"));
				config.NightToggle = false;
			}
			if (config.DuskTime < 0f || config.DuskTime > 24f)
			{
				Puts(Lang("bad dusk time"));
				config.DuskTime = 17f;
			}
			if (config.DawnTime < 0f || config.DawnTime > 24f)
			{
				Puts(Lang("bad dawn time"));
				config.DawnTime = 9f;
			}
			if (config.DawnTime == config.DuskTime)
			{
				Puts(Lang("dawn=dusk"));
				config.DawnTime = 9f;
				config.DuskTime = 17f;
			}
			if (config.CheckFrequency < 10 || config.CheckFrequency > 600)
			{
				Puts(Lang("bad check frequency"));
				config.CheckFrequency = 30;
			}

			// determine correct light timing logic
			if  (config.DuskTime > config.DawnTime)
				nightcross24 = true;
			else
				nightcross24 = false;

			if (config.AlwaysOn || config.NightToggle)
			{
				// start timer to toggle lights based on time
				lotimer = timer.Once(config.CheckFrequency, TimerProcess);
			}

        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        private void OnServerInitialized()
        {
			permission.RegisterPermission(perm_freelights,this);
		}
        bool CanCookShortPrefabName(string prefabName)
        {
            switch (prefabName)
            {
				case "bbq.deployed":				return true;
				case "bbq.static":					return true;
				case "campfire":					return true;
				case "cursedcauldron.deployed":		return true;
				case "fireplace.deployed":			return true;
				case "furnace":						return true;
				case "furnace.large":				return true;
				case "hobobarrel_static":			return true;
				case "refinery_small_deployed":		return true;
				case "skull_fire_pit":				return true;
				case "small_refinery_static":		return true;
				default:
				{
                    return false;
				}
            }
        }

        bool ProtectShortPrefabName(string prefabName)
        {
            switch (prefabName)
            {
				case "bbq.deployed":				return config.ProtectBBQ;
				case "bbq.static":					return config.ProtectBBQ;
				case "campfire":					return config.ProtectCampfires;
				case "cursedcauldron.deployed":		return config.ProtectCauldron;
				case "fireplace.deployed":			return config.ProtectFireplaces;
				case "furnace":						return config.ProtectFurnaces;
				case "furnace.large":				return config.ProtectFurnaces;
				case "hobobarrel_static":			return config.ProtectHobo;
				case "refinery_small_deployed":		return config.ProtectRefineries;
				case "skull_fire_pit":				return config.ProtectFirePits;
				case "small_refinery_static":		return config.ProtectRefineries;
				default:
				{
                    return false;
				}
            }
        }

        bool ProcessShortPrefabName(string prefabName)
        {
            switch (prefabName)
            {
				case "bbq.deployed":				return config.BBQ;
				case "bbq.static":					return config.BBQ;
				case "campfire":					return config.Campfires;
				case "ceilinglight.deployed":		return config.CeilingLights;
				case "cursedcauldron.deployed":		return config.Cauldron;
				case "fireplace.deployed":			return config.Fireplaces;
				case "fogmachine":					return config.Fog;
				case "furnace":						return config.Furnaces;
				case "furnace.large":				return config.Furnaces;
				case "hobobarrel_static":			return config.Hobo;
				case "jackolantern.angry":			return config.Lanterns;
				case "jackolantern.angry.deployed":	return config.Lanterns;
				case "jackolantern.happy":			return config.Lanterns;
				case "jackolantern.happy.deployed":	return config.Lanterns;
				case "lantern.deployed":			return config.Lanterns;
				case "largecandleset":				return config.Candles;
				case "refinery_small_deployed":		return config.Refineries;
				case "searchlight":					return config.SearchLights;
				case "searchlight.deployed":		return config.SearchLights;
				case "skull_fire_pit":				return config.FirePits;
				case "small_refinery_static":		return config.Refineries;
				case "smallcandleset":				return config.Candles;
				case "spookyspeaker":				return config.Speaker;
				case "strobelight":					return config.Strobe;
				case "tunalight.deployed":			return config.Lanterns;
				case "hat.miner": 					return config.Hats;
				case "hat.candle": 					return config.Hats;
				default:
				{
                    return false;
				}
            }
        }

		private void TimerProcess()
		{
			if (config.NightToggle)
			{
				ProcessNight(InitialPass);
				// clear the Inital flag as we now accurately know the state
				InitialPass = false;
			}
			else if (config.AlwaysOn)
			{
				ProcessLights(true, null);
			}
			// submit for the next pass
			lotimer = timer.Once(config.CheckFrequency, TimerProcess);

		}

		private void ProcessNight(bool InitialPass)
		{
			var gtime = TOD_Sky.Instance.Cycle.Hour;
			if ((nightcross24 == false &&   gtime >= config.DuskTime && gtime < config.DawnTime) ||
					(nightcross24 == true  && ((gtime >= config.DuskTime && gtime < 24) || gtime < config.DawnTime))
					&& (!NightToggleactive || InitialPass == true))
			{
				NightToggleactive = true;
				ProcessLights(true,null);
				if (config.ConsoleMsg)
					Puts(Lang("dusk") + config.DawnTime);
			}
			else if ((nightcross24 == false &&  gtime >= config.DawnTime) ||
					(nightcross24 == true  && (gtime <  config.DuskTime && gtime >= config.DawnTime))
					&& (NightToggleactive || InitialPass == true))
			{
				NightToggleactive = false;
				ProcessLights(false,null);
				if (config.ConsoleMsg)
					Puts(Lang("dawn") + config.DuskTime);
			}
		}

        private void ProcessLights(bool state, string prefabName)
        {
            BaseOven[] ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>() as BaseOven[];
            SearchLight[] searchlights = UnityEngine.Object.FindObjectsOfType<SearchLight>() as SearchLight[];
            Candle[] candles = UnityEngine.Object.FindObjectsOfType<Candle>() as Candle[];
            FogMachine[] fogmachines = UnityEngine.Object.FindObjectsOfType<FogMachine>() as FogMachine[];
            StrobeLight[] strobelights = UnityEngine.Object.FindObjectsOfType<StrobeLight>() as StrobeLight[];
            SpookySpeaker[] spookyspeakers = UnityEngine.Object.FindObjectsOfType<SpookySpeaker>() as SpookySpeaker[];

            foreach (BaseOven oven in ovens)
            {
				if (oven == null || oven.IsDestroyed)
					continue;
				if (oven.IsOn() == state &&
					((oven.cookingTemperature > 50 && state == true) || (oven.cookingTemperature > 0 && CanCookShortPrefabName(oven.ShortPrefabName))))
					continue;
				else if (state == false && string.IsNullOrEmpty(prefabName) && ProtectShortPrefabName(oven.ShortPrefabName))
					continue;
				else
				{
					// Puts(oven.ShortPrefabName + " : " + oven.cookingTemperature);
					if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(oven.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || oven.ShortPrefabName.Contains(prefabName))))
						 {
							 try
							 {
								 oven.SetFlag(BaseEntity.Flags.On, state);
							 }
							 catch
							 {}
						 }
				}
            }

            foreach (SearchLight searchlight in searchlights)
            {
                if (searchlight == null || searchlight.IsDestroyed || searchlight.IsOn() == state)
                        continue;
				else if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(searchlight.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || searchlight.ShortPrefabName.Contains(prefabName))))
                {
					try
					{
						searchlight.SetFlag(BaseEntity.Flags.On, state);
						searchlight.secondsRemaining = 99999999;
					}
					catch
					{}
                }
            }

            foreach (Candle candle in candles)
            {
                if (candle == null || candle.IsDestroyed || candle.IsOn() == state)
                        continue;
				else if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(candle.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || candle.ShortPrefabName.Contains(prefabName))))
						 {
							 try
							 {
								 candle.SetFlag(BaseEntity.Flags.On, state);
							 }
							 catch
							 {
							 }
						 }
            }

            foreach (StrobeLight strobelight in strobelights)
            {
                if (strobelight == null || strobelight.IsDestroyed || strobelight.IsOn() == state)
                        continue;
				else if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(strobelight.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || strobelight.ShortPrefabName.Contains(prefabName))))
 						 {
							 try
							 {
								strobelight.SetFlag(BaseEntity.Flags.On, state);
							 }
							 catch
							 {
							 }
						 }
            }

            foreach (FogMachine fogmachine in fogmachines)
            {
                if (fogmachine == null || fogmachine.IsDestroyed || fogmachine.IsEmitting() == state)
                        continue;
				else if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(fogmachine.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || fogmachine.ShortPrefabName.Contains(prefabName))))
                {
					try
					{
						if (state)
						{
							fogmachine.SetFlag(BaseEntity.Flags.On, state);
							fogmachine.UseFuel(99999999999999);
							fogmachine.EnableFogField();
							fogmachine.StartFogging();
						}
						else
						{
							fogmachine.FinishFogging();
							fogmachine.SetFlag(BaseEntity.Flags.On, state);
						}
                    }
					catch
					{}
                }
            }

            foreach (SpookySpeaker spookyspeaker in spookyspeakers)
            {
                if (spookyspeaker == null || spookyspeaker.IsDestroyed || spookyspeaker.IsOn() == state)
                        continue;
				else if ((string.IsNullOrEmpty(prefabName) && ProcessShortPrefabName(spookyspeaker.ShortPrefabName)) ||
						 (!string.IsNullOrEmpty(prefabName) && (prefabName == "all" || spookyspeaker.ShortPrefabName.Contains(prefabName))))
                {
					try
					{
						spookyspeaker.SetFlag(BaseEntity.Flags.On, state);
						spookyspeaker.SendPlaySound();
					}
					catch
					{}
                }
            }
        }

        private void process_command(string state, string prefabName, IPlayer player)
        {

            if (string.IsNullOrEmpty(state))
			{
				player.Message(String.Concat(Lang("prefix", player.Id), Lang("syntax", player.Id)));
                return;
			}
			else
            {
                if (state == "off" || state == "false" || state == "0")
                {
                    ProcessLights(false, prefabName);
					player.Message(String.Concat(Lang("prefix") , Lang("lights off", player.Id)));
                }
                else if (state == "on" || state == "true" || state == "1")
                {
                    ProcessLights(true, prefabName);
                    player.Message(String.Concat(Lang("prefix"), Lang("lights on", player.Id)));
                }
                else
                {
                    player.Message(String.Concat(Lang("prefix"), Lang("state", player.Id)));
                }
            }
        }

        private object OnFindBurnable(BaseOven oven)
        {
			//Puts("OnFindBurnable: " + oven.ShortPrefabName + " : " + oven.cookingTemperature);
            if (oven != null && !string.IsNullOrEmpty(oven.ShortPrefabName) &&
				ProcessShortPrefabName(oven.ShortPrefabName) &&
				!CanCookShortPrefabName(oven.ShortPrefabName) &&
				oven.OwnerID != 0U && permission.UserHasPermission(oven.OwnerID.ToString(), perm_freelights))
				{
					return ItemManager.CreateByItemID(oven.fuelType.itemid);
					oven.StopCooking();
					oven.allowByproductCreation = false;
					oven.SetFlag(BaseEntity.Flags.On, true);
				}
			return null;
        }

		// for jack o laterns
        private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
			//Puts("OnConsumeFuel: " + oven.ShortPrefabName + " : " + oven.cookingTemperature);
            if (oven != null && fuel != null && !string.IsNullOrEmpty(oven.ShortPrefabName) &&
				ProcessShortPrefabName(oven.ShortPrefabName) &&
				!CanCookShortPrefabName(oven.ShortPrefabName) &&
				oven.OwnerID != 0U && permission.UserHasPermission(oven.OwnerID.ToString(), perm_freelights))
				{
					fuel.amount += 1;
					oven.StopCooking();
					oven.allowByproductCreation = false;
					oven.SetFlag(BaseEntity.Flags.On, true);
				}
			return;
        }

		// for hats
        private void OnItemUse(Item item, int amount)
        {
			string ShortPrefabName = item?.parent?.parent?.info?.shortname ?? item?.GetRootContainer()?.entityOwner?.ShortPrefabName;
			BasePlayer player = null;
			string entityId = null;

			if (string.IsNullOrEmpty(ShortPrefabName))
				return;
            if (!string.IsNullOrEmpty(ShortPrefabName) &&
				ProcessShortPrefabName(ShortPrefabName) &&
				!CanCookShortPrefabName(ShortPrefabName))
				{
					try
					{
						player = item?.GetRootContainer()?.playerOwner;
						entityId = item?.GetRootContainer()?.entityOwner?.OwnerID.ToString();
					}
					catch
					{
						player = null;
						entityId = null;
					}

					if (string.IsNullOrEmpty(player.UserIDString) && string.IsNullOrEmpty(entityId))
					{
						//Puts("OnItemUse no perm");
						return;  // no owner so no permission
					}
					try
					{
						if (permission.UserHasPermission(player.UserIDString, perm_freelights) ||
							permission.UserHasPermission(entityId, perm_freelights))
							item.amount += amount;
					}
					catch
					{
						return;
					}
				}
			return;
		}

		// automatically set lights on that are deployed if the lights are in the on state
		private void OnEntitySpawned(BaseNetworkable entity)
		{
			// Puts(entity.ShortPrefabName);
			// will turn the light on during a lights on phase or if neither is set to on
			if ((config.AlwaysOn || NightToggleactive || !(config.AlwaysOn || config.NightToggle)) &&
				 ProcessShortPrefabName(entity.ShortPrefabName))
			{
				if (entity is BaseOven)
				{
					var bo = entity as BaseOven;
					bo.SetFlag(BaseEntity.Flags.On, true);
				}
				else if (entity is SearchLight)
				{
					var sl = entity as SearchLight;
					sl.SetFlag(BaseEntity.Flags.On, true);
					sl.secondsRemaining = 99999999;
				}
				else if (entity is Candle)
				{
					var ca = entity as Candle;
					ca.SetFlag(BaseEntity.Flags.On, true);
				}
				else if (entity is FogMachine)
				{
                    var fm = entity as FogMachine;
					fm.SetFlag(BaseEntity.Flags.On, true);
                    fm.UseFuel(99999999999999);
                    fm.EnableFogField();
                    fm.StartFogging();
				}
				else if (entity is StrobeLight)
				{
					var sl = entity as StrobeLight;
					sl.SetFlag(BaseEntity.Flags.On, true);
				}
				else if (entity is SpookySpeaker)
				{
					var ss = entity as SpookySpeaker;
                    ss.SetFlag(BaseEntity.Flags.On, true);
                    ss.SendPlaySound();
				}
			}
		}

		[Command("lights"), Permission(perm_lightson)]
		private void ChatCommandlo(IPlayer player, string cmd, string[] args)
        {
            string state = null;
            string prefab = null;

            if (args == null || args.Length < 1)
                player.Message(String.Concat(Lang("prefix", player.Id), Lang("syntax", player.Id)));
			else
			{
				state = args[0].ToLower();
				if (args.Length > 1)
					prefab = args[1].ToLower();

				process_command(state, prefab, player);
			}
        }

    }
}
