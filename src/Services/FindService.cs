using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EssentialsPlus.Utils;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace EssentialsPlus.Services
{
    public enum FindMode
    {
        Command,
        Item,
        Npc,
        Tile,
        Wall,
        Prefix,
        Paint,
        Buff
    }

    public static class FindService
    {
        private static readonly Dictionary<FindMode, FindModeDefinition> Definitions = new()
        {
            [FindMode.Command] = new FindModeDefinition(
                aliases: new[] { "c", "command" },
                header: "Found Commands ({0}/{1}):",
                empty: "No commands were found.",
                finder: FindCommands
            ),
            [FindMode.Item] = new FindModeDefinition(
                aliases: new[] { "i", "item" },
                header: "Found Items ({0}/{1}):",
                empty: "No items were found.",
                finder: FindItems
            ),
            [FindMode.Npc] = new FindModeDefinition(
                aliases: new[] { "n", "npc" },
                header: "Found NPCs ({0}/{1}):",
                empty: "No NPCs were found.",
                finder: FindNpcs
            ),
            [FindMode.Tile] = new FindModeDefinition(
                aliases: new[] { "t", "tile" },
                header: "Found Tiles ({0}/{1}):",
                empty: "No tiles were found.",
                finder: search => FindFields(search, typeof(TileID))
            ),
            [FindMode.Wall] = new FindModeDefinition(
                aliases: new[] { "w", "wall" },
                header: "Found Walls ({0}/{1}):",
                empty: "No walls were found.",
                finder: search => FindFields(search, typeof(WallID))
            ),
            [FindMode.Prefix] = new FindModeDefinition(
                aliases: new[] { "p", "prefix" },
                header: "Found Prefixes ({0}/{1}):",
                empty: "No prefixes were found.",
                finder: search => FindFields(search, typeof(PrefixID))
            ),
            [FindMode.Paint] = new FindModeDefinition(
                aliases: new[] { "pa", "paint" },
                header: "Found Paint ({0}/{1}):",
                empty: "No paints were found.",
                finder: FindPaints
            ),
            [FindMode.Buff] = new FindModeDefinition(
                aliases: new[] { "b", "buff" },
                header: "Found Buffs ({0}/{1}):",
                empty: "No buffs were found.",
                finder: search => FindFields(search, typeof(BuffID))
            ),
        };

        private static readonly Dictionary<string, FindMode> AliasIndex =
            Definitions
                .SelectMany(pair => pair.Value.Aliases.Select(alias => new { alias, pair.Key }))
                .ToDictionary(x => x.alias, x => x.Key, StringComparer.OrdinalIgnoreCase);

        public static bool TryGetMode(string token, out FindMode mode)
        {
            mode = default;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string normalized = token.Trim();
            if (normalized.StartsWith("-"))
            {
                normalized = normalized.Substring(1);
            }

            return AliasIndex.TryGetValue(normalized, out mode);
        }

        public static Task<List<string>> FindAsync(FindMode mode, string search)
        {
            return Task.Run(() => Definitions[mode].Finder(search));
        }

        public static void SendPage(TSPlayer player, FindMode mode, int page, string rawMode, string search, List<string> results)
        {
            FindModeDefinition definition = Definitions[mode];
            PaginationTools.SendPage(player, page, results,
                new PaginationTools.Settings
                {
                    HeaderFormat = definition.Header,
                    FooterFormat = string.Format("Type /find {0} {1} {{0}} for more", rawMode, search),
                    NothingToDisplayString = definition.Empty
                });
        }

        private static List<string> FindCommands(string search)
        {
            var commands = new List<string>();
            foreach (Command command in Commands.ChatCommands
                .FindAll(c => c.Names.Any(s => s.Contains(search, StringComparison.OrdinalIgnoreCase))))
            {
                commands.Add(string.Format(
                    "{0} (Permission: {1})",
                    command.Name,
                    command.Permissions.FirstOrDefault()));
            }
            return commands;
        }

        private static List<string> FindItems(string search)
        {
            var items = new List<string>();
            for (int i = -48; i < 0; i++)
            {
                var item = new Item();
                item.netDefaults(i);
                if (item.HoverName.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(string.Format("{0} (ID: {1})", item.HoverName, i));
                }
            }

            for (int i = 0; i < ItemID.Count; i++)
            {
                if (Lang.GetItemNameValue(i).Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(string.Format("{0} (ID: {1})", Lang.GetItemNameValue(i), i));
                }
            }

            return items;
        }

        private static List<string> FindNpcs(string search)
        {
            var npcs = new List<string>();
            for (int i = -65; i < 0; i++)
            {
                var npc = new NPC();
                npc.SetDefaults(i);
                if (npc.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    npcs.Add(string.Format("{0} (ID: {1})", npc.FullName, i));
                }
            }

            for (int i = 0; i < NPCID.Count; i++)
            {
                if (Lang.GetNPCNameValue(i).Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    npcs.Add(string.Format("{0} (ID: {1})", Lang.GetNPCNameValue(i), i));
                }
            }

            return npcs;
        }

        private static List<string> FindPaints(string search)
        {
            var paints = new List<string>();
            for (int i = 0; i < PaintCatalog.Names.Count; i++)
            {
                string paint = PaintCatalog.Names[i];
                if (paint.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    paints.Add(string.Format("{0} (ID: {1})", paint, i + 1));
                }
            }
            return paints;
        }

        private static List<string> FindFields(string search, Type idType)
        {
            var results = new List<string>();
            foreach (FieldInfo fi in idType.GetFields())
            {
                string name = HumanizeFieldName(fi.Name);
                if (name.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(string.Format("{0} (ID: {1})", name, fi.GetValue(null)));
                }
            }
            return results;
        }

        private static string HumanizeFieldName(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        private sealed class FindModeDefinition
        {
            public FindModeDefinition(string[] aliases, string header, string empty, Func<string, List<string>> finder)
            {
                Aliases = aliases;
                Header = header;
                Empty = empty;
                Finder = finder;
            }

            public string[] Aliases { get; }
            public string Header { get; }
            public string Empty { get; }
            public Func<string, List<string>> Finder { get; }
        }
    }
}
