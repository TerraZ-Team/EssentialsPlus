using System;
using EssentialsPlus.Extensions;
using Terraria;

namespace EssentialsPlus.Services
{
    public enum TeleportDirection
    {
        Down,
        Left,
        Right,
        Up
    }

    public static class TeleportNavigator
    {
        public static bool TryFindTeleportTarget(int tileX, int tileY, int levels, TeleportDirection direction, out int targetX, out int targetY, out int stepsFound)
        {
            targetX = Clamp(tileX, 0, Main.maxTilesX - 2);
            targetY = Clamp(tileY, 0, Main.maxTilesY - 3);
            stepsFound = 0;

            if (levels <= 0)
            {
                return false;
            }

            switch (direction)
            {
                case TeleportDirection.Down:
                    {
                        bool empty = false;
                        int startY = Clamp(tileY + 3, 0, Main.maxTilesY - 3);
                        targetY = startY;
                        for (int y = startY; levels > 0 && y < Main.maxTilesY - 2; y++)
                        {
                            if (Is2x3Empty(targetX, y))
                            {
                                empty = true;
                            }
                            else if (empty)
                            {
                                empty = false;
                                levels--;
                                targetY = y;
                                stepsFound++;
                            }
                        }
                        return stepsFound > 0;
                    }
                case TeleportDirection.Left:
                    {
                        bool solid = false;
                        int startY = Clamp(tileY, 0, Main.maxTilesY - 3);
                        targetY = startY;
                        for (int x = targetX; levels > 0 && x >= 0; x--)
                        {
                            if (Is2x3Empty(x, targetY))
                            {
                                if (solid)
                                {
                                    solid = false;
                                    levels--;
                                    targetX = x;
                                    stepsFound++;
                                }
                            }
                            else
                            {
                                solid = true;
                            }
                        }
                        return stepsFound > 0;
                    }
                case TeleportDirection.Right:
                    {
                        bool solid = false;
                        int startX = Clamp(tileX + 1, 0, Main.maxTilesX - 2);
                        int startY = Clamp(tileY, 0, Main.maxTilesY - 3);
                        targetX = startX;
                        targetY = startY;
                        for (int x = startX; levels > 0 && x < Main.maxTilesX - 1; x++)
                        {
                            if (Is2x3Empty(x, targetY))
                            {
                                if (solid)
                                {
                                    solid = false;
                                    levels--;
                                    targetX = x;
                                    stepsFound++;
                                }
                            }
                            else
                            {
                                solid = true;
                            }
                        }
                        return stepsFound > 0;
                    }
                case TeleportDirection.Up:
                    {
                        bool solid = false;
                        int startY = Clamp(tileY, 0, Main.maxTilesY - 3);
                        targetY = startY;
                        for (int y = startY; levels > 0 && y >= 0; y--)
                        {
                            if (Is2x3Empty(targetX, y))
                            {
                                if (solid)
                                {
                                    solid = false;
                                    levels--;
                                    targetY = y;
                                    stepsFound++;
                                }
                            }
                            else
                            {
                                solid = true;
                            }
                        }
                        return stepsFound > 0;
                    }
                default:
                    return false;
            }
        }

        private static bool Is2x3Empty(int x, int y)
        {
            return Main.tile[x, y].IsEmpty() && Main.tile[x + 1, y].IsEmpty()
                && Main.tile[x, y + 1].IsEmpty() && Main.tile[x + 1, y + 1].IsEmpty()
                && Main.tile[x, y + 2].IsEmpty() && Main.tile[x + 1, y + 2].IsEmpty();
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }
    }
}
