using ConVar;
using Oxide.Core;
using System;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using System.IO;

namespace Oxide.Plugins
{
    [Info("LightsOn", "MSPEEDIE", "1.1.2", ResourceId = 0)]
    [Description("Automatically starts all lights.")]
    public class LightsOn : RustPlugin
    {

		Configuration config;

        public class Configuration
        {
			// True means turn them on
            [JsonProperty(PropertyName = "Campfires (true/false)")]
            public bool Campfires { get; set; } = true;

            [JsonProperty(PropertyName = "Ceiling lights (true/false)")]
            public bool CeilingLights { get; set; } = true;

            [JsonProperty(PropertyName = "Fire pits (true/false)")]
            public bool FirePits { get; set; } = true;

            [JsonProperty(PropertyName = "Fireplaces (true/false)")]
            public bool Fireplaces { get; set; } = true;

            [JsonProperty(PropertyName = "Furnaces (true/false)")]
            public bool Furnaces { get; set; } = false;

            [JsonProperty(PropertyName = "Grills (true/false)")]
            public bool Grills { get; set; } = false;

            [JsonProperty(PropertyName = "Lanterns (true/false)")]
            public bool Lanterns { get; set; } = true;

            [JsonProperty(PropertyName = "Refineries (true/false)")]
            public bool Refineries { get; set; } = false;

            [JsonProperty(PropertyName = "Search lights (true/false)")]
            public bool SearchLights { get; set; } = true;

            [JsonProperty(PropertyName = "Hats (true/false)")]
            public bool Hats { get; set; } = true;

            [JsonProperty(PropertyName = "Hobo Barrels (true/false)")]
            public bool Hobo { get; set; } = true;


        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        bool FuelSetting(string prefabName)
        {
            switch (prefabName.ToLower())
            {
                case "campfire":
                    return config.Campfires;
                case "ceilinglight.deployed":
                    return config.CeilingLights;
                case "skull_fire_pit":
                    return config.FirePits;
                case "furnace":
                    return config.Furnaces;
                case "furnace.large":
                    return config.Furnaces;
                case "fireplace.deployed":
                    return config.Fireplaces;
                case "bbq.deployed":
                    return config.Grills;
                case "lantern.deployed":
                    return config.Lanterns;
                case "tunalight.deployed":
                    return config.Lanterns;
                case "refinery_small_deployed":
                    return config.Refineries;
                case "searchlight":
                    return config.SearchLights;
                case "searchlight.deployed":
                    return config.SearchLights;
                case "hat.candle":
                    return config.Hats;
                case "hat.miner":
                    return config.Hats;
                case "jackolantern.happy":
                    return config.Lanterns;
                case "jackolantern.angry":
                    return config.Lanterns;
                case "jackolantern.happy.deployed":
                    return config.Lanterns;
                case "jackolantern.angry.deployed":
                    return config.Lanterns;
                case "hobobarrel_static":
                    return config.Hobo;
                default:
				// Puts (prefabName);
                    return false;
            }
		}

        void ForceLights(bool state)
        {
            BaseOven[] ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>() as BaseOven[];

			if (state == true)
				Puts("Lights on!");
			else if (state == false)
				Puts("Lights off!");
			else
			{
				Puts("Lights unknown.");
				return;
			}


            foreach (BaseOven oven in ovens) {
				if (oven.IsOn() != state)
				{
					//Puts(oven.ShortPrefabName);
                    oven.SetFlag(BaseEntity.Flags.On, state);
				}
				//else
				//	Puts("Failed: " + oven.ShortPrefabName);
			}
		}

		void Lights(bool state)
        {
            BaseOven[] ovens = UnityEngine.Object.FindObjectsOfType<BaseOven>() as BaseOven[];

			if (state == true)
				Puts("Lights on!");
			else if (state == false)
				Puts("Lights off!");
			else
			{
				Puts("Lights unknown.");
				return;
			}


            foreach (BaseOven oven in ovens) {
				if (FuelSetting(oven.ShortPrefabName) && oven.IsOn() != state)
				{
					//Puts(oven.ShortPrefabName);
                    oven.SetFlag(BaseEntity.Flags.On, state);
				}
				//else
				//	Puts("Failed: " + oven.ShortPrefabName);
			}
		}


        [ConsoleCommand("LightsOn")]
		void al(ConsoleSystem.Arg arg)
		{
			Puts("LightsOn");
            if ((arg.Args.Length == 0) || arg.Connection != null)
            {
                return;
            }

			if (arg.Args.Length == 2)
			{
				if  (arg.Args[1].ToLower() != "forced")
				{
					Puts("Second Argument needs to be forced");
					return;
				}
				if (arg.Args[0].ToLower() == "off")
					ForceLights(false);
				else
					ForceLights(true);
			}

			else if (arg.Args.Length == 1)
			{
				if (arg.Args[0].ToLower() == "off")
					Lights(false);
				else
					Lights(true);
			}
			else
				Lights(true);
		}
    }
}
