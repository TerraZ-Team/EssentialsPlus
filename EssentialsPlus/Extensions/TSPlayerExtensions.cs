using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TShockAPI;
using TShockAPI.Hooks;

namespace EssentialsPlus.Extensions
{
	public static class TSPlayerExtensions
	{
		public static PlayerInfo GetPlayerInfo(this TSPlayer tsplayer)
		{
			if (!tsplayer.ContainsData(PlayerInfo.KEY))
				tsplayer.SetData(PlayerInfo.KEY, new PlayerInfo());
			return tsplayer.GetData<PlayerInfo>(PlayerInfo.KEY);
        }

        /// <summary>
        /// Executes a command. Checks for specific permissions.
        /// </summary>
        /// <param name="player">The command issuer.</param>
        /// <param name="text">The command text.</param>
        /// <returns>True or false.</returns>
        public static bool ExecuteCommand(this TSPlayer player, string text, bool Force = false)
        {
            string cmdText = text.Remove(0, 1);
            string cmdPrefix = text[0].ToString();
            bool silent = (cmdPrefix == TShock.Config.Settings.CommandSilentSpecifier);

            MethodInfo methodInfo = typeof(TShockAPI.Commands).GetMethod
            (
                "ParseParameters",
                (BindingFlags.Static | BindingFlags.NonPublic)
            );
            List<string> args = (List<string>)methodInfo
                                    .Invoke(null, new object[] { cmdText });

            string cmdName = args[0].ToLower();
            args.RemoveAt(0);

            IEnumerable<Command> cmds = TShockAPI.Commands.ChatCommands.FindAll(c => c.HasAlias(cmdName));

            if (PlayerHooks.OnPlayerCommand(player, cmdName, cmdText, args, ref cmds, cmdPrefix))
                return true;

            if (cmds.Count() == 0)
            {
                if (player.AwaitingResponse.ContainsKey(cmdName))
                {
                    Action<CommandArgs> call = player.AwaitingResponse[cmdName];
                    player.AwaitingResponse.Remove(cmdName);
                    call(new CommandArgs(cmdText, player, args));
                    return true;
                }
                player.SendErrorMessage
                (
                    "Invalid command entered. Type {0}help for a list of valid commands.",
                    TShock.Config.Settings.CommandSpecifier
                );
                return true;
            }

            foreach (Command command in cmds)
            {
                if (!command.AllowServer && !player.RealPlayer)
                { player.SendErrorMessage("You must use this command in-game."); }
                else if (command.CanRun(player) || Force)
                {
                    if (command.DoLog)
                    {
                        TShock.Utils.SendLogs
                        (
                            string.Format
                            (
                                "{0} executed: {1}{2}.",
                                player.Name,
                                (silent
                                    ? TShock.Config.Settings.CommandSilentSpecifier
                                    : TShock.Config.Settings.CommandSpecifier),
                                cmdText
                            ),
                            Color.PaleVioletRed,
                            player
                        );
                    }
                    try
                    {
                        CommandDelegate commandD = command.CommandDelegate;
                        commandD(new CommandArgs(cmdText, player, args));
                    }
                    catch (Exception ex)
                    {
                        player.SendErrorMessage("Command failed, check logs for more details.");
                        TShock.Log.Error(ex.ToString());
                    }
                }
                else
                {
                    TShock.Utils.SendLogs
                    (
                        string.Format
                        (
                            "{0} tried to execute {1}{2}.",
                            player.Name,
                            TShock.Config.Settings.CommandSpecifier,
                            cmdText
                        ),
                        Color.PaleVioletRed,
                        player
                    );
                    player.SendErrorMessage("You do not have access to this command.");
                }
            }
            return true;
        }
    }
}
