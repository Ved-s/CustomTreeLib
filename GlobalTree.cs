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
    public class GlobalTree : GlobalTile
    {
        public override void RandomUpdate(int i, int j, int type)
        {
            if (CustomTree.TryGetTreeSettingsByType(type, out TreeSettings settings))
            {
                TreeTileInfo info = TreeTileInfo.GetInfo(i, j);
                if (info.Type != TreeTileType.LeafyTop) return;

                if (settings.CanGrowMoreCheck(new(i, j), settings, TreeGrowing.GetTreeStats(i, j)))
                    TreeGrowing.TryGrowHigher(i, j, settings);                
            }
        }
    }
}
