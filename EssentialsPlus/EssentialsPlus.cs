using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EssentialsPlus.Db;
using EssentialsPlus.Extensions;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.Configuration;

namespace EssentialsPlus
{
	[ApiVersion(2, 1)]
	public class EssentialsPlus : TerrariaPlugin
	{
		public static ConfigFile<Settings> Config { get; private set; }
		public static IDbConnection Db { get; private set; }
		public static HomeManager Homes { get; private set; }
		public static MuteManager Mutes { get; private set; }

		public override string Author => "WhiteX et al. & AnzhelikaO & Zoom L1";
		public override string Description => "Essentials, but better";
		public override string Name => "EssentialsPlus";
		public override Version Version => new(1, 4);

		public EssentialsPlus(Main game) : base(game) { Order = 999999; }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GeneralHooks.ReloadEvent -= OnReload;
				PlayerHooks.PlayerCommand -= OnPlayerCommand;

				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
			}
			base.Dispose(disposing);
		}
		public override void Initialize()
		{
			GeneralHooks.ReloadEvent += OnReload;
			PlayerHooks.PlayerCommand += OnPlayerCommand;

			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
		}

		private async void OnReload(ReloadEventArgs e)
		{
			string path = Path.Combine(TShock.SavePath, "essentials.json");
			Config = new();
			Config.Read(path, out bool write);
			if (write)
				Config.Write(path);

			await Homes.ReloadAsync();
			e.Player.SendSuccessMessage("[EssentialsPlus] Reloaded config and homes!");
		}

		private List<string> teleportCommands = new List<string>
		{
			"tp", "tppos", "tpnpc", "warp", "spawn", "home", "myhome"
		};

		private void OnPlayerCommand(PlayerCommandEventArgs e)
		{
			if (e.Handled || e.Player == null)
			{
				return;
			}

			Command command = e.CommandList.FirstOrDefault();
			if (command == null || (command.Permissions.Any() && !command.Permissions.Any(s => e.Player.Group.HasPermission(s))))
			{
				return;
			}

			if (e.Player.TPlayer.hostile &&
				command.Names.Select(s => s.ToLowerInvariant())
					.Intersect(Config.Settings.DisabledCommandsInPvp.Select(s => s.ToLowerInvariant()))
					.Any())
			{
				e.Player.SendErrorMessage("This command is blocked while in PvP!");
				e.Handled = true;
				return;
			}

			if (e.Player.Group.HasPermission(Permissions.LastCommand) && command.CommandDelegate != Commands.RepeatLast)
			{
				e.Player.GetPlayerInfo().PushCommand(e.CommandText);
			}

			if (teleportCommands.Contains(e.CommandName) && e.Player.Group.HasPermission(Permissions.TpBack))
			{
				e.Player.GetPlayerInfo().PushBackHistory(e.Player.TPlayer.position);
			}
		}

		private void OnInitialize(EventArgs e)
		{
			#region Config

			string path = Path.Combine(TShock.SavePath, "essentials.json");
			Config = new();
			Config.Read(path, out bool write);
			if (write)
				Config.Write(path);

			#endregion

			#region Database

			switch (TShock.Config.Settings.StorageType)
            {
				case "mysql":
					string[] host = TShock.Config.Settings.MySqlHost.Split(':');
					Db = new MySqlConnection
					{
						ConnectionString = String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.Settings.MySqlDbName,
							TShock.Config.Settings.MySqlUsername,
							TShock.Config.Settings.MySqlPassword)
					};
					break;
				case "sqlite":
					Db = new SqliteConnection(
						new SqliteConnectionStringBuilder()
						{
							DataSource = Path.Combine(TShock.SavePath, "essentials.sqlite")
						}.ToString()
					);
					break;

			}

			Mutes = new MuteManager();

			#endregion

			#region Commands

			//Allows overriding of already created commands.
			Action<Command> Add = c =>
			{
				//Finds any commands with names and aliases that match the new command and removes them.
				TShockAPI.Commands.ChatCommands.RemoveAll(c2 => c2.Names.Exists(s2 => c.Names.Contains(s2)));
				//Then adds the new command.
				TShockAPI.Commands.ChatCommands.Add(c);
			};

			Add(new Command(Permissions.Find, Commands.Find, "find", "f")
			{
				HelpText = "Finds an item and/or NPC with the specified name."
			});

			Add(new Command(Permissions.FreezeTime, Commands.FreezeTime, "freezetime", "ft")
			{
				HelpText = "Toggles freezing the time."
			});

			Add(new Command(Permissions.HomeDelete, Commands.DeleteHome, "delhome")
			{
				AllowServer = false,
				HelpText = "Deletes one of your home points."
			});
			Add(new Command(Permissions.HomeSet, Commands.SetHome, "sethome")
			{
				AllowServer = false,
				HelpText = "Sets you a home point."
			});
			Add(new Command(Permissions.HomeTp, Commands.MyHome, "myhome")
			{
				AllowServer = false,
				HelpText = "Teleports you to one of your home points."
			});

			Add(new Command(Permissions.KickAll, Commands.KickAll, "kickall")
			{
				HelpText = "Kicks everyone on the server."
			});

			Add(new Command(Permissions.LastCommand, Commands.RepeatLast, "=")
			{
				HelpText = "Allows you to repeat your last command."
			});

			Add(new Command(Permissions.More, Commands.More, "more")
			{
				AllowServer = false,
				HelpText = "Maximizes item stack of held item."
			});

			//This will override TShock's 'mute' command
			Add(new Command(Permissions.Mute, Commands.Mute, "mute")
			{
				HelpText = "Manages mutes."
			});

			Add(new Command(Permissions.PvP, Commands.PvP, "pvp", "togglepvp", "togpvp")
			{
				AllowServer = false,
				HelpText = "Toggles your PvP status."
			});

			Add(new Command(Permissions.Ruler, Commands.Ruler, "ruler")
			{
				AllowServer = false,
				HelpText = "Allows you to measure the distances between two blocks."
			});

			Add(new Command(Permissions.Send, Commands.Send, "send")
			{
				HelpText = "Broadcasts a message in a custom color."
			});

			Add(new Command(Permissions.Sudo, Commands.Sudo, "sudo")
			{
				HelpText = "Allows you to execute a command as another user."
			});

			Add(new Command(Permissions.TimeCmd, Commands.TimeCmd, "timecmd")
			{
				HelpText = "Executes a command after a given time interval."
			});

			Add(new Command(Permissions.TpBack, Commands.Back, "back", "b")
			{
				AllowServer = false,
				HelpText = "Teleports you back to your previous position after dying or teleporting."
			});
			Add(new Command(Permissions.TpDown, Commands.Down, "down")
			{
				AllowServer = false,
				HelpText = "Teleports you down through a layer of blocks."
			});
			Add(new Command(Permissions.TpLeft, Commands.Left, "left")
			{
				AllowServer = false,
				HelpText = "Teleports you left through a layer of blocks."
			});
			Add(new Command(Permissions.TpRight, Commands.Right, "right")
			{
				AllowServer = false,
				HelpText = "Teleports you right through a layer of blocks."
			});
			Add(new Command(Permissions.TpUp, Commands.Up, "up")
			{
				AllowServer = false,
				HelpText = "Teleports you up through a layer of blocks."
			});

			#endregion
		}

		private void OnPostInitialize(EventArgs args)
		{
			Homes = new HomeManager(Db);
		}

		private async void OnJoin(JoinEventArgs e)
		{
			if (e.Handled)
				return;

			TSPlayer player = TShock.Players[e.Who];
			if (player == null)
				return;

			List<Mute> mutes = await Mutes.GetUserMuteAsync(player);
			if (mutes == null || mutes.Count == 0)
				return;
			
			foreach (Mute mute in mutes)
            {
				if (DateTime.UtcNow < mute.Expiration)
				{
					player.mute = true;
					player.GetPlayerInfo().Mute = mute;
					break;
				}
			}
		}
		private void OnChat(ServerChatEventArgs args)
		{
			if (args.Handled)
				return;
			TSPlayer player = TShock.Players[args.Who];
			if (player != null)
            {
				if ((args.Text.StartsWith(TShock.Config.Settings.CommandSpecifier) || args.Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier)) && !string.IsNullOrWhiteSpace(args.Text.Substring(1)))
					return;

				var mute = player.GetPlayerInfo().Mute;
				if (mute != null)
                {
					if (mute.Expiration < DateTime.UtcNow)
                    {
						player.mute = false;
						player.SendInfoMessage("You have been unmuted.");
						player.GetPlayerInfo().Mute = null;
					}
					else
                    {
						player.SendErrorMessage($"You have been muted due to \"{mute.Reason}\". Remaining time: {mute.Expiration - DateTime.UtcNow}");
						args.Handled = true;
                    }
				}
			}
		}

		private void OnGetData(GetDataEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}

			TSPlayer tsplayer = TShock.Players[e.Msg.whoAmI];
			if (tsplayer == null)
			{
				return;
			}

			switch (e.MsgID)
			{
				case PacketTypes.PlayerDeathV2:
					if (tsplayer.Group.HasPermission(Permissions.TpBack))
					{
						tsplayer.GetPlayerInfo().PushBackHistory(tsplayer.TPlayer.position);
					}
					return;

				case PacketTypes.Teleport:
					{
						if (tsplayer.Group.HasPermission(Permissions.TpBack))
						{
							using (MemoryStream ms = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
							{
								BitsByte flags = (byte)ms.ReadByte();

								int type = 0;
								if (flags[1])
								{
									type = 2;
								}

								if (type == 0 && tsplayer.Group.HasPermission(TShockAPI.Permissions.rod))
								{
									tsplayer.GetPlayerInfo().PushBackHistory(tsplayer.TPlayer.position);
								}
								else if (type == 2 && tsplayer.Group.HasPermission(TShockAPI.Permissions.wormhole))
								{
									tsplayer.GetPlayerInfo().PushBackHistory(tsplayer.TPlayer.position);
								}
							}
						}
					}
					return;
			}
		}
	}
}
