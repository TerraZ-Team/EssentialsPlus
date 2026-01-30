using System;
using System.Collections.Generic;
using TShockAPI;

namespace EssentialsPlus.Extensions
{
    public static class GroupExtensions
    {
        public static int GetDynamicPermission(this Group group, string permission)
        {
            if (group == null || string.IsNullOrWhiteSpace(permission))
            {
                return 0;
            }

            int best = group.HasPermission(permission) ? int.MaxValue : 0;

            IEnumerable<string> permissions = group.TotalPermissions;
            if (permissions == null)
            {
                string permissionsString = group.Permissions ?? string.Empty;
                permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
            string prefix = permission + ".";

            foreach (string entry in permissions)
            {
                string trimmed = entry.Trim();
                if (trimmed.Length == 0 || !trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string suffix = trimmed.Substring(prefix.Length);
                if (int.TryParse(suffix, out int value))
                {
                    best = Math.Max(best, value);
                }
            }

            return best;
        }
    }
}
