using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AutomatedEvents", "k1lly0u", "0.1.2", ResourceId = 0)]
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
					RunEvent(EventType.Bradley);
					break;
				case "bradley":
					RunEvent(EventType.Bradley);
					break;
				case "plane":
					RunEvent(EventType.CargoPlane);
					break;
				case "ship":
					RunEvent(EventType.CargoShip);
					break;
				case "cargo":
					RunEvent(EventType.CargoShip);
					break;
				case "ch47":
					RunEvent(EventType.Chinook);
					break;
				case "chinook":
					RunEvent(EventType.Chinook);
					break;
				case "heli":
					RunEvent(EventType.Helicopter);
					break;
				case "xmas":
					RunEvent(EventType.XMasEvent);
					break;
				case "chris":
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
            if (!config.Enabled) return;
            eventTimers[type] = timer.In(UnityEngine.Random.Range(config.MinimumTimeBetween, config.MaximumTimeBetween) * 60, () => RunEvent(type));
        }
        void RunEvent(EventType type)
        {
            string prefabName = string.Empty;
			float  y_extra_offset = 0.0f;
            switch (type)
            {
                case EventType.Bradley:
					if(UnityEngine.Object.FindObjectsOfType<BradleyAPC>().ToList().Count < 1)
	                {    
						prefabName = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";
						Puts("Spawning Bradley");
					}
					else
						Puts(" Bradley already out");
                    break;
                case EventType.CargoPlane:
                    prefabName = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
					y_extra_offset = 200.0f;
					Puts("Spawning Cargo Plane");
                    break;
                case EventType.CargoShip:
                    prefabName = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
					y_extra_offset = 0.0f;
					Puts("Spawning CargoShip");
                    break;
                case EventType.Chinook:
                    prefabName = "assets/prefabs/npc/ch47/ch47.entity.prefab";
					y_extra_offset = 200.0f;
					Puts("Spawning Chinook");
                    break;
                case EventType.Helicopter:
                    prefabName = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
					y_extra_offset = 200.0f;
					Puts("Spawning Helicopter");
                    break;
                case EventType.XMasEvent:
                    rust.RunServerCommand("xmas.refill");
					Puts("Christmas is occuring");
                    break;
            }
            if (!string.IsNullOrEmpty(prefabName))
            {
				if (prefabName == "assets/prefabs/npc/m2bradley/bradleyapc.prefab")
				{
					var entity = GameManager.server.CreateEntity(prefabName, new Vector3(), new Quaternion(), true);
					entity.Spawn();
				}
				else
				{
					float x = TerrainMeta.Size.x;
					float mapScaleDistance = ConVar.Server.worldsize;
					//Puts(mapScaleDistance.ToString());
					Vector3 vector3_1 = Vector3Ex.Range(-1.0f, 1.0f);
					vector3_1.Normalize();
					vector3_1.x = UnityEngine.Random.Range(0.8f, 0.99f) * ((Math.Round(UnityEngine.Random.value)==0)?-1.0f:1.0f);
					vector3_1.z = UnityEngine.Random.Range(0.8f, 0.99f) * ((Math.Round(UnityEngine.Random.value)==0)?-1.0f:1.0f);
					//Puts("X1: " + vector3_1.x.ToString());
					//Puts("Z1: " + vector3_1.z.ToString());
					vector3_1.y = 0.0f;
					Vector3 vector3_2 = vector3_1 * mapScaleDistance;
					vector3_2.y = vector3_2.y + y_extra_offset;
					//Puts("X2: " + vector3_2.x.ToString());
					//Puts("Z2: " + vector3_2.z.ToString());
					//Puts("Y2: " + vector3_2.y.ToString());
					var entity = GameManager.server.CreateEntity(prefabName, vector3_2, new Quaternion(), true);
					entity.Spawn();
				}
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