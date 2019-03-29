using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Recycler Teleport", "Norn", "0.0.2")]
    [Description("Teleport to recyclers via command.")]
    public class RecyclerTeleport : RustPlugin
    {
        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private const string PERMISSION = "RecyclerTeleport.able";
        private List<Recycler> RecyclerList = new List<Recycler>();

        private void OnServerInitialized() { Finalise(); }

        public void Finalise()
        {
            permission.RegisterPermission(PERMISSION.ToLower(), this);
            AddCovalenceCommand("recycler", "RecyclerCommand");
            RecyclerList = UnityEngine.Object.FindObjectsOfType<Recycler>().ToList();
            Puts($"{RecyclerList.Count} recyclers found.");
        }

        private void TeleportToRecycler(IPlayer player)
        {
			int loop_counter = 0;
			BasePlayer bplayer = player.Object as BasePlayer;
            Vector3 newPos = RecyclerList.GetRandom().transform.position;
			while (loop_counter < 21 && (bplayer.IsBuildingBlocked(newPos, new Quaternion(0, 0, 0, 0), new Bounds(Vector3.zero, Vector3.zero))))
			{
				Puts(loop_counter.ToString());
				if (bplayer.IsBuildingBlocked(newPos, new Quaternion(0, 0, 0, 0), new Bounds(Vector3.zero, Vector3.zero)))
				{
					newPos = RecyclerList.GetRandom().transform.position;
					loop_counter++;
				}
			}
			if (bplayer.IsBuildingBlocked(newPos, new Quaternion(0, 0, 0, 0), new Bounds(Vector3.zero, Vector3.zero)))
			{
				player.Message(Lang("RecyclerBlockedm ", player.Id.ToString()));
				return;
			}
			else
			{
				timer.Once((int)Config["TeleportSeconds"], () => { player.Teleport(new GenericPosition(newPos.x, newPos.y + 2.0f, newPos.z)); });
				player.Message(Lang("Teleporting", player.Id.ToString(), Config["TeleportSeconds"].ToString()));
			}
        }

        private void RecyclerCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id.ToString(), PERMISSION)) { player.Message(Lang("NoPermission", player.Id.ToString())); return; }
            if (RecyclerList.Count == 0) { player.Message(Lang("NoRecyclers", player.Id.ToString())); return; }
            object canTeleport = Interface.CallHook("CanTeleport", player);
            if (canTeleport is string) { player.Message((string)canTeleport); return; }
            TeleportToRecycler(player);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "<color=red>You don't have permission to use this command.</color>",
                ["Teleporting"] = "Teleporting to recycler in <color=yellow>{0}</color> seconds.",
                ["RecyclerBlocked"] = "Could not find an unblocked recycler.",
                ["NoRecyclers"] = "No recyclers found."
            }, this);
        }

        protected override void LoadDefaultConfig() { Config["TeleportSeconds"] = 10; }
    }
}