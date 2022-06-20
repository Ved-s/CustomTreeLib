using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;

#pragma warning disable CS1591

namespace CustomTreeLib
{
    public static class TreeLoader
    {
        enum HookID
        {
            Shake,
            ModifyTreeSettings,
            CanGrowMore,
            PreDrawFoliage,
            PostDrawFoliage
        }

        static Dictionary<HookID, List<GlobalTree>> Hooks;
        static Dictionary<HookID, string> HookNames;

        static TreeLoader()
        {
            Hooks = new(Enum.GetValues<HookID>().Select(id => new KeyValuePair<HookID, List<GlobalTree>>(id, new())));
            HookNames = new(Enum.GetValues<HookID>().Select(id => new KeyValuePair<HookID, string>(id, id.ToString())));
        }

        internal static void LoadGlobal(GlobalTree globalTree)
        {
            foreach (HookID id in HookNames.Keys)
                AddHookIfOverridden(globalTree, id);
        }
        internal static void UnloadGlobal(GlobalTree globalTree)
        {
            foreach (var hook in Hooks.Values)
                hook.Remove(globalTree);
        }
        static void AddHookIfOverridden(GlobalTree tree, HookID id)
        {
            Type t = tree.GetType();
            MethodInfo info = t.GetMethod(HookNames[id]);
            if (info is null || info.DeclaringType == typeof(GlobalTree) || Hooks[id].Contains(tree)) return;
            Hooks[id].Add(tree);
        }

        public static bool Shake(int x, int y, ref bool createLeaves)
        {
            Tile tile = Main.tile[x, y];

            foreach (GlobalTree global in Hooks[HookID.Shake])
                if (!global.Shake(x, y, tile.TileType, ref createLeaves))
                    return false;

            if (CustomTree.ByTileType.TryGetValue(tile.TileType, out CustomTree tree))
            {
                return tree.Shake(x, y, ref createLeaves);
            }
            return true;
        }
        public static TreeSettings? GetTreeSettings(int x, int y)
        {
            TreeSettings settings = default;
            bool result = false;

            Tile t = Framing.GetTileSafely(x, y);
            switch (t.TileType)
            {
                case 583:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Topaz);
                    result = true;
                    break;
                case 584:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Amethyst);
                    result = true;
                    break;
                case 585:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Sappphire);
                    result = true;
                    break;
                case 586:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Emerald);
                    result = true;
                    break;
                case 587:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Ruby);
                    result = true;
                    break;
                case 588:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Diamond);
                    result = true;
                    break;
                case 589:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.GemTree_Amber);
                    result = true;
                    break;
                case 596:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.VanityTree_Sakura);
                    result = true;
                    break;
                case 616:
                    settings = new(WorldGen.GrowTreeSettings.Profiles.VanityTree_Willow);
                    result = true;
                    break;
                case 5:
                    settings = TreeSettings.VanillaCommonTree;
                    result = true;
                    break;
            }

            if (CustomTree.ByTileType.TryGetValue(t.TileType, out CustomTree tree))
            {
                settings = tree.GetTreeSettings();
                result = true;
            }

            if (result)
                foreach (GlobalTree global in Hooks[HookID.ModifyTreeSettings])
                    global.ModifyTreeSettings(x, y, t.TileType, ref settings);

            return result? settings : null;
        }
        public static bool CanGrowMore(Point topPos, TreeSettings settings, TreeStats stats)
        {
            foreach (GlobalTree global in Hooks[HookID.CanGrowMore])
                if (!global.CanGrowMore(topPos, settings, stats))
                    return false;

            if (CustomTree.ByTileType.TryGetValue(settings.TreeTileType, out CustomTree tree))
                return tree.CanGrowMore(topPos, settings, stats);

            float mod = (settings.MaxHeight / 17f);
            return stats.LeafyBranches < 3 * mod && stats.TotalBranches < 5 * mod && stats.TotalBlocks < 20 * mod;
        }
        public static bool PreDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            foreach (GlobalTree global in Hooks[HookID.PreDrawFoliage])
                if (!global.PreDrawFoliage(type, position, size, foliageType, treeFrame, origin, color, rotation))
                    return false;

            if (CustomTree.ByTileType.TryGetValue(type, out CustomTree tree))
                return tree.PreDrawFoliage(position, size, foliageType, treeFrame, origin, color, rotation);

            return true;
        }
        public static void PostDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            foreach (GlobalTree global in Hooks[HookID.PostDrawFoliage])
                global.PostDrawFoliage(type, position, size, foliageType, treeFrame, origin, color, rotation);

            if (CustomTree.ByTileType.TryGetValue(type, out CustomTree tree))
                tree.PostDrawFoliage(position, size, foliageType, treeFrame, origin, color, rotation);
        }
    }
}
