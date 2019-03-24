using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AutomatedEvents", "k1lly0u", "0.1.3", ResourceId = 0)]
    class AutomatedEvents : RustPlugin
    {
        #region Fields
        private Dictionary<EventType, Timer> eventTimers = new Dictionary<EventType, Timer>();
        #endregion

        #region Oxide Hooks

        void OnServerInitialized()
        {
            LoadVariables();
            foreach (var eventType in configData.Events)
                StartEventTimer(eventType.Key);
        }
        void Unload()
        {
            foreach(var timer in eventTimers)
            {
                if (timer.Value != null)
                    timer.Value.Destroy();
            }
        }
        #endregion
        [ConsoleCommand("runevent")]
        private void consoleRunEvent(ConsoleSystem.Arg arg)
        {
			if (arg?.Args == null || arg?.Args?.Length < 1)
			{
				Puts("No event specified!");
				return;
			}
			//else
			//	Puts ("Running Automated Event: " + arg.Args[0].ToLower());

			switch (arg.Args[0].ToLower())
			{
				case "brad":
				case "bradley":
					RunEvent(EventType.Bradley);
					break;
				case "plane":
				case "cargoplane":
					RunEvent(EventType.CargoPlane);
					break;
				case "ship":
				case "cargo":
				case "cargoship":
					RunEvent(EventType.CargoShip);
					break;
				case "ch47":
				case "chinook":
					RunEvent(EventType.Chinook);
					break;
				case "heli":
				case "helicopter":
				case "copter":
					RunEvent(EventType.Helicopter);
					break;
				case "xmas":
				case "chris":
				case "christmas":
					RunEvent(EventType.XMasEvent);
					break;
				default:
					Puts("No clue what event this is: " + arg.Args[0].ToLower());
					break;
			}
		}

        #region Functions
        void StartEventTimer(EventType type)
        {
            var config = configData.Events[type];
            if (!config.Enabled) 
				return;
			else
				eventTimers[type] = timer.In(UnityEngine.Random.Range(config.MinimumTimeBetween, config.MaximumTimeBetween) * 60, () => RunEvent(type));
        }
        void RunEvent(EventType type)
        {
            string prefabName = string.Empty;
			float  x_extra_offset = 0.0f;
			float  y_extra_offset = 0.0f;

			//Puts(ConVar.Server.worldsize.ToString());
			float ran_min =  0.65f;
			float ran_max =  0.80f;
			Vector3 vector3_1 = new Vector3();
			vector3_1.x = UnityEngine.Random.Range(ran_min, ran_max) * ((Math.Round(UnityEngine.Random.value)==0)?-1.0f:1.0f) * (ConVar.Server.worldsize/2);
			vector3_1.z = UnityEngine.Random.Range(ran_min, ran_max) * ((Math.Round(UnityEngine.Random.value)==0)?-1.0f:1.0f) * (ConVar.Server.worldsize/2);
			vector3_1.y = 0.0f;
			//Puts("water level: " + TerrainMeta.WaterMap.GetHeight(vector3_1).ToString());
			vector3_1.y = TerrainMeta.WaterMap.GetHeight(vector3_1);
			if (vector3_1.y < 0)  // make sure its not messed up
				vector3_1.y = 300;
			//Puts("X1: " + vector3_1.x.ToString());
			//Puts("Z1: " + vector3_1.z.ToString());
			//Puts("Y1: " + vector3_1.y.ToString());


            switch (type)
            {
                case EventType.Bradley:
					if(UnityEngine.Object.FindObjectsOfType<BradleyAPC>().ToList().Count < 1)
	                {    
						prefabName = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";
						Puts("Spawning Bradley");
						var entity = (BradleyAPC)GameManager.server.CreateEntity(prefabName, new Vector3(), new Quaternion(), true);
						entity.Spawn();
					}
					else
					{
						Puts(" Bradley already out");
						return;
					}
                    break;
                case EventType.CargoPlane:
                    prefabName = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
					y_extra_offset = 300.0f;
					vector3_1.y  = vector3_1.y + y_extra_offset;
					Puts("Spawning Cargo Plane");
					var Plane = (CargoPlane)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
					Plane.Spawn();
                    break;
                case EventType.CargoShip:
                    prefabName = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
					x_extra_offset = ConVar.Server.worldsize * 0.125f;
					vector3_1.x = vector3_1.x + x_extra_offset;
					vector3_1.z = vector3_1.z + x_extra_offset;
					Puts("Spawning CargoShip");
					var Ship = (CargoShip)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
					Ship.Spawn();
                    break;
                case EventType.Chinook:
                    prefabName = "assets/prefabs/npc/ch47/ch47scientists.entity.prefab"; // "assets/prefabs/npc/ch47/ch47.entity.prefab";
					y_extra_offset = 300.0f;
					vector3_1.y  = vector3_1.y + y_extra_offset;
					Puts("Spawning Chinook");
					var Chin = (CH47HelicopterAIController)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
					Chin.Spawn();
                    break;
                case EventType.Helicopter:
                    prefabName = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
					y_extra_offset = 300.0f;
					vector3_1.y  = vector3_1.y + y_extra_offset;
					Puts("Spawning Helicopter");
					var Heli = (BaseHelicopter)GameManager.server.CreateEntity(prefabName, vector3_1, new Quaternion(), true);
					Heli.Spawn();
                    break;
                case EventType.XMasEvent:
                    rust.RunServerCommand("xmas.refill");
					Puts("Christmas is occuring");
					return;
                    break;
            }
            StartEventTimer(type);
        }
        #endregion

        #region Config
        enum EventType { Bradley, CargoPlane, CargoShip, Chinook, Helicopter, XMasEvent }
        private ConfigData configData;
        class ConfigData
        {
            public Dictionary<EventType, EventEntry> Events { get; set; }
        }
        class EventEntry
        {
            public bool Enabled { get; set; }
            public int MinimumTimeBetween { get; set; }
            public int MaximumTimeBetween { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                Events = new Dictionary<EventType, EventEntry>
                {
                    { EventType.Bradley, new EventEntry
                    {
                        Enabled = true,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45
                    }
                    },
                    { EventType.CargoPlane, new EventEntry
                    {
                        Enabled = true,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45
                    }
                    },
                    { EventType.CargoShip, new EventEntry
                    {
                        Enabled = true,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45
                    }
                    },
                    { EventType.Chinook, new EventEntry
                    {
                        Enabled = true,
                        MinimumTimeBetween = 30,
                        MaximumTimeBetween = 45
                    }
                    },
                    { EventType.Helicopter, new EventEntry
                    {
                        Enabled = true,
                        MinimumTimeBetween = 45,
                        MaximumTimeBetween = 60
                    }
                    },
                    { EventType.XMasEvent, new EventEntry
                    {
                        Enabled = false,
                        MinimumTimeBetween = 60,
                        MaximumTimeBetween = 120
                    }
                    }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}