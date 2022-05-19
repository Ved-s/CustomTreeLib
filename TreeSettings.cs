using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace CustomTreeLib
{
    public struct TreeSettings
    {
        public ushort TreeTileType;

        public Func<int, bool> GroundTypeCheck, WallTypeCheck;
        public CanTreeGrowMoreDelegate CanGrowMoreCheck;

        public int MinHeight;
        public int MaxHeight;
        public int TopPaddingNeeded;

        public int NoRootChance;
        public int LessBarkChance;
        public int MoreBarkChance;
        public int BranchChance;
        public int NotLeafyBranchChance;
        public int BrokenTopChance;

        public TreeSettings(WorldGen.GrowTreeSettings vanillaSettings) 
        {
            TreeTileType = vanillaSettings.TreeTileType;

            GroundTypeCheck = (t) => vanillaSettings.GroundTest(t);
            WallTypeCheck = (t) => vanillaSettings.WallTest(t);

            MinHeight = vanillaSettings.TreeHeightMin;
            MaxHeight = vanillaSettings.TreeHeightMax;

            TopPaddingNeeded = vanillaSettings.TreeTopPaddingNeeded;

            NoRootChance = 3;
            LessBarkChance = 7;
            MoreBarkChance = 7;
            BranchChance = 4;
            NotLeafyBranchChance = 3;
            BrokenTopChance = 13;

            CanGrowMoreCheck = VanillaTreeCanGrowMore;
        }

        public static TreeSettings VanillaCommonTree = new()
        {
            TreeTileType = TileID.Trees,

            GroundTypeCheck = (t) => WorldGen.IsTileTypeFitForTree((ushort)t),
            WallTypeCheck = WorldGen.DefaultTreeWallTest,

            MinHeight = 5,
            MaxHeight = 17,

            TopPaddingNeeded = 4,
            BranchChance = 4,
            MoreBarkChance = 7,
            LessBarkChance = 7,
            NotLeafyBranchChance = 3,
            BrokenTopChance = 13,
            NoRootChance = 3,

            CanGrowMoreCheck = VanillaTreeCanGrowMore,
        };

        public static bool VanillaTreeCanGrowMore(Point topPos, TreeSettings settings, TreeStats stats) 
        {
            float mod = (settings.MaxHeight / 17f);

            return stats.LeafyBranches < 3 * mod && stats.TotalBranches < 5 * mod && stats.TotalBlocks < 20 * mod;
        }
    }

    public delegate bool CanTreeGrowMoreDelegate(Point topPos, TreeSettings settings, TreeStats stats);
}
