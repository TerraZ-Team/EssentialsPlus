using System;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using EssentialsPlus.Extensions;
using EssentialsPlus.Configuration;

namespace EssentialsPlus.Handlers
{
    class TShockEventsHandler
    {
        private TerrariaPlugin _plugin;

        public TShockEventsHandler(TerrariaPlugin plugin)
        {
            _plugin = plugin;
        }

        public void RegisterHandlers()
        {
            GeneralHooks.ReloadEvent += OnReload;
            PlayerHooks.PlayerCommand += OnPlayerCommand;
            ServerApi.Hooks.GameInitialize.Register(_plugin, OnInitialize);
            ServerApi.Hooks.NetGetData.Register(_plugin, OnGetData);
        }

        private void OnInitialize(EventArgs args) =>
            Config.Reload();

        public void UnregisterHandlers()
        {
            GeneralHooks.ReloadEvent -= OnReload;
            PlayerHooks.PlayerCommand -= OnPlayerCommand;
            ServerApi.Hooks.GameInitialize.Deregister(_plugin, OnInitialize);
            ServerApi.Hooks.NetGetData.Deregister(_plugin, OnGetData);
        }

        private void OnReload(ReloadEventArgs args) =>
            Config.Reload();

        private void OnPlayerCommand(PlayerCommandEventArgs args)
        {
            if (args.Handled || args.Player == null)
                return;

            Command command = args.CommandList.FirstOrDefault();
            if (command == null || (command.Permissions.Any() && !command.Permissions.Any(p => args.Player.Group.HasPermission(p))))
                return;

            if (args.Player.Group.HasPermission(Permissions.LastCommand)
                && !TshockCommandsHandler.IsRepeatLastCommand(command))
            {
                args.Player.GetPlayerInfo().PushCommand(args.CommandText);
            }

            if (TshockCommandsHandler.TrackedTeleportCommands.Contains(args.CommandName)
                && args.Player.Group.HasPermission(Permissions.TpBack))
            {
                args.Player.GetPlayerInfo().PushBackHistory(args.Player.TPlayer.position);
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.Handled)
                return;

            TSPlayer tsplayer = TShock.Players[args.Msg.whoAmI];
            if (tsplayer == null)
            {
                return;
            }

            switch (args.MsgID)
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
                            using MemoryStream ms = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length);
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
                    return;
            }
        }

    }
}
