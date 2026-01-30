using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EssentialsPlus.Extensions;
using EssentialsPlus.Services;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;
using Terraria.GameContent.NetModules;
using Terraria.Net;

namespace EssentialsPlus.Handlers
{
	static class TshockCommandsHandler
    {
		private static readonly HashSet<string> TeleportCommandNames = new(StringComparer.OrdinalIgnoreCase)
		{
			"tp",
			"tppos",
			"tphere",
			"tpnpc",
			"tpall",
			"warp",
			"home",
			"spawn",
			"return"
		};

		internal static IReadOnlyCollection<string> TrackedTeleportCommands => TeleportCommandNames;
		internal static bool IsRepeatLastCommand(Command command) => command.CommandDelegate == RepeatLastCommand;

		private static Command[] _cmds = new[]
		{
			new Command(Permissions.Find, FindCommand, "find", "f")
			{ HelpText = "Finds an item and/or NPC with the specified name." },
			new Command(Permissions.FreezeTime, FreezeTimeCommand, "freezetime", "ft")
			{ HelpText = "Toggles freezing the time." },
            new Command(Permissions.KickAll, KickAllCommand, "kickall")
			{ HelpText = "Kicks everyone on the server." },
            new Command(Permissions.LastCommand, RepeatLastCommand, "=")
			{ HelpText = "Allows you to repeat your last command." },
            new Command(Permissions.More, MoreCommand, "more")
			{ AllowServer = false, HelpText = "Maximizes item stack of held item." },
            new Command(Permissions.PvP, PvPCommand, "pvp", "togglepvp", "togpvp")
			{ AllowServer = false, HelpText = "Toggles your PvP status." },
            new Command(Permissions.Ruler, RulerCommand, "ruler")
			{ AllowServer = false, HelpText = "Allows you to measure the distances between two blocks." },
            new Command(Permissions.Send, SendCommand, "send")
			{ HelpText = "Broadcasts a message in a custom color." },
            new Command(Permissions.Sudo, SudoCommand, "sudo")
			{ HelpText = "Allows you to execute a command as another user." },
            new Command(Permissions.TimeCmd, TimeCmdCommand, "timecmd")
			{ HelpText = "Executes a command after a given time interval." },
            new Command(Permissions.TpBack, BackCommand, "back", "b")
			{ AllowServer = false, HelpText = "Teleports you back to your previous position after dying or teleporting." },
            new Command(Permissions.TpDown, DownCommand, "down")
			{ AllowServer = false,  HelpText = "Teleports you down through a layer of blocks." },
            new Command(Permissions.TpLeft, LeftCommand, "left")
			{ AllowServer = false,  HelpText = "Teleports you left through a layer of blocks." },
            new Command(Permissions.TpRight, RightCommand, "right")
			{ AllowServer = false, HelpText = "Teleports you right through a layer of blocks." },
            new Command(Permissions.TpUp, UpCommand, "up")
			{ AllowServer = false, HelpText = "Teleports you up through a layer of blocks." },
        };

		private static List<Command> _origCommands = new();

		public static void RegisterCommands()
		{
			foreach (var cmd in _cmds)
			{
				var commandsToRemove = Commands.ChatCommands
					.Where(c => c.Names.Exists(name => cmd.Names.Contains(name)))
					.ToArray();

				foreach (var existing in commandsToRemove)
				{
					if (!_origCommands.Contains(existing))
					{
						_origCommands.Add(existing);
					}
				}
				Commands.ChatCommands.Add(cmd);
			}

            _origCommands.ForEach(c => Commands.ChatCommands.Remove(c));
        }

        public static void UnregisterCommands()
        {
			foreach (var cmd in _cmds)
			{
				Commands.ChatCommands.Remove(cmd);
			}
            _origCommands.ForEach(c => Commands.ChatCommands.Add(c));
			_origCommands.Clear();
        }

		private static void FindHelp(TSPlayer player)
        {
            player.SendErrorMessage("Invalid syntax! Proper syntax: {0}find <switch> <name...> [page]",
                Commands.Specifier);
            player.SendSuccessMessage("Valid {0}find switches:", Commands.Specifier);

            if (player.RealPlayer)
            {
                player.SendInfoMessage
                (
                    "[c/00FF00:-command], [c/00FF00:-c],  [c/00FF00:c]:  Finds a command.\n" +
                    "[c/00FF00:-item],       [c/00FF00:-i],   [c/00FF00:i]:   Finds an item.\n" +
                    "[c/00FF00:-npc],       [c/00FF00:-n],   [c/00FF00:n]:  Finds an NPC.\n" +
                    "[c/00FF00:-tile],        [c/00FF00:-t],   [c/00FF00:t]:   Finds a tile.\n" +
                    "[c/00FF00:-wall],       [c/00FF00:-w],  [c/00FF00:w]:  Finds a wall.\n" +
                    "[c/00FF00:-prefix]     [c/00FF00:-p],   [c/00FF00:p]:  Finds a prefix.\n" +
                    "[c/00FF00:-paint],      [c/00FF00:-pa], [c/00FF00:pa]: Finds a paint.\n" +
                    "[c/00FF00:-buff],      [c/00FF00:-b],   [c/00FF00:b]:  Finds a buff."
				);
            }
            else
            {
                player.SendInfoMessage
                (
                    "-command, -c,  c:  Finds a command.\n" +
                    "-item,    -i,  i:  Finds an item.\n" +
                    "-npc,     -n,  n:  Finds an NPC.\n" +
                    "-tile,    -t,  t:  Finds a tile.\n" +
                    "-wall,    -w,  w:  Finds a wall.\n" +
                    "-prefix,  -p,  p:  Finds a prefix.\n" +
                    "-paint,   -pa, pa: Finds a paint.\n" +
                    "-buff,    -b,  b:  Finds a buff."
				);
            }
        }

        private static void FindCommand(CommandArgs args)
        {
            RunAsync(FindCommandAsync(args));
        }

        private static async Task FindCommandAsync(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                FindHelp(args.Player);
                return;
            }
            
            string Search = int.TryParse(args.Parameters.Last(), out int Page)
                                ? string.Join(" ", args.Parameters.Skip(1).Take(args.Parameters.Count - 2))
                                : string.Join(" ", args.Parameters.Skip(1));
            if (Page < 1) { Page = 1; }

			string modeToken = args.Parameters[0];
			if (modeToken.StartsWith("!"))
			{
				modeToken = modeToken.Substring(1);
			}

            if (!FindService.TryGetMode(modeToken, out FindMode mode))
            {
                FindHelp(args.Player);
                return;
            }

            List<string> results = await FindService.FindAsync(mode, Search);
            FindService.SendPage(args.Player, mode, Page, args.Parameters[0], Search, results);
        }

        private static void FreezeTimeCommand(CommandArgs args)
		{
			var freezeTime = Terraria.GameContent.Creative.CreativePowerManager.Instance.GetPower<Terraria.GameContent.Creative.CreativePowers.FreezeTime>();
			var enabled = freezeTime.Enabled;

			freezeTime.SetPowerInfo(!enabled);

			NetPacket packet = NetCreativePowersModule.PreparePacket(freezeTime.PowerId, 1);
			packet.Writer.Write(freezeTime.Enabled);
			NetManager.Instance.Broadcast(packet);

            args.Player.SendInfoMessage("{0} {1}froze time.", args.Player.Name, enabled ? "un" : "");
		}

        private static void KickAllCommand(CommandArgs args)
		{
			bool noSave = false;
			int index = 0;
			while (index < args.Parameters.Count && args.Parameters[index].StartsWith("-"))
			{
				string flag = args.Parameters[index].Substring(1).ToLowerInvariant();
				switch (flag)
				{
					case "nosave":
						noSave = true;
						break;
					default:
						args.Player.SendSuccessMessage("Valid {0}kickall switches:", TShock.Config.Settings.CommandSpecifier);
						args.Player.SendInfoMessage("-nosave: Kicks without saving SSC data.");
						return;
				}
				index++;
			}

			int kickLevel = args.Player.Group.GetDynamicPermission(Permissions.KickAll);
			string reason = index < args.Parameters.Count
				? string.Join(" ", args.Parameters.Skip(index))
				: "No reason.";
			if (string.IsNullOrWhiteSpace(reason))
			{
				reason = "No reason.";
			}
			foreach (var player in TShock.Players.Where(p => p != null && p.Group.GetDynamicPermission(Permissions.KickAll) < kickLevel))
			{
				if (!noSave && player.IsLoggedIn)
				{
					player.SaveServerCharacter();
				}
				player.Disconnect("Kicked: " + reason);
			}
            args.Player.SendSuccessMessage("Kicked everyone for '{0}'.", reason);
        }

        private static void RepeatLastCommand(CommandArgs args)
        {
            string arg0 = args.Parameters.FirstOrDefault() ?? "1";
            IReadOnlyList<string> lastCommands = args.Player.GetPlayerInfo().LastCommands;
            if (lastCommands.Count == 0)
            {
                args.Player.SendErrorMessage("You don't have last commands!");
                return;
            }

            if (arg0.ToLower() == "list" || arg0.ToLower() == "l")
            {
                List<string> formated = new List<string>();
                for (int i = 1; i <= lastCommands.Count; i++)
                {
                    if (args.Player.RealPlayer)
                    {
                        formated.Insert(0, string.Format
                        (
                            "[c/808080:({0})] {1}{2}",
                            i, TShock.Config.Settings.CommandSpecifier,
                            lastCommands[i - 1]
                        ));
                    }
                    else
                    {
                        formated.Insert(0, string.Format
                        (
                            "[{0}] {1}{2}",
                            i, TShock.Config.Settings.CommandSpecifier,
                            lastCommands[i - 1]
                        ));
                    }
                }
                args.Player.SendInfoMessage(string.Join("\n", formated));
                return;
            }

            int Index = 1;
            if (args.Parameters.Count > 0
                && (!int.TryParse(arg0, out Index)
                    || Index < 1 || Index > lastCommands.Count))
            {
                args.Player.SendErrorMessage("Invalid command index!");
                return;
            }

            args.Player.SendSuccessMessage("Repeated {0} command '{1}'!", StringExtensions.GetIndex(Index), lastCommands[Index - 1]);
            TShockAPI.Commands.HandleCommand(args.Player, args.Message[0] + lastCommands[Index - 1]);
        }

        private static void MoreCommand(CommandArgs args)
		{
			if (args.TPlayer.preventAllItemPickups)
			{
				args.Player.SendErrorMessage("You can't give away items to yourself.");
				return;
			}
			if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "all")
			{
				bool full = true;
				foreach (Item item in args.TPlayer.inventory)
				{
					if (item == null || item.stack == 0) continue;
					int amtToAdd = item.maxStack - item.stack;
					if (amtToAdd > 0 && item.stack > 0 && !item.IsACoin)//!item.Name.ToLower().Contains("coin"))
					{
						full = false;
						args.Player.GiveItem(item.type, amtToAdd);
					}
				}
				if (!full)
					args.Player.SendSuccessMessage("Filled all your items.");
				else
					args.Player.SendErrorMessage("Your inventory is already full.");
			}
			else
			{
				Item item = args.Player.TPlayer.inventory[args.TPlayer.selectedItem];
				int amtToAdd = item.maxStack - item.stack;
				if (amtToAdd == 0)
					args.Player.SendErrorMessage("Your {0} is already full.", item.Name);
				else if (amtToAdd > 0 && item.stack > 0)
					args.Player.GiveItem(item.type, amtToAdd);
				args.Player.SendSuccessMessage("Filled up your {0}.", item.Name);
			}
		}

        private static void PvPCommand(CommandArgs args)
		{
			TSPlayer player = args.Player;
			if (args.Parameters.Count > 0)
            {
				if (!args.Player.HasPermission(Permissions.PvPOthers))
                {
                    args.Player.SendErrorMessage("You do not have access to this command argument.");
					return;
                }
				string playerName = string.Join(" ", args.Parameters);
                var players = TSPlayer.FindByNameOrID(playerName);
				if (players.Count == 0)
                {
                    args.Player.SendErrorMessage("Invalid player.");
					return;
                }
				else if (players.Count > 1)
                {
                    args.Player.SendMultipleMatchError(players.Select(p => p.Name));
					return;
                }
				else
					player = players.First();
			}
			player.SetPvP(!player.TPlayer.hostile, true);
		}

        private static void RulerCommand(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				if (args.Player.TempPoints.Any(p => p == Point.Zero))
				{
                    args.Player.SendErrorMessage("Ruler points are not set up!");
					return;
				}

				Point p1 = args.Player.TempPoints[0];
				Point p2 = args.Player.TempPoints[1];
				int x = Math.Abs(p1.X - p2.X) + 1;
				int y = Math.Abs(p1.Y - p2.Y) + 1;
				double cartesian = Math.Sqrt(x * x + y * y);
                args.Player.SendInfoMessage("Distances: X: {0}, Y: {1}, Cartesian: {2:N3}", x, y, cartesian);
			}
			else if (args.Parameters.Count == 1)
			{
				if (args.Parameters[0] == "1")
				{
                    args.Player.AwaitingTempPoint = 1;
                    args.Player.SendInfoMessage("Modify a block to set the first ruler point.");
				}
				else if (args.Parameters[0] == "2")
				{
                    args.Player.AwaitingTempPoint = 2;
                    args.Player.SendInfoMessage("Modify a block to set the second ruler point.");
				}
				else
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}ruler [1/2]", TShock.Config.Settings.CommandSpecifier);
			}
			else
                args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}ruler [1/2]", TShock.Config.Settings.CommandSpecifier);
		}

        private static void SendCommand(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}send [r,g,b] <text...>", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			byte r = args.Player.Group.R;
			byte g = args.Player.Group.G;
			byte b = args.Player.Group.B;
			int index = 0;

			if (TryParseRgb(args.Parameters[0], out byte pr, out byte pg, out byte pb))
			{
				r = pr;
				g = pg;
				b = pb;
				index = 1;
			}
			else if (args.Parameters[0].Contains(","))
			{
				args.Player.SendErrorMessage("Invalid color!");
				return;
			}

			if (index >= args.Parameters.Count)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}send [r,g,b] <text...>", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			string message = string.Join(" ", args.Parameters.Skip(index));
			TSPlayer.All.SendMessage(message, new Color(r, g, b));
        }

        private static void SudoCommand(CommandArgs args)
        {
			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sudo [-force] <player> <command...>");
				args.Player.SendInfoMessage("-f, -force: Force sudo, ignoring permissions.");
				return;
			}

			bool force = false;
			int index = 0;
			while (index < args.Parameters.Count && args.Parameters[index].StartsWith("-"))
			{
				string flag = args.Parameters[index].Substring(1).ToLowerInvariant();
				switch (flag)
				{
					case "f":
					case "force":
						if (!args.Player.Group.HasPermission(Permissions.SudoForce))
						{
							args.Player.SendErrorMessage("You do not have access to the switch '-{0}'!", flag);
							return;
						}
						force = true;
						break;
					default:
						args.Player.SendSuccessMessage("Valid {0}sudo switches:", TShock.Config.Settings.CommandSpecifier);
						args.Player.SendInfoMessage("-f, -force: Force sudo, ignoring permissions.");
						return;
				}
				index++;
			}

			if (index >= args.Parameters.Count)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sudo [-force] <player> <command...>");
				return;
			}

			string playerName = args.Parameters[index];
			index++;
			if (index >= args.Parameters.Count)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /sudo [-force] <player> <command...>");
				return;
			}

			string command = string.Join(" ", args.Parameters.Skip(index));
			if (command.StartsWith(TShock.Config.Settings.CommandSpecifier)
				|| command.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
			{
				command = command.Substring(1);
			}
			command = TShock.Config.Settings.CommandSpecifier + command;

            List<TSPlayer> players = TSPlayer.FindByNameOrID(playerName);
            if (players.Count == 0)
            { args.Player.SendErrorMessage("Invalid player '{0}'!", playerName); }
            else if (players.Count > 1)
            {
                args.Player.SendErrorMessage("More than one player matched: {0}",
                    string.Join(", ", players.Select(p => p.Name)));
            }
            else
            {
                if (args.Player.Group.GetDynamicPermission(Permissions.Sudo)
                    <= players[0].Group.GetDynamicPermission(Permissions.Sudo)
                    && !args.Player.Group.HasPermission(Permissions.SudoSuper))
                {
                    args.Player.SendErrorMessage("You cannot force {0} to execute {1}!",
                        players[0].Name, command);
                    return;
                }

                string cmd = command.Split(' ')[0].Substring(1).ToLower();
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    args.Player.SendErrorMessage("Invalid command.");
                    return;
                }

                List<Command> cmds = TShockAPI.Commands.ChatCommands.Where(c => c.Names.Contains(cmd)).ToList();
                if (cmds.Count == 0)
                {
                    args.Player.SendErrorMessage("Invalid command.");
                    return;
                }
                else if (cmds.Any(c => c.Permissions.Any(p => !args.Player.HasPermission(p))))
                {
                    args.Player.SendErrorMessage("You do not have permission to use this command.");
                    return;
                }

                args.Player.SendSuccessMessage("Forced {0} to execute {1}.", players[0].Name, command);
                if (!args.Player.Group.HasPermission(Permissions.SudoInvisible))
                { players[0].SendInfoMessage("{0} forced you to execute {1}.", args.Player.Name, command); }
                players[0].ExecuteCommand(command, force);
            }
        }

        private static void TimeCmdCommand(CommandArgs args)
		{
			RunAsync(TimeCmdCommandAsync(args));
		}

        private static async Task TimeCmdCommandAsync(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}timecmd [-switches...] <time> <command...>", TShock.Config.Settings.CommandSpecifier);
				args.Player.SendSuccessMessage("Valid {0}timecmd switches:", TShock.Config.Settings.CommandSpecifier);
				args.Player.SendInfoMessage("-r, -repeat: Repeats the time command indefinitely.");
				return;
			}

			bool repeat = false;
			int index = 0;
			while (index < args.Parameters.Count && args.Parameters[index].StartsWith("-"))
			{
				string flag = args.Parameters[index].Substring(1).ToLowerInvariant();
				switch (flag)
				{
					case "r":
					case "repeat":
						repeat = true;
						break;
					default:
						args.Player.SendSuccessMessage("Valid {0}timecmd switches:", TShock.Config.Settings.CommandSpecifier);
						args.Player.SendInfoMessage("-r, -repeat: Repeats the time command indefinitely.");
						return;
				}
				index++;
			}

			if (index >= args.Parameters.Count)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}timecmd [-switches...] <time> <command...>", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			string timeToken = args.Parameters[index];
			index++;
			if (index >= args.Parameters.Count)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}timecmd [-switches...] <time> <command...>", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			if (!TShock.Utils.TryParseTime(timeToken, out int seconds) || seconds <= 0 || seconds > int.MaxValue / 1000)
			{
				args.Player.SendErrorMessage("Invalid time '{0}'!", timeToken);
				return;
			}

			string command = string.Join(" ", args.Parameters.Skip(index));
			if (command.StartsWith(TShock.Config.Settings.CommandSpecifier)
				|| command.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
			{
				command = command.Substring(1);
			}

			if (repeat)
				args.Player.SendSuccessMessage("Queued command '{0}{1}' indefinitely. Use /cancel to cancel!", TShock.Config.Settings.CommandSpecifier, command);
			else
				args.Player.SendSuccessMessage("Queued command '{0}{1}'. Use /cancel to cancel!", TShock.Config.Settings.CommandSpecifier, command);
			args.Player.AddResponse("cancel", o =>
			{
                args.Player.GetPlayerInfo().CancelTimeCmd();
                args.Player.SendSuccessMessage("Cancelled all time commands!");
			});

			CancellationToken token = args.Player.GetPlayerInfo().TimeCmdToken;
			try
			{
				do
				{
					await Task.Delay(TimeSpan.FromSeconds(seconds), token);
					TShockAPI.Commands.HandleCommand(args.Player, TShock.Config.Settings.CommandSpecifier + command);
				}
				while (repeat);
			}
			catch (TaskCanceledException)
			{
			}
		}

        private static bool TryParseRgb(string token, out byte r, out byte g, out byte b)
        {
            r = 0;
            g = 0;
            b = 0;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string[] parts = token.Split(',');
            if (parts.Length != 3)
            {
                return false;
            }

            return byte.TryParse(parts[0], out r)
                && byte.TryParse(parts[1], out g)
                && byte.TryParse(parts[2], out b);
        }

        private static void BackCommand(CommandArgs args)
		{
			if (args.Parameters.Count > 1)
			{
				args.Player.SendErrorMessage("Invalid syntax! Proper syntax: {0}back [steps]", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			int steps = 1;
			if (args.Parameters.Count > 0 && (!int.TryParse(args.Parameters[0], out steps) || steps <= 0))
			{
				args.Player.SendErrorMessage("Invalid number of steps '{0}'!", args.Parameters[0]);
				return;
			}

			PlayerInfo info = args.Player.GetPlayerInfo();
			if (info.BackHistoryCount == 0)
			{
				args.Player.SendErrorMessage("Could not teleport back!");
				return;
			}

			steps = Math.Min(steps, info.BackHistoryCount);
			args.Player.SendSuccessMessage("Teleported back {0} step{1}.", steps, steps == 1 ? "" : "s");
			Vector2 vector = info.PopBackHistory(steps);
			args.Player.Teleport(vector.X, vector.Y);
		}
        private static void DownCommand(CommandArgs args)
		{
			RunAsync(RunDirectionalTeleport(args, TeleportDirection.Down, "down", xOffset: 0, yOffset: -10));
		}
        private static void LeftCommand(CommandArgs args)
		{
			RunAsync(RunDirectionalTeleport(args, TeleportDirection.Left, "left", xOffset: 12, yOffset: 0));
		}
        private static void RightCommand(CommandArgs args)
		{
			RunAsync(RunDirectionalTeleport(args, TeleportDirection.Right, "right", xOffset: 0, yOffset: 0));
		}
        private static void UpCommand(CommandArgs args)
		{
			RunAsync(RunDirectionalTeleport(args, TeleportDirection.Up, "up", xOffset: 0, yOffset: 6));
		}

        private static async Task RunDirectionalTeleport(CommandArgs args, TeleportDirection direction, string label, int xOffset, int yOffset)
        {
            if (args.Parameters.Count > 1)
            {
                args.Player.SendErrorMessage("Invalid syntax! Correct syntax: {0}{1} [levels]", TShock.Config.Settings.CommandSpecifier, label);
                return;
            }

            int levels = 1;
            if (args.Parameters.Count > 0 && (!int.TryParse(args.Parameters[0], out levels) || levels <= 0))
            {
                args.Player.SendErrorMessage("Invalid number of levels '{0}'!", args.Parameters[0]);
                return;
            }

            int tileX = args.Player.TileX;
            int tileY = args.Player.TileY;
            int targetX = 0;
            int targetY = 0;
            int stepsFound = 0;

            bool found = await Task.Run(() =>
                TeleportNavigator.TryFindTeleportTarget(tileX, tileY, levels, direction, out targetX, out targetY, out stepsFound));

            if (!found)
            {
                args.Player.SendErrorMessage("Could not teleport {0}!", label);
                return;
            }

            if (args.Player.Group.HasPermission(Permissions.TpBack))
            {
                args.Player.GetPlayerInfo().PushBackHistory(args.TPlayer.position);
            }

            args.Player.Teleport(16 * targetX + xOffset, 16 * targetY + yOffset);
            args.Player.SendSuccessMessage("Teleported {0} {1} level{2}.", label, stepsFound, stepsFound == 1 ? "" : "s");
        }

        private static void RunAsync(Task task)
        {
            if (task == null)
            {
                return;
            }

            task.ContinueWith(t =>
            {
                Exception ex = t.Exception?.GetBaseException();
                if (ex != null)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
	}
}
