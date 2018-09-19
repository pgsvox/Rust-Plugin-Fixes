using Oxide.Core;
using System;
using System.Text;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("Pigrels", "ichaleynbin, mspeedie", "2.0.1", ResourceId = 0)]
    [Description("Random chance of animal spawning when a barrel breaks")]
    class Pigrels : CovalencePlugin
    {
        private System.Random random = new System.Random();
		private int SumChances = 0;
        private ConfigDats configData;
		private string pig_string =     "assets/rust.ai/agents/boar/boar.prefab";
		private string chicken_string = "assets/rust.ai/agents/chicken/chicken.prefab";
		private string horse_string =   "assets/rust.ai/agents/horse/horse.prefab";
		private string stag_string =    "assets/rust.ai/agents/stag/stag.prefab";
		private string bear_string =    "assets/rust.ai/agents/bear/bear.prefab";
		private string wolf_string =    "assets/rust.ai/agents/wolf/wolf.prefab";
		private string zombie_string =  "assets/rust.ai/agents/zombie/zombie.prefab";
		
		private readonly string permAdmin = "pigrels.admin";
		private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private Dictionary<string, string> messages = new Dictionary<string,string>();

        class ConfigDats
        {
            public int ChanceOfSpawn { get; set; }
            public int ChanceOfPig { get; set; }
            public int ChanceOfChicken { get; set; }
            public int ChanceOfHorse { get; set; }
            public int ChanceOfStag { get; set; }
            public int ChanceOfBear { get; set; }
            public int ChanceOfWolf { get; set; }
            public int ChanceOfZombie { get; set; }
			public int AirDropSurprise { get; set; }
			public int MinSurprise { get; set; }
			public int MaxSurprise { get; set; }			
        }

		bool IsAllowed(IPlayer player) 
		{
			return player != null && (player.IsAdmin || player.HasPermission(permAdmin));
		}

        void SpawnSpecific(Vector3 pos, string whatanimal)
        {

            BaseEntity Surprise = GameManager.server.CreateEntity(whatanimal, pos, new Quaternion(), true);
            Surprise.Spawn();

			return;
        }
		void OnEntitySpawned(BaseNetworkable ent)
        {
            if (ent && ent is Zombie)
            {
                (ent as BaseEntity).Kill();
            }        
        }

        void SpawnSurprise(Vector3 pos)
        {
			System.Random rndnum = new System.Random();
			System.Random rnd = new System.Random();
			int num_animal = rndnum.Next(configData.MinSurprise, configData.MaxSurprise);
			if (num_animal < 1)
				num_animal = 1;

			for (int i = 0; i < num_animal; i++)
			{
				int find_animal = rnd.Next(1, SumChances);
				string supriseanimal = null;
				if (configData.ChanceOfPig > 0 && find_animal <= configData.ChanceOfPig)
					supriseanimal = pig_string;
				else if (configData.ChanceOfChicken > 0 && find_animal <= configData.ChanceOfPig + configData.ChanceOfChicken)
					supriseanimal = chicken_string;
				else if (configData.ChanceOfHorse > 0 && find_animal <= configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse)
					supriseanimal = horse_string;
				else if (configData.ChanceOfStag > 0 && find_animal <= configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + configData.ChanceOfStag)
					supriseanimal = stag_string;
				else if (configData.ChanceOfBear > 0 && find_animal <= configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + configData.ChanceOfStag + configData.ChanceOfBear)
					supriseanimal = bear_string;
				else if (configData.ChanceOfWolf > 0 && find_animal <= configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + configData.ChanceOfStag + configData.ChanceOfBear + configData.ChanceOfWolf)
					supriseanimal = wolf_string;
				else if (configData.ChanceOfZombie > 0)
					supriseanimal = zombie_string;
				else
				{
					Puts(string.Format(lang.GetMessage("nochances", this)));
					supriseanimal = pig_string;
				}
					
				SpawnSpecific(pos, supriseanimal);
			}

			return;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.name.Contains("barrel") && (random.Next(1, 100)<= configData.ChanceOfSpawn))
            {
                SpawnSurprise(entity.transform.position);
                var player = info.Initiator?.ToPlayer();

                if (player != null)
				{
					var Iplayer = players.FindPlayerById(player.UserIDString);
					if (Iplayer != null)
						Iplayer.Reply(Lang("Surprise", Iplayer.Id));
				}
            }
			return;
        }

		void OnAirdrop(CargoPlane plane, Vector3 dropPosition)
		{
			if (configData.AirDropSurprise == null || configData.AirDropSurprise < 1)
				return;
			if (random.Next(1, 100)<= configData.AirDropSurprise)
			{
				SpawnSurprise(dropPosition);
			}
		}

// might not need these two
		
		void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
		{
			if (entity == null || !(entity is SupplySignal)) return;
			if (entity.net == null)
				entity.net = Network.Net.sv.CreateNetworkable();

			if (configData.AirDropSurprise == null || configData.AirDropSurprise < 1)
				return;
			if (random.Next(1, 100)<= configData.AirDropSurprise)
			{
				Vector3 playerposition = player.transform.position;
				SpawnSurprise(playerposition);
			}
		}

		void OnExplosiveDropped(BasePlayer player, BaseEntity entity)
		{
			if (entity == null || !(entity is SupplySignal)) return;

			if (configData.AirDropSurprise == null || configData.AirDropSurprise < 1)
				return;
			if (random.Next(1, 100)<= configData.AirDropSurprise)
			{
				Vector3 playerposition = player.transform.position;
				SpawnSurprise(playerposition);
			}
		}

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigDats
            {
              ChanceOfSpawn   = 5
			, ChanceOfPig     = 1
			, ChanceOfChicken = 1
			, ChanceOfHorse   = 1
			, ChanceOfStag    = 1
			, ChanceOfBear    = 1
			, ChanceOfWolf    = 1
			, ChanceOfZombie  = 1
			, AirDropSurprise = 0
			, MinSurprise     = 1
			, MaxSurprise     = 1
            };
			SumChances = configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + 
							 configData.ChanceOfStag + configData.ChanceOfBear + configData.ChanceOfWolf + configData.ChanceOfZombie;

            Config.WriteObject(configData,true);
        }

        [ChatCommand("pigrels")]
        void SetPigrel(IPlayer player, string command, string[] args)
        {
			if (!IsAllowed(player)) 
			{
				player.Reply(Lang("Noperms", player.Id));
				return;
			}				
            if (args.Count() == 2) 
            {
                int chance;
                try
                {
                    chance = Convert.ToInt32(args[1]);
                }
                catch
                {
					player.Reply(Lang("FailChange", player.Id));
                    return;
                }

                if (chance >100)
                    chance = 100;
                else if (chance < 0)
                    chance = 0;

				if (args[0].ToLower() == "spawn")
				    configData.ChanceOfSpawn = chance;
				else if (args[0].ToLower() == "pig")
				    configData.ChanceOfPig = chance;
				else if (args[0].ToLower() == "chicken")
				    configData.ChanceOfChicken = chance;
				else if (args[0].ToLower() == "horse")
				    configData.ChanceOfHorse = chance;
				else if (args[0].ToLower() == "stag")
				    configData.ChanceOfStag = chance;
				else if (args[0].ToLower() == "bear")
				    configData.ChanceOfBear = chance;
				else if (args[0].ToLower() == "wolf")
				    configData.ChanceOfWolf = chance;
				else if (args[0].ToLower() == "zombie")
				    configData.ChanceOfZombie = chance;
				else if (args[0].ToLower() == "airdrop")
				    configData.AirDropSurprise = chance;
				else if (args[0].ToLower() == "min")
				{
					if (chance < 1)  // has to be > 0
					{
						chance = 1;
						player.Reply(Lang("mustbegt1", player.Id, args[0], chance.ToString()));

					}
					if (chance > configData.MaxSurprise)  // min must be <= max
					{
						player.Reply(Lang("badmin", player.Id, args[0], chance.ToString()));
					}
					else
						configData.MinSurprise = chance;
				    
				}
				else if (args[0].ToLower() == "max")  // has to be > 0
				{
					if (chance < 1)
					{
						chance = 1;
						player.Reply(Lang("mustbegt1", player.Id, args[0], chance.ToString()));

					}
					if (chance < configData.MinSurprise) // max must be >= min
					{
						player.Reply(Lang("badmax", player.Id, args[0], chance.ToString()));
					}
					else
						configData.MaxSurprise = chance;
				}

				
				SumChances = configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + 
							 configData.ChanceOfStag + configData.ChanceOfBear + configData.ChanceOfWolf + configData.ChanceOfZombie;
				
				player.Reply(Lang("Changing", player.Id, args[0], chance.ToString()));
				
				Config.WriteObject(configData);
            }
        }

        private BasePlayer FindPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
			var players     = covalence.Players.Connected.ToList();
            int playerCount = players.Count;
			BasePlayer player = null;
			string lower_name = name.ToLower().Trim();

			if (lower_name == null || playerCount == null && playerCount > 0)
				return null;
            else
            {
               for (int i = 0; i < playerCount; i++)
               {
					//Puts(i.ToString());
					var  icplayer = players[i];
					var  cplayer = icplayer.Object as BasePlayer;
					if (cplayer.displayName.ToLower().Contains(lower_name) == true || cplayer.displayName.ToLower().Trim() == lower_name)
					{
						if (player != null) //  too  many players match
						{
							Puts(string.Format(lang.GetMessage("toomanyplayer", this)),name);
							return null;
						}
						else
						{
							player = cplayer;
						}
					}
				}
            }
			return player;

        }

        private BasePlayer FindPlayerByID(ulong userID) { return BasePlayer.FindByID(userID) ?? BasePlayer.FindSleeping(userID) ?? null; }

		[Command("surpriseplayer", "sp")]
		void SurprisePlayer(IPlayer player, string command, string[] args)
        {
			Puts("SurprisePlayer");
			if (!IsAllowed(player))
			{
				player.Reply(Lang("Noperms", player.Id));
				return;
			}
            if (args == null || args.Length < 1)
            {
                Puts(string.Format(lang.GetMessage("noplayer", this)));
                return;
            }

            ulong ID = 0ul;
			BasePlayer target = null;

			// Puts("args[0]" + args[0]);
			
			if (ID != null)
				try
				{
					ulong.TryParse(args[0], out ID);
					target = FindPlayerByID(ID);
				}
				catch
				{
					target = null;
				}

			if (target == null)
				try
				{
					target = FindPlayerByPartialName(args[0]);
				}
				catch
				{
					target = null;
				}

            if (target == null)
            {
                Puts(string.Format(lang.GetMessage("playerNotFound", this),args[0]));
                return;
            }

			var pos = target?.transform?.position ?? Vector3.zero;
            if (pos == null)
			{
				Puts(string.Format(lang.GetMessage("playerPosNotFound", this),args[0]));
				return;
			}

			if ( args.Length < 2)
			{
				Puts(string.Format(lang.GetMessage("randomanimal", this)));
				SpawnSurprise(pos);
				return;
			}
			else
			{
				//Puts ("args[1]" + args[1]);

				var check_name = args[1].Trim().ToLower();
				
				//Puts("check_name: " + check_name);
				
				if (check_name == null)
				{
					Puts(string.Format(lang.GetMessage("randomanimal", this)));
					SpawnSurprise(pos);
					return;
				}
				else if (check_name == "boar" || check_name == "pig" || check_name == "schwein" || check_name == "porco" || check_name == "porc" || check_name == "cerdo" || 
						 check_name == "puerco" || check_name == "sanglier" || check_name == "jabali" || check_name == "javali" || check_name == "eber" || check_name == "porky" || check_name == "swine")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, pig_string);
				}
				else if (check_name == "chicken" || check_name == "rooster" || check_name == "kip" || check_name == "poulet" || check_name == "frango" || check_name == "pollo" || 
						 check_name == "hähnchen" || check_name == "hahn" || check_name == "gallo" || check_name == "galo" || check_name == "coq" || check_name == "cock")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, chicken_string);
				}
				else if (check_name == "horse" || check_name == "cheval" || check_name == "pfred" || check_name == "cavalo" || check_name == "caballo")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, horse_string);
				}
				else if (check_name == "stag" || check_name == "deer" || check_name == "bambi" || check_name == "ciervo" || check_name == "veado" || check_name == "cerf" || check_name == "hirsch")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, stag_string);
				}
				else if (check_name == "wolf" || check_name == "dog" || check_name == "doggo" || check_name == "lobo" || check_name == "loup" || 
						 check_name == "wilk" || check_name == "ulv" || check_name == "perro" || check_name == "hund" || check_name == "cachorro" || check_name == "chien")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, wolf_string);
				}
				else if (check_name == "bear" || check_name == "yogi" || check_name == "oso" || check_name == "ours" || check_name == "bär"  || check_name == "urso")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, bear_string);
				}
				else if (check_name == "zombie" || check_name == "gumby" || check_name == "zumbi" || check_name == "zombi" || check_name == "zambi")
				{
					Puts(string.Format(lang.GetMessage("specificanimal", this),check_name));
					SpawnSpecific(pos, zombie_string);
				}
				else
					Puts(string.Format(lang.GetMessage("invalidanimal", this),check_name));
			}
			return;
        }

        void Init()
        {
            try
            {
				if (!permission.PermissionExists(permAdmin)) permission.RegisterPermission(permAdmin, this);
                configData = Config.ReadObject<ConfigDats>();
				SumChances = configData.ChanceOfPig + configData.ChanceOfChicken + configData.ChanceOfHorse + 
							 configData.ChanceOfStag + configData.ChanceOfBear + configData.ChanceOfWolf + configData.ChanceOfZombie;
            }
            catch
            {
                LoadDefaultConfig();
				Config.WriteObject(configData);
            }

            messages["Changing"] = "Changing chance of {0} to {1}";
            messages["FailChange"] = "Invalid integer chance format: chance unchanged.";
            messages["Surprise"] = "That wasn't just a barrel- it was a suprise!";
			messages["playerNotFound"] = "Could not find player: {0}";
			messages["playerPosNotFound"] = "Could not find player position: {0}";
			messages["noplayer"] = "you need to specify a player";
			messages["toomanyplayer"] = "too many players match that name: {0}";
			messages["invalidanimal"] = "invalid animal: {0}";
			messages["randomanimal"] = "spawing random animal";
			messages["specificanimal"] = "spawning specific animal: {0}";
			messages["nochances"] = "all chances zero, fix pigrel config!";
			messages["badmin"] = "min must be less than max.";
			messages["badmax"] = "max must be more than min.";
			messages["mustbegt1"] = "must be one or more.";
			messages["noperms"] = "you do not have permissions for pigrels.";
            lang.RegisterMessages(messages,this);
        }

        void OnServerSave()
        {
            Config.WriteObject(configData);
        }
    }
}
