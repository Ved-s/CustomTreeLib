using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace CustomTreeLib
{
    internal class CustomTreeSystem : ModSystem
    {
        bool NeedsFrameConversion = true;

        public override void Load()
        {
            WorldFile.OnWorldLoad += WorldFile_OnWorldLoad;
        }

        public override void Unload()
        {
            WorldFile.OnWorldLoad -= WorldFile_OnWorldLoad;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey("newFrame"))
                NeedsFrameConversion = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["newFrame"] = true;
        }

        private void WorldFile_OnWorldLoad()
        {
            if (NeedsFrameConversion)
            {
                for (int i = 0; i < Main.maxTilesX; i++)
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        Tile t = Main.tile[i, j];

                        if (CustomTree.ByTileType.ContainsKey(t.TileType))
                        {
                            t.TileFrameX = (short)(t.TileFrameX / 22 * 18);
                            t.TileFrameY = (short)(t.TileFrameY / 22 * 18);
                        }
                    }
                NeedsFrameConversion = false;
            }
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            tasks.Add(new TreeGenPass());
            totalWeight += 300;
        }
    }
}
