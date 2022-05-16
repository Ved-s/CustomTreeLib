using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CustomTreeLib
{
	public class CustomTreeLib : Mod
	{
    }

    // Example tree
    public class ExampleTree : CustomTree
    {
        public override string SaplingTexture => "CustomTreeLib/ExampleSapling";
        public override string AcornTexture => "CustomTreeLib/ExampleAcorn";
        public override string TileTexture => "Terraria/Images/Tiles_583";
        public override string TopTexture => "Terraria/Images/Tree_Tops_30";
        public override string BranchTexture => "Terraria/Images/Tree_Branches_26";

        // either Leaf Texture or LeafType (to tree leaf gore) can be set
        public override string LeafTexture => "CustomTreeLib/ExampleLeaf";

        public override int[] ValidGroundTiles => new int[] { TileID.Grass };

        public override int SaplingStyles => 3;
        public override int GrowChance => 1;

        public override Color? MapColor => Color.Yellow;
        public override string MapName => "Example Tree";

        public override Color? SaplingMapColor => Color.Orange;
        public override string SaplingMapName => "Example Sapling";

        public override bool Shake(int x, int y, ref bool createLeaves)
        {
            createLeaves = true;
            Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16, ItemID.StoneBlock);
            return false;
        }
        public override bool Drop(int x, int y)
        {
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Vector2(x, y) * 16, ItemID.DirtBlock);
            return false;
        }
        public override bool GetTreeFoliageData(int i, int j, int xoffset, ref int treeFrame, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight)
        {
            topTextureFrameWidth = 118;
            topTextureFrameHeight = 96;
            floorY = 0;
            return true;
        }
    }
}