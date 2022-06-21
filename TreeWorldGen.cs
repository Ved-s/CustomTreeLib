using CustomTreeLib.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace CustomTreeLib
{
    public static class TreeWorldGen
    {
        /// <summary>
        /// Clear selected trees from world. Useful for debugging world gen
        /// </summary>
        public static void Clear(CustomTree[] trees)
        {
            HashSet<ushort> tilesToClear = new(trees.Select(t => t.Tile.Type));

            for (int x = 0; x < Main.maxTilesX; x++)
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Tile tile = Main.tile[x, y];

                    if (!tilesToClear.Contains(tile.TileType))
                        continue;

                    tile.ClearTile();
                }
        }

        /// <summary>
        /// Generates specified trees in world
        /// </summary>
        public static void Generate(CustomTree[] trees, Action<float> progress = null)
        {
            List<TreeWithSettings> settings = trees
                .Where(t => t is not null)
                .Select(t => new TreeWithSettings(t))
                .ToList();

            List<TreeWithSettings> validSettings = new();

            progress?.Invoke(0);
            int progressWidth = Main.maxTilesX - 40;
            for (int x = 20; x < Main.maxTilesX - 20; x++)
            {
                for (int y = 0; y < Main.maxTilesY - 20; y++)
                {
                    validSettings.Clear();
                    Tile tile = Main.tile[x, y];

                    validSettings.AddRange(settings.Where(s => (y - s.Settings.MinHeight) >= 0 && s.Settings.GroundTypeCheck(tile.TileType)));

                    while (validSettings.Count > 0)
                    {
                        int index = WorldGen.genRand.Next(0, validSettings.Count);
                        TreeWithSettings tws = validSettings[index];
                        validSettings.RemoveAt(index);

                        if (tws.Tree.TryGenerate(x, y))
                            continue;
                    }
                }
                progress?.Invoke((x - 20f) / progressWidth);
            }

            progress?.Invoke(1);
        }
    }

    internal record struct TreeWithSettings(TreeSettings Settings, CustomTree Tree)
    {
        public TreeWithSettings(CustomTree tree) : this(tree.GetTreeSettings(), tree)
        { }
    }
}
