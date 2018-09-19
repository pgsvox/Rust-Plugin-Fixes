using System.Collections.Generic;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("AdvanceGather", "Hougan", "1.1.0")]
	[Description("Custom gathering with some action's and extension drop")]
	class AdvanceGather : RustPlugin
	{
		#region Variable
		private bool   enableBroadcast;
		private int    AppleChance;
		private int    BlackRaspberryAmountMax;
		private int    BlackRaspberryAmountMin;
		private int    BlackRaspberryChance;
		private int    BlackRaspberryChancePlanted;
		private int    BlueberryAmountMax;
		private int    BlueberryAmountMin;
		private int    BlueberryChance;
		private int    BlueberryChancePlanted;
		private int    LGFAmountMaxCorn;
		private int    LGFAmountMaxPumpkin;
		private int    LGFAmountMinCorn;
		private int    LGFAmountMinPumpkin;
		private int    LGFChanceCorn;
		private int    LGFChancePlantedCorn;
		private int    LGFChancePlantedPumpkin;
		private int    LGFChancePumpkin;
		private int    SpoiledAppleChance;
		private string AppleItem;
		private string BlackRaspberryItem;
		private string BlueberryItem;
		private string LGFItemCorn;
		private string LGFItemPumpkin;
		private string SpoiledAppleItem;

		#endregion

		#region Function
		object GetVariable(string menu, string datavalue, object defaultValue)
		{
			var data = Config[menu] as Dictionary<string, object>;
			if (data == null)
			{
				data = new Dictionary<string, object>();
				Config[menu] = data;
			}
			object value;
			if (!data.TryGetValue(datavalue, out value))
			{
				value = defaultValue;
				data[datavalue] = value;
			}
			return value;
		}
		string msg(string key, string id = null) => lang.GetMessage(key, this, id);
		#endregion

		#region Hooks
		protected override void LoadDefaultConfig()
		{

			AppleItem = Convert.ToString(GetVariable("Get Apples from Trees", "Apple Item", "apple"));
			SpoiledAppleItem = Convert.ToString(GetVariable("Get Rotten Apples from Trees", "Rotten Apple Item", "apple.spoiled"));
			enableBroadcast = Convert.ToBoolean(GetVariable("Option", "Enable broadcast", true));
			SpoiledAppleChance = Convert.ToInt32(GetVariable("Get Rotten Apple from Tree", "Chance to drop rotten apple per hit", 3));
			AppleChance = Convert.ToInt32(GetVariable("Get an Apple from Tree", "Chance to drop a apples per hit", 3));

			BlackRaspberryItem = Convert.ToString(GetVariable("Get Black raspberries from Hemp", "black raspberry Item", "black.raspberries"));
			BlackRaspberryChance = Convert.ToInt32(GetVariable("Get Black raspberries from Hemp", "Chance to get Black raspberry from hemp", 5));
			BlackRaspberryChancePlanted = Convert.ToInt32(GetVariable("Get Black raspberries from Hemp", "Chance to get Black raspberry from planted hemp", 25));
			BlackRaspberryAmountMax = Convert.ToInt32(GetVariable("Get Black raspberries from Hemp", "Max amount", 1));
			BlackRaspberryAmountMin = Convert.ToInt32(GetVariable("Get Black raspberries from Hemp", "Min amount", 1));
			
			BlueberryItem = Convert.ToString(GetVariable("Get Blueberries from Hemp", "blueberry Item", "blueberries"));
			BlueberryChance = Convert.ToInt32(GetVariable("Get Blueberries from Hemp", "Chance to get Blueberry from hemp", 5));
			BlueberryChancePlanted = Convert.ToInt32(GetVariable("Get Blueberries from Hemp", "Chance to get Blueberry from planted hemp", 25));
			BlueberryAmountMax = Convert.ToInt32(GetVariable("Get Blueberries from Hemp", "Max amount", 1));
			BlueberryAmountMin = Convert.ToInt32(GetVariable("Get Blueberries from Hemp", "Min amount", 1));

			LGFItemCorn = Convert.ToString(GetVariable("Get Low Grade Fuel from Corn", "LGF Item", "lowgradefuel"));
			LGFChanceCorn = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Corn", "Chance to get LGF from corn", 10));
			LGFChancePlantedCorn = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Corn", "Chance to get LGF from planted corn", 50));
			LGFAmountMaxCorn = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Corn", "Max amount", 3));
			LGFAmountMinCorn = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Corn", "Min amount", 3));

			LGFItemPumpkin = Convert.ToString(GetVariable("Get Low Grade Fuel from Pumpkin", "LGF Item", "lowgradefuel"));
			LGFChancePumpkin = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Pumpkin", "Chance to get LGF from pumpkin", 10));
			LGFChancePlantedPumpkin = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Pumpkin", "Chance to get LGF from planted pumpkin", 50));
			LGFAmountMaxPumpkin = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Pumpkin", "Max amount", 6));
			LGFAmountMinPumpkin = Convert.ToInt32(GetVariable("Get Low Grade Fuel from Pumpkin", "Min amount", 3));

			SaveConfig();
		}
		void Init()
		{
			LoadDefaultConfig();
			ItemManager.FindItemDefinition(BlueberryItem).stackable = 1000000;
			ItemManager.FindItemDefinition(BlackRaspberryItem).stackable = 1000000;
			lang.RegisterMessages(new Dictionary<string, string>
			{
				//chat
				["Apple"] = "Congratulations!  You received an <color=#66FF33>Apple</color> from tree !",
				["SpoiledApple"] = "That sucks!  You received a <color=#66FF33>Rotten Apple</color> from tree !",
				["Blueberry"] = "Congratulations!  You received <color=#66FF33>Blueberry</color> from hemp !",
				["BlackRaspberry"] = "Congratulations!  You received <color=#66FF33>Black Raspberry</color> from hemp !",
				["LGFCorn"] = "Congratulations!  You received some <color=#66FF33>Low Grade Fuel</color> from corn !",
				["LGFPumpkin"] = "Congratulations!  You received some <color=#66FF33>Low Grade Fuel</color> from pumpkin !"
			}, this);
		}
	
	//Get apple from Tree
		void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
			if (entity is BasePlayer)
			{
				if (dispenser.GetComponent<BaseEntity>() is TreeEntity)
				{
					if (UnityEngine.Random.Range(0, 100) < AppleChance)
					{
						ItemManager.CreateByName(AppleItem, 1).Drop(new Vector3(entity.transform.position.x, entity.transform.position.y + 20f, entity.transform.position.z), Vector3.zero);
						if (enableBroadcast) SendReply(entity as BasePlayer, String.Format(msg("Apple")));
					}
					if (UnityEngine.Random.Range(0, 100) < SpoiledAppleChance)
					{
						ItemManager.CreateByName(SpoiledAppleItem, 1).Drop(new Vector3(entity.transform.position.x, entity.transform.position.y + 20f, entity.transform.position.z), Vector3.zero);
						if (enableBroadcast) SendReply(entity as BasePlayer, String.Format(msg("SpoiledApple")));
					}
				}
			}
		}
		
	//Natural plants
		void OnCollectiblePickup(Item item, BasePlayer player)
		{
			//Get berry from hemp
			if (item.info.shortname.Contains("cloth"))
			{
				if (UnityEngine.Random.Range(0, 100) < BlackRaspberryChance)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(BlackRaspberryItem, Oxide.Core.Random.Range(BlackRaspberryAmountMin, BlackRaspberryAmountMax + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("BlackRaspberry")));
				}
				if (UnityEngine.Random.Range(0, 100) < BlueberryChance)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(BlueberryItem, Oxide.Core.Random.Range(BlueberryAmountMin, BlueberryAmountMax + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("Blueberry")));
				}
			}

	    //Get LGF from corn
			if (item.info.shortname.Contains("corn") && !item.info.shortname.Contains("seed.corn"))
			{
				if (UnityEngine.Random.Range(0, 100) < LGFChanceCorn)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(LGFItemCorn, Oxide.Core.Random.Range(LGFAmountMinCorn, LGFAmountMaxCorn + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("LGFCorn")));
				}
			}

	    //Get LGF from pumpkin
			if (item.info.shortname.Contains("pumpkin") && !item.info.shortname.Contains("seed.pumpkin"))
			{
				if (UnityEngine.Random.Range(0, 100) < LGFChancePumpkin)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(LGFItemPumpkin, Oxide.Core.Random.Range(LGFAmountMinPumpkin, LGFAmountMaxPumpkin + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("LGFPumpkin")));
				}
			}
		}

	//Planted plants
		void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
		{
			//Get berry from planted hemp
			if (item.info.shortname.Contains("cloth"))
			{
				if (UnityEngine.Random.Range(0, 100) < BlackRaspberryChancePlanted)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(BlackRaspberryItem, Oxide.Core.Random.Range(BlackRaspberryAmountMin, BlackRaspberryAmountMax + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("BlackRaspberry")));
				}
				if (UnityEngine.Random.Range(0, 100) < BlueberryChancePlanted)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(BlueberryItem, Oxide.Core.Random.Range(BlueberryAmountMin, BlueberryAmountMax + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("Blueberry")));
				}
			}

			//Get LGF from planted corn
			if (item.info.shortname.Contains("corn") && !item.info.shortname.Contains("seed.corn"))
			{
				if (UnityEngine.Random.Range(0, 100) < LGFChancePlantedCorn)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(LGFItemCorn, Oxide.Core.Random.Range(LGFAmountMinCorn, LGFAmountMaxCorn + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("LGFCorn")));
				}
			}

	    //Get LGF from planted pumpkin
			if (item.info.shortname.Contains("pumpkin") && !item.info.shortname.Contains("seed.pumpkin"))
			{
				if (UnityEngine.Random.Range(0, 100) < LGFChancePlantedPumpkin)
				{
					player.inventory.GiveItem(ItemManager.CreateByName(LGFItemPumpkin, Oxide.Core.Random.Range(LGFAmountMinPumpkin, LGFAmountMaxPumpkin + 1)));
					if (enableBroadcast) SendReply(player, String.Format(msg("LGFPumpkin")));
				}
			}
		}
		#endregion
	}
}