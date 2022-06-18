using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace CustomTreeLib
{
    internal class GlobalTile : Terraria.ModLoader.GlobalTile
    {
        public override void RandomUpdate(int i, int j, int type)
        {
            TreeSettings? settings = TreeLoader.GetTreeSettings(i, j);

            if (settings.HasValue)
            {
                TreeTileInfo info = TreeTileInfo.GetInfo(i, j);
                if (info.Type != TreeTileType.LeafyTop) return;

                if (TreeLoader.CanGrowMore(new(i, j), settings.Value, TreeGrowing.GetTreeStats(i, j)))
                    TreeGrowing.TryGrowHigher(i, j, settings.Value);                
            }
        }
    }
}
