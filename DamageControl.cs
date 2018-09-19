using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Rust;
using System.IO;
using UnityEngine;

namespace Oxide.Plugins {
 [Info("DamageControl", "mspeedie", "2.3.6", ResourceId = 2677)]
 [Description("Allows authorized users to control damage settings for time, boats, animals, apc, barrels, buildings, bgrades, heli, npcs, players and zombies")]
 // internal class DamageControl : RustPlugin
 class DamageControl: CovalencePlugin {
  // note we check isAdmin as well so Admins get this by default
  readonly string permAdmin = "damagecontrol.admin";

  // zero = damage immuned  1 = no modifier 0.5 half damage 2 double damage
  public float ModifyDeployed;
  public float ModifyDoor;
  public float ModifyFloor;
  public float ModifyFoundation;
  public float ModifyHighExternal;
  public float ModifyOther;
  public float ModifyRoof;
  public float ModifyStairs;
  public float ModifyTC;
  public float ModifyBarrel;
  public float ModifyWall;
  public bool  AllowDecay;

  // these are to make checking look cleaner
  // action
  readonly List < string > dcaction = new List < string > {
   "help",
   "list",
   "set"
  };

  // Class
  // note the code does some mapping as well:
  // bradley = apc
  // scientist = npc
  // heli = helicopter
  // murderer = zombie  (I know they are different but how often to do use both at the same time?)
  readonly List < string > dclass = new List < string > {
   "chicken",
   "bear",
   "wolf",
   "boar",
   "horse",
   "stag",
   "buildingblock",
   "npc",
   "player",
   "time",
   "zombie",
   "apc",
   "helicopter",
   "building",
   "bgrade",
   "boat"
  };

  // time types
  readonly List < string > ttype = new List < string > {
  "0",
  "1",
  "2",
  "3",
  "4",
  "5",
  "6",
  "7",
  "8",
  "9",
  "10",
  "11",
  "12",
  "13",
  "14",
  "15",
  "16",
  "17",
  "18",
  "19",
  "20",
  "21",
  "22",
  "23"
  };

  // bgrade types
  readonly List < string > bgtype = new List < string > {
  "Twigs",
  "Wood",
  "Stone",
  "Metal",
  "TopTier"
  };

  // building material types
  readonly List < string > btype = new List < string > {
   "allowdecay",
   "deployed",
   "door",
   "floor",
   "foundation",
   "highexternal",
   "other",
   "roof",
   "stairs",
   "toolcupboard",
   "wall",
   "barrel"
  };

  // damage types (some seem rather redundant, go FacePunch)
  // this order matched the HitInfo, do not touch or it will break the list command
  readonly List < string > dtype = new List < string > {
   "generic",
   "hunger",
   "thirst",
   "cold",
   "drowned",
   "heat",
   "bleeding",
   "poison",
   "suicide",
   "bullet",
   "slash",
   "blunt",
   "fall",
   "radiation",
   "bite",
   "stab",
   "explosion",
   "radiationexposure",
   "coldexposure",
   "decay",
   "electricshock",
   "arrow"
  };

  // deployables list
  List < string > deployable_list = new List< string >();

  // max size of damage types, if this changed the dtype above needs to be updated
  private
  const int DamageTypeMax = (int) DamageType.LAST;

  // arrays of multipliers by class and one with zero to make buidlings immuned
  float[] _Zeromultipliers = new float[DamageTypeMax]; // immuned, zero damage
  float[] _Onemultipliers = new float[DamageTypeMax]; // normal damage


  // Animals
  float[] _Bearmultipliers = new float[DamageTypeMax]; // Bear
  float[] _Boarmultipliers = new float[DamageTypeMax]; // Boar
  float[] _Chickenmultipliers = new float[DamageTypeMax]; // Chicken
  float[] _Horsemultipliers = new float[DamageTypeMax]; // Horse
  float[] _Stagmultipliers = new float[DamageTypeMax]; // Stag
  float[] _Wolfmultipliers = new float[DamageTypeMax]; // Wolf

  float[] _Boatmultipliers = new float[DamageTypeMax]; // Boats
  float[] _Buildingmultipliers = new float[DamageTypeMax]; // Buildings
  float[] _Zombiemultipliers = new float[DamageTypeMax]; // Murderer (Halloween) and Zombies
  float[] _Playermultipliers = new float[DamageTypeMax]; // Players
  float[] _NPCmultipliers = new float[DamageTypeMax]; // Scientists and NPCs
  float[] _APCmultipliers = new float[DamageTypeMax]; // APC aka Bradley
  float[] _Helimultipliers = new float[DamageTypeMax]; // Helicopter

  // time multiplier
  float[] _Timemultipliers = new float[24]; // Per Hour Multiplier

  // bgrade multipliers
  float _TwigsMultiplier  = 1.0F; // Twigs Multiplier
  float _WoodMultiplier  = 1.0F; // Wood Multiplier
  float _StoneMultiplier = 1.0F; // Stone Multiplier
  float _MetalMultiplier = 1.0F; // Metal Multiplier
  float _TopTierMultiplier = 1.0F; // TopTier Multiplier

  // to indicate I need to update the json file
  bool _didConfigChange;

  void Init() {
   if (!permission.PermissionExists(permAdmin)) permission.RegisterPermission(permAdmin, this);
   // LoadDefaultMessages();  // Done Automatically
   LoadConfigValues();
   build_dep_list();
  }

  void LoadDefaultMessages() {
   // English
   lang.RegisterMessages(new Dictionary < string, string > {
    // general messages
    ["help"] = "You can use list to show a setting and set to set setting.  For example /dc list building door or /dc set npc arrow 2 .",
    ["nopermission"] = "You do not have permission to use that command.",
    ["wrongsyntax"] = "Incorrect Syntax used, please specify help, list or set and then the parameters for those actions.",
    ["wrongsyntaxList"] = "Incorrect Syntax used for action List. Parameters are optionally: Class, Type.",
    ["wrongsyntaxSet"] = "Incorrect Syntax used for action Set. Parameters are: Class, Type, Value.",
    ["wrongaction"] = "Action can be Help, List or Set.",
    ["wrongclass"] = "Class can only be set to one of Boat, Bear, Boar, Chicken, Horse, Stag, Wolf, APC (or Bradley), BGrade, Building, BuildingBlock, Player, Heli, NPC (which includes scientists) , Zombie (which includes Murderers).",
    ["wrongbtype"] = "That is not a supported type: allowdecay, foundation, wall, floor, door, stair, roof, highexternal, barrel, other.",
    ["wrongttype"] = "That is not a supported type: 0 through 23.",
    ["wrongbgtype"] = "That is not a supported type: twigs, wood, stone, metal, toptier. (or 0-4)",
    ["wrongtype"] = "That is not a supported type: Arrow, Bite, Bleeding, Blunt, Bullet, Cold, ColdExposure, Decay, Drowned, ElectricShock, Explosion, Fall, Generic, Heat, Hunger, Poison, Radiation, RadiationExposure, Slash, Stab, Suicide, Thirst.",
    ["wrongbvalues"] = "Building Values can only be set to true or false.",
    ["wrongnvalues"] = "Multiplier Values can only be set from 0 to 100.00.",
    ["frontmess"] = "You have set",
    ["bmiddlemess"] = "protection to",
    ["middlemess"] = "to",
    ["endmess"] = ".",
    // Building Types
    ["door"] = "Doors",
    ["floor"] = "Floors",
    ["foundation"] = "Foundations",
    ["other"] = "Other Building Materials",
    ["roof"] = "Roofs",
    ["stairs"] = "Stairs",
    ["wall"] = "Walls",
	["toolcupboard"] = "ToolCupboard",
	["deployed"] = "Deployable",
	["highexternal"] = "High External",
    // Class
    ["apc"] = "APC aka Bradley",
    ["bear"] = "bear",
    ["boar"] = "boar",
    ["building"] = "Building",
    ["buildingblock"] = "Building Block",
    ["chicken"] = "Chicken",
    ["heli"] = "Helicopter",
    ["horse"] = "Horse",
    ["npc"] = "NPC aka Scientist",
    ["player"] = "Player",
    ["stag"] = "Stag",
	["time"] = "Time",
	["bgrade"] = "Build Grade",
    ["wolf"] = "Wolf",
    ["zombie"] = "Zombie and Murderer",
    ["murderer"] = "Zombie and Murderer",
    ["scientist"] = "NPC aka Scientist",
    // Damage Types
    ["arrow"] = "Arrow",
    ["bite"] = "Bite",
    ["bleeding"] = "Bleeding",
    ["blunt"] = "Blunt",
    ["bullet"] = "Bullet",
    ["cold"] = "Cold",
    ["coldexposure"] = "Cold Exposure",
    ["decay"] = "Decay",
    ["drowned"] = "Drowned",
    ["electricshock"] = "Electric Shock",
    ["explosion"] = "Explosion",
    ["fall"] = "Fall",
    ["generic"] = "Generic",
    ["heat"] = "Heat",
    ["hunger"] = "Hunger",
    ["poison"] = "Poison",
    ["radiation"] = "Radiation",
    ["radiationexposure"] = "Radiation Exposure",
    ["slash"] = "Slash",
    ["stab"] = "Stab",
    ["suicide"] = "Suicide",
    ["thirst"] = "Thirst",
    // Multiplier headings
    ["multipliers"] = "Multipliers"
   }, this);
  }

	void build_dep_list()
    {
        foreach (var itemDef in ItemManager.GetItemDefinitions().ToList())
             {
                var mod = itemDef.GetComponent<ItemModDeployable>();
                if (mod != null)
				{
					if (itemDef.name.LastIndexOf(".item") > 0)
					{
						deployable_list.Add(itemDef.name.Substring(0,itemDef.name.LastIndexOf(".item")).Replace("_",".").ToLower());
						deployable_list.Add(itemDef.name.Substring(0,itemDef.name.LastIndexOf(".item")).Replace("_",".").ToLower()+".deployed"); // hack to deal with some having deployed and some not
					}
					else
					{
						deployable_list.Add(itemDef.name.Replace("_",".").ToLower());
						deployable_list.Add(itemDef.name.Replace("_",".").ToLower()+".deployed");  // hack to deal with some having deployed and some not
					}
				}
             }
		// deal with messed up repair_bench losing its "_" to become repairbench
		deployable_list.Add("repairbench.deployed");
		deployable_list.Add("refinery.small.deployed");

		// debugging dump
		//foreach (string p in deployable_list)
        //{
        //    PrintWarning(p);
        //}
    }


  void Loaded() => LoadConfigValues();
  protected override void LoadDefaultConfig() => Puts("New configuration file created.");

  void LoadConfigValues() {
   foreach(DamageType val in Enum.GetValues(typeof(DamageType))) {
    if (val == DamageType.LAST) continue;
    _APCmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("APC_Multipliers", val.ToString().ToLower(), 1.0));
    _Bearmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Bear_Multipliers", val.ToString().ToLower(), 1.0));
    _Boarmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Boar_Multipliers", val.ToString().ToLower(), 1.0));
    _Boatmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Boat_Multipliers", val.ToString().ToLower(), 1.0));
    _Buildingmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("BuildingBlock_Multipliers", val.ToString().ToLower(), 1.0));
    _Chickenmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Chicken_Multipliers", val.ToString().ToLower(), 1.0));
    _Helimultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Heli_Multipliers", val.ToString().ToLower(), 1.0));
    _Horsemultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Horse_Multipliers", val.ToString().ToLower(), 1.0));
    _NPCmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Scientist_Multipliers", val.ToString().ToLower(), 1.0));
    _Playermultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Player_Multipliers", val.ToString().ToLower(), 1.0));
    _Stagmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Stag_Multipliers", val.ToString().ToLower(), 1.0));
    _Wolfmultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Wolf_Multipliers", val.ToString().ToLower(), 1.0));
    _Zombiemultipliers[(int) val] = Convert.ToSingle(GetConfigValue("Zombie_Multipliers", val.ToString().ToLower(), 1.0)); // also murderers
    _Zeromultipliers[(int) val] = 0;
    _Onemultipliers[(int) val] = 1;
	}

   for (var i = 0; i < 24; i++) {
	   // Puts(i.ToString());
       _Timemultipliers[(int) i] = Convert.ToSingle(GetConfigValue("Time_Multipliers", i.ToString(), 1.0)); // time in hours
   }

   ModifyFoundation = Convert.ToSingle(GetConfigValue("Building", "ModifyFoundation",  1.0));
   ModifyFloor = Convert.ToSingle(GetConfigValue("Building", "ModifyFloor",  1.0));
   ModifyRoof = Convert.ToSingle(GetConfigValue("Building", "ModifyRoof",  1.0));
   ModifyWall = Convert.ToSingle(GetConfigValue("Building", "ModifyWall",  1.0));
   ModifyStairs = Convert.ToSingle(GetConfigValue("Building", "ModifyStairs",  1.0));
   ModifyDoor = Convert.ToSingle(GetConfigValue("Building", "ModifyDoor",  1.0));
   ModifyOther = Convert.ToSingle(GetConfigValue("Building", "ModifyOther",  1.0));
   ModifyDeployed = Convert.ToSingle(GetConfigValue("Building", "ModifyDeployed",  1.0));
   ModifyTC = Convert.ToSingle(GetConfigValue("Building", "ModifyToolCupboard",  1.0));
   ModifyBarrel= Convert.ToSingle(GetConfigValue("Building", "ModifyBarrel",  1.0));
   ModifyHighExternal = Convert.ToSingle(GetConfigValue("Building", "ModifyHighExternal",  1.0));

   AllowDecay = Convert.ToBoolean(GetConfigValue("Building", "AllowDecay", "true"));

   _TwigsMultiplier   = Convert.ToSingle(GetConfigValue("Building_Grade_Multipliers", "Twigs",   1.0));
   _WoodMultiplier    = Convert.ToSingle(GetConfigValue("Building_Grade_Multipliers", "Wood",    1.0));
   _StoneMultiplier   = Convert.ToSingle(GetConfigValue("Building_Grade_Multipliers", "Stone",   1.0));
   _MetalMultiplier   = Convert.ToSingle(GetConfigValue("Building_Grade_Multipliers", "Metal",   1.0));
   _TopTierMultiplier = Convert.ToSingle(GetConfigValue("Building_Grade_Multipliers", "TopTier", 1.0));

   if (!_didConfigChange) return;
   Puts("Configuration file updated.");
   SaveConfig();
  }

  object GetConfigValue(string category, string setting, object defaultValue) {
   var data = Config[category] as Dictionary < string, object > ;
   object value;
   if (data == null) {
    data = new Dictionary < string, object > ();
    Config[category] = data;
    _didConfigChange = true;
   }

   if (data.TryGetValue(setting, out value)) return value;
   value = defaultValue;
   data[setting] = value;
   _didConfigChange = true;
   return value;
  }

  object SetConfigValue(string category, string setting, object defaultValue) {
   var data = Config[category] as Dictionary < string, object > ;
   object value;

   if (data == null) {
    data = new Dictionary < string, object > ();
    Config[category] = data;
    _didConfigChange = true;
   }

   value = defaultValue;
   data[setting] = value;
   _didConfigChange = true;
   return value;
  }

  [Command("DamageControl", "damagecontrol", "damcon", "dc","global.dc","global.DamageControl")]
  void chatCommand_DamageControl(IPlayer iplayer, string command, string[] args) {

   string paramaaction = null;
   string paramaclass = null;
   string paramatype = null;
   string paramavalue = null;
   Boolean newbool = false;
   float newnumber = -1;
   iplayer.Reply("DamageControl running");

   if (!IsAllowed(iplayer))
   {
    iplayer.Reply(Lang("nopermission", iplayer.Id));
   }
   else
   {
    if (args == null || args.Length < 1)
	{
     iplayer.Reply(Lang("wrongsyntax", iplayer.Id));
     return;
    }
	else
	{
     paramaaction = args[0].ToLower();
     if (!dcaction.Contains(paramaaction))
	 {
      iplayer.Reply(Lang("wrongaction", iplayer.Id, args[0]));
      return;
     }
	 if (paramaaction == "help")
	 {
      iplayer.Reply(Lang("help", iplayer.Id, args[0]));
      return;
     }
     else if (paramaaction == "set" && args.Length != 4)
	 {
	  iplayer.Reply(Lang("wrongsyntaxSet", iplayer.Id));
      return;
	 }
     else if (paramaaction == "list" && args.Length < 2 )
	 {
      iplayer.Reply(Lang("wrongsyntaxList", iplayer.Id));
      return;
     }
	 else
	 {
      if (args.Length > 1)
	  {
		paramaclass = args[1].ToLower();
		if (paramaclass == "build")
		   paramaclass = "building";
	   else if (paramaclass.Contains("brad"))
		   paramaclass = "apc";
	   else if (paramaclass.Contains("murder"))
		   paramaclass = "zombie";
	   else if (paramaclass.Contains("heli"))
		   paramaclass = "helicopter";
	   else if (paramaclass == "science")
		   paramaclass = "npc";
	   }
      else
       paramaclass = null;
      if (args.Length > 2)
	  {
		paramatype = args[2].ToLower();
		if (paramatype == "stair")
			paramatype = "stairs";
		if (paramatype.Length > 6 && paramatype.Substring(0,6) == "modify")
			paramatype = paramatype.Substring(6);
	  }
      else
       paramatype = null;
      if (args.Length > 3)
       paramavalue = args[3].ToLower();
      else
       paramavalue = null;
     }

     if (paramaaction == "set" && paramaclass == null) {
      iplayer.Reply(Lang("wrongclass", iplayer.Id, args[1]));
      return;
     }
     if (paramaclass != null && paramaclass != "" && !dclass.Contains(paramaclass)) {
      iplayer.Reply(Lang("wrongclass", iplayer.Id, args[1]));
      return;
     }
     if (paramavalue == "1" && paramaclass.Contains("build") && paramatype.Contains("decay")) {
      paramavalue = "true";
     } else if (paramavalue == "0" && paramaclass.Contains("build") && paramatype.Contains("decay")) {
      paramavalue = "false";
     }
     if (paramavalue != "true" && paramavalue != "false" && paramavalue != null)
      try {
       newnumber = Convert.ToSingle(paramavalue);
      } catch (FormatException) {
       iplayer.Reply(Lang("wrongnvalues", iplayer.Id, args[3]));
       return;
      } catch (OverflowException) {
       iplayer.Reply(Lang("wrongnvalues", iplayer.Id, args[3]));
       return;
      }
     if (paramaclass.Contains("build") && paramatype.Contains("decay"))
	 {
		if (paramavalue != "true" && paramavalue != "false" && paramaaction == "set")
		{
			iplayer.Reply(Lang("wrongbvalues", iplayer.Id, args[3]));
			return;
		}
     } else if ((newnumber < 0 || newnumber > 100) && paramaaction == "set") {
      iplayer.Reply(Lang("wrongnvalues", iplayer.Id, args[3]));
      return;
     }
     if (paramaaction == "set" || paramatype != null) {
		// change text to boolean
		if (paramavalue == "true") {
		newbool = true;
		} else if (paramavalue == "false") {
		newbool = false;
		}

		if (paramaclass.Contains("time")) {
			// check type values
			if (!ttype.Contains(paramatype)) {
				iplayer.Reply(Lang("wrongttype", iplayer.Id, args[2]));
				return;
			}
		} else if (paramaclass.Contains("bgrade")) {
			// Puts("before paramatype: " + paramatype);
			// convert numbers to names
			if (paramatype == "0" || paramatype.Contains("twig"))
				paramatype = "Twigs";
			else if (paramatype == "1" || paramatype.Contains("wood"))
				paramatype = "Wood";
			else if (paramatype == "2" || paramatype.Contains("stone"))
				paramatype = "Stone";
			else if (paramatype == "3" || paramatype.Contains("metal"))
				paramatype = "Metal";
			else if (paramatype == "4" || paramatype.Contains("toptier"))
				paramatype = "TopTier";
			// Puts("After paramatype: " + paramatype);

			// check type values
			if (!bgtype.Contains(paramatype)) {
				iplayer.Reply(Lang("wrongbgtype", iplayer.Id, args[2]));
				return;
			}
		} else if (!paramaclass.Contains("bgrade")  && (!paramaclass.Contains("build") || paramaclass.Contains("block"))) {
			// check type values
			if (!dtype.Contains(paramatype)) {
				iplayer.Reply(Lang("wrongtype", iplayer.Id, args[2]));
				return;
			}
		} else {
			// check type values
			if (!btype.Contains(paramatype)) {
				iplayer.Reply(Lang("wrongbtype", iplayer.Id, args[2]));
				return;
			}
		}
	 }

	 if (paramaaction == "set")
	 {
		if (paramaclass.Contains("bgrade"))
		{
			SetConfigValue("Building_Grade_Multipliers", paramatype, newnumber);
		}
		else if (paramaclass.Contains("build") && !paramaclass.Contains("block")) {
		if (paramatype.Contains("decay")) {
			AllowDecay = newbool;
			SetConfigValue("Building", "AllowDecay", newbool);
		}else if (paramatype.Contains("found")) {
			ModifyFoundation = newnumber;
			SetConfigValue("Building", "ModifyFoundation", newnumber);
		} else if (paramatype.Contains("floor")) {
			ModifyFloor = newnumber;
			SetConfigValue("Building", "ModifyFloor", newnumber);
		} else if (paramatype.Contains("door")) {
			ModifyDoor = newnumber;
			SetConfigValue("Building", "ModifyDoor", newnumber);
		} else if (paramatype.Contains("highexternal")) {
			ModifyHighExternal = newnumber;
			SetConfigValue("Building", "ModifyHighExternal", newnumber);
		} else if (paramatype.Contains("wall")) {
			ModifyWall = newnumber;
			SetConfigValue("Building", "ModifyWall", newnumber);
		} else if (paramatype.Contains("stair")) {
			ModifyStairs = newnumber;
			SetConfigValue("Building", "ModifyStairs", newnumber);
		} else if (paramatype.Contains("roof")) {
			ModifyRoof = newnumber;
			SetConfigValue("Building", "ModifyRoof", newnumber);
		} else if (paramatype.Contains("other")) {
			ModifyOther = newnumber;
			SetConfigValue("Building", "ModifyOther", newnumber);
		} else if (paramatype.Contains("deploy")) {
			ModifyDeployed = newnumber;
			SetConfigValue("Building", "ModifyDeployed", newnumber);
		} else if (paramatype.Contains("cupboard")) {
			ModifyTC = newnumber;
			SetConfigValue("Building", "ModifyToolCupboard", newnumber);
		} else if (paramatype.Contains("barrel")) {
			ModifyBarrel = newnumber;
			SetConfigValue("Building", "ModifyBarrel", newnumber);
		}

		} else if (paramaclass.Contains("build") && paramaclass.Contains("block")) {
		SetConfigValue("BuildingBlock_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("boat") || paramaclass.Contains("rhib") || paramaclass.Contains("rowboat")) {
		SetConfigValue("Boat_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("apc") || paramaclass.Contains("bradley")) {
		SetConfigValue("APC_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("heli")) {
		SetConfigValue("Heli_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("npc") || paramaclass.Contains("scientist")) {
		SetConfigValue("Scientist_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("zombie") || paramaclass.Contains("murderer")) {
		SetConfigValue("Zombie_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("player")) {
		SetConfigValue("Player_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("bear")) {
		SetConfigValue("Bear_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("boar")) {
		SetConfigValue("Boar_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("chicken")) {
		SetConfigValue("Chicken_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("horse")) {
		SetConfigValue("Horse_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("stag")) {
		SetConfigValue("Stag_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("time")) {
		SetConfigValue("Time_Multipliers", paramatype, newnumber);
		} else if (paramaclass.Contains("wolf")) {
		SetConfigValue("Wolf_Multipliers", paramatype, newnumber);
		}
		SaveConfig();
		if (paramavalue != "true" && paramavalue != "false") {
		iplayer.Reply(Lang("frontmess", iplayer.Id) + " " + Lang(paramaclass, iplayer.Id) + " " + Lang(paramatype, iplayer.Id) + " " + Lang("multipliers", iplayer.Id) + " " +
			Lang("middlemess", iplayer.Id) + " " + newnumber.ToString("G4") + " " + Lang("endmess"));
		} else {
		iplayer.Reply(Lang("frontmess", iplayer.Id) + " " + Lang(paramaclass, iplayer.Id) + " " + Lang(paramatype, iplayer.Id) + " " +
						Lang("bmiddlemess", iplayer.Id) + " " + paramavalue + " " + Lang("endmess", iplayer.Id));
		}

     } else // list
     {
		if (paramaclass != null && paramatype != null) // dump a type per class
		{
		 printvalue(iplayer, paramaclass, paramatype, getHitScale(paramaclass,paramatype.ToLower()));
		}
		else if (paramaclass != null) // dump a class
		{
			if (paramaclass.Contains("bgrade"))
				for (var i = 0; i < bgtype.Count; i++) {
					printvalue(iplayer, paramaclass, bgtype[i], getHitScale(paramaclass,bgtype[i].ToLower()));
				}
			else if (paramaclass.Contains("build") && !paramaclass.Contains("block"))
				for (var i = 0; i < btype.Count; i++) {
					printvalue(iplayer, paramaclass, btype[i], getHitScale(paramaclass,btype[i].ToLower()));
				}
			else
				for (var i = 0; i < DamageTypeMax; i++) {
					printvalue(iplayer, paramaclass, dtype[i], getHitScale(paramaclass,dtype[i].ToLower()));
				}
		}
		// this is just too long to read in game so I have removed it
		//else // dump all
		//{
		//	// Buildings
		//	for (var k = 0; k < btype.Count; k++) {
		//			printvalue(player, paramaclass, btype[k], getHitScale(paramaclass,btype[k]));
		//		}
		//	// all other types
		//	for (var i = 0; i < dclass.Count-1; i++) {
		//		for (var j = 0; j < DamageTypeMax; j++) {
		//			printvalue(player, dclass[i], dtype[j], getHitScale(paramaclass,dtype[j]));
		//		}
		//	}
		//}
     }
    }
   }
   return;
  }

    void printvalue(IPlayer player, string paramaclass, string paramatype, string paravalue) {
	if (paramaclass.Contains("build") && !paramaclass.Contains("block"))
		 {
			player.Reply(Lang("frontmess", player.Id) + " " + Lang(paramaclass, player.Id) + " " + Lang(paramatype, player.Id) + " " +
	                Lang("bmiddlemess", player.Id) + " " + paravalue + " " + Lang("endmess", player.Id));
		 }
		else
		 {
			 player.Reply(Lang("frontmess", player.Id) + " " + Lang(paramaclass, player.Id) + " " + Lang(paramatype, player.Id) + " " + Lang("multipliers", player.Id) + " " +
								Lang("middlemess", player.Id) + " " + paravalue + " " + Lang("endmess"));
		 }
	}


    string getHitScale(string paramaclass, string paramatype) {

	float  tempnumber = -1;
	string tempstring = "Undefined";

	if (paramaclass.Contains("build") && !paramaclass.Contains("block")) {
       if (paramatype.Contains("found")) {
			tempstring = Convert.ToString(ModifyFoundation);
       } else if (paramatype.Contains("floor")) {
			tempstring = Convert.ToString(ModifyFloor);
       } else if (paramatype.Contains("door")) {
			tempstring = Convert.ToString(ModifyDoor);
       } else if (paramatype.Contains("wall")) {
			tempstring = Convert.ToString(ModifyWall);
       } else if (paramatype.Contains("stair")) {
			tempstring = Convert.ToString(ModifyStairs);
       } else if (paramatype.Contains("roof")) {
			tempstring = Convert.ToString(ModifyRoof);
       } else if (paramatype.Contains("other")) {
			tempstring = Convert.ToString(ModifyOther);
       } else if (paramatype.Contains("deployed")) {
			tempstring = Convert.ToString(ModifyDeployed);
       }  else if (paramatype.Contains("highexternal")) {
			tempstring = Convert.ToString(ModifyHighExternal);
       }  else if (paramatype.Contains("cupboard")) {
			tempstring = Convert.ToString(ModifyTC);
       } else if (paramatype.Contains("barrel")) {
			tempstring = Convert.ToString(ModifyBarrel);
       }
	}
	else
	{
		if (paramaclass.Contains("build") && paramaclass.Contains("block")) {
			tempnumber = _Buildingmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("apc") || paramaclass.Contains("bradley")) {
			tempnumber = _APCmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("boat") || paramaclass.Contains("rhib")) {
			tempnumber = _Boatmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("heli")) {
			tempnumber = _Helimultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("npc") || paramaclass.Contains("scientist")) {
			tempnumber = _NPCmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("zombie") || paramaclass.Contains("murderer")) {
			tempnumber = _Zombiemultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("player")) {
			tempnumber = _Playermultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("bear")) {
			tempnumber = _Bearmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("boar")) {
			tempnumber = _Boarmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("chicken")) {
			tempnumber = _Chickenmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("horse")) {
			tempnumber = _Horsemultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("stag")) {
			tempnumber = _Stagmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("time")) {
			tempnumber = _Timemultipliers[ttype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("wolf")) {
			tempnumber = _Wolfmultipliers[dtype.IndexOf(paramatype)];
		} else if (paramaclass.Contains("bgrade")) {
			if (paramatype == "twigs" || paramatype == "0")
				tempnumber = _TwigsMultiplier;
			else if (paramatype == "wood" || paramatype == "1")
				tempnumber = _WoodMultiplier;
			else if (paramatype == "stone" || paramatype == "2")
				tempnumber = _StoneMultiplier;
			else if (paramatype == "metal" || paramatype == "3")
				tempnumber = _MetalMultiplier;
			else if (paramatype == "toptier" || paramatype == "4")
				tempnumber = _TopTierMultiplier;
		}
		tempstring = tempnumber.ToString();
	}
	return tempstring;
  }

  void setHitScale(HitInfo hitInfo, float[] _multipliers, float addlnmod) {

	int   time      = 0;
	float timemod   = 0.0F;

	time = Convert.ToInt32(Math.Floor(TOD_Sky.Instance.Cycle.Hour));

	// make sure this is in range
	if (time > 23 || time < 0)
		time = 0;

	// Puts (time.ToString());
	// get the time of day multiplier
	timemod = _Timemultipliers[time];
	if (timemod == null)
		timemod = 1;
	
	// Puts (timemod.ToString());
	//Puts ("addlnmod: " + addlnmod.ToString());
	if (addlnmod == null)
		addlnmod = 1;

	// added logic to apply decay
	for (var i = 0; i < DamageTypeMax; i++)
	{
		if (_multipliers[i] == null)
			_multipliers[i] = 1;
		
		if (AllowDecay == true &&
		   ((DamageType) i == Rust.DamageType.Decay || (DamageType) i == Rust.DamageType.Generic ) &&
		   _multipliers[i] * timemod * addlnmod < 0.01F &&
		   !(hitInfo.Initiator is BaseCombatEntity) &&
			hitInfo?.Weapon?.GetItem()?.info?.displayName?.english == null)
		{
			if (_multipliers[i] == 0)
				_multipliers[i] = 1;
			if (timemod == 0)
				timemod = 1;
			if (addlnmod == 0)
				addlnmod = 1;

			hitInfo.damageTypes.Scale((DamageType) i, _multipliers[i] * timemod * addlnmod);
		}
		else
			hitInfo.damageTypes.Scale((DamageType) i, _multipliers[i] * timemod * addlnmod);
		// Puts (_multipliers[i].ToString());
	}

  }

  void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo) {

	float modifier = 1.0F;
	// debugging
	//PrintWarning("0 " + entity.ShortPrefabName.Replace("_","."));

	if (entity == null || hitInfo == null)
		{
			return; // Nothing to process
		}
	else if(entity is LootContainer && entity.ShortPrefabName.Contains("barrel"))  // barrel
	{
		modifier = ModifyBarrel;

		setHitScale(hitInfo, _Onemultipliers,modifier);
	}
	else if (entity is NPCMurderer) // Murderer (treated the same as zombies)
		{
			setHitScale(hitInfo, _Zombiemultipliers,1.0F);
			return;
		}
	else if (entity is NPCPlayerApex || entity is NPCPlayer) // BotSpawn type Scientists, etc.
		{
			setHitScale(hitInfo, _NPCmultipliers,1.0F);
			return;
		}
	else if (entity as BradleyAPC != null) // APC
		{
			setHitScale(hitInfo, _APCmultipliers,1.0F);
			return;
		}
	else if (entity as BaseHelicopter != null) // Heli
		{
			setHitScale(hitInfo, _Helimultipliers,1.0F);
			return;
		}
	else if (entity as BaseNpc != null)
		{
			if (entity.ShortPrefabName == "zombie") // Zombie
				{
				setHitScale(hitInfo, _Zombiemultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "bear") // Bear
				{
				setHitScale(hitInfo, _Bearmultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "boar") // Boar
				{
				setHitScale(hitInfo, _Boarmultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "chicken") // Chicken
				{
				setHitScale(hitInfo, _Chickenmultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "horse") // Horse
				{
				setHitScale(hitInfo, _Horsemultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "stag") // Stag
				{
				setHitScale(hitInfo, _Stagmultipliers,1.0F);
				}
			else if (entity.ShortPrefabName == "wolf") // Wolf
				{
				setHitScale(hitInfo, _Wolfmultipliers,1.0F);
				}
			else // Animal not found
				{
				Puts ("Animal not found in Damage Control: " + entity.ShortPrefabName + " using Bear");
				setHitScale(hitInfo, _Bearmultipliers,1.0F);
				}
			return;
		}
	else if (entity as BasePlayer != null)
		{
			setHitScale(hitInfo, _Playermultipliers,1.0F);
			return;
		}
	// special overrides for building
	else if (entity is BuildingBlock || entity is Door || entity.ShortPrefabName.Contains("external") || entity.ShortPrefabName.Contains("hatch"))
	{
		//Puts ("BB: " + entity.ShortPrefabName);
		if (entity.ShortPrefabName.Contains("foundation"))
			modifier = ModifyFoundation;
		else if (entity.ShortPrefabName.Contains("external"))
			modifier = ModifyHighExternal;
		else if (entity.ShortPrefabName.Contains("wall") && !(entity is Door) && !(entity.ShortPrefabName.Contains("external")))
			modifier = ModifyWall;
		else if (entity.ShortPrefabName.Contains("floor") && !entity.ShortPrefabName.Contains("hatch"))
			modifier = ModifyFloor;
		else if (entity.ShortPrefabName.Contains("roof"))
			modifier = ModifyRoof;
		else if ((entity is Door || entity.ShortPrefabName.Contains("hatch")) && !entity.ShortPrefabName.Contains("external"))
			modifier = ModifyDoor;
		else if (entity.ShortPrefabName.Contains("stairs"))
			modifier = ModifyStairs;
		else if (entity is BuildingBlock)
			modifier = ModifyOther;
		else if (deployable_list.Contains(entity.ShortPrefabName.Replace("_",".").ToLower()))  // this deal with high walls etc.
			modifier = ModifyDeployed;

		if (entity is BuildingBlock)
		{
			BuildingBlock buildingBlock = entity as BuildingBlock;

			if (buildingBlock.grade == BuildingGrade.Enum.Twigs)
				modifier = _TwigsMultiplier * modifier;
			else if (buildingBlock.grade == BuildingGrade.Enum.Wood )
				modifier = _WoodMultiplier * modifier;
			else if (buildingBlock.grade == BuildingGrade.Enum.Stone )
				modifier = _StoneMultiplier * modifier;
			else if (buildingBlock.grade == BuildingGrade.Enum.Metal )
				modifier = _MetalMultiplier * modifier;
			else if (buildingBlock.grade == BuildingGrade.Enum.TopTier )
				modifier = _TopTierMultiplier * modifier;
		}
		setHitScale(hitInfo, _Buildingmultipliers, modifier);
		//Puts("Building: " + modifier.ToString());
	}
	else if (entity.ShortPrefabName.Contains("cupboard.tool.deployed")) // TC
	{
		modifier = ModifyTC;
		setHitScale(hitInfo, _Buildingmultipliers,modifier);
	}
	else if (deployable_list.Contains(entity.ShortPrefabName.Replace("_",".").ToLower()) || entity.ShortPrefabName == "woodbox_deployed") // Deployed
	{
		//Puts ("dep: " + entity.ShortPrefabName);
		modifier = ModifyDeployed;
		setHitScale(hitInfo, _Buildingmultipliers,modifier);
	}
	else if (entity.ShortPrefabName == "rhib" || entity.ShortPrefabName == "rowboat")  // Boats
	{
		setHitScale(hitInfo, _Boatmultipliers,1.0F);
	}

   return; // Nothing to process
  }

  bool IsAllowed(IPlayer iplayer) {
   return iplayer != null && (iplayer.IsAdmin || iplayer.HasPermission(permAdmin));
  }

  T GetConfig < T > (string name, T value) => Config[name] == null ? value : (T) Convert.ChangeType(Config[name], typeof(T));
  string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

 }
}