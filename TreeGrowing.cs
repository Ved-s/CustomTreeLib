using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace CustomTreeLib
{
    public static class TreeGrowing
    {
		static bool prevLeftBranch = false;
		static bool prevRightBranch = false;

        public static bool GrowTree(int x, int y, CustomTree tree)
        {
			prevLeftBranch = false;
			prevRightBranch = false;

			int groundY = y;
			while (Main.tile[x, groundY].TileType == tree.Sapling.Type)
			{
				groundY++;
			}
			if (Main.tile[x - 1, groundY - 1].LiquidAmount != 0 || Main.tile[x, groundY - 1].LiquidAmount != 0 || Main.tile[x + 1, groundY - 1].LiquidAmount != 0)
			{
				return false;
			}
			Tile ground = Main.tile[x, groundY];
			if (!ground.HasUnactuatedTile || ground.IsHalfBlock || ground.Slope != SlopeType.Solid)
			{
				return false;
			}
			if (!tree.ValidGroundType(ground.TileType) || !tree.ValidWallType(Main.tile[x, groundY - 1].WallType))
			{
				return false;
			}

            Tile groundLeft = Main.tile[x - 1, groundY];
            Tile groundRight = Main.tile[x + 1, groundY];
            if (
				   (!groundLeft.HasTile  || !tree.ValidGroundType(groundLeft.TileType)) 
				&& (!groundRight.HasTile || !tree.ValidGroundType(groundRight.TileType)))
			{
				return false;
			}
			byte color = Main.tile[x, groundY].TileColor;
			int treeHeight = WorldGen.genRand.Next(tree.MinHeight, tree.MaxHeight);
			int treeHeightWithTop = treeHeight + tree.TopPadding;
			if (!WorldGen.EmptyTileCheck(x - 2, x + 2, groundY - treeHeightWithTop, groundY - 1, 20))
			{
				return false;
			}

			int treeBottom = groundY - 1;
			int treeTop = treeBottom - treeHeight;

			for (int i = treeBottom; i >= treeTop; i--)
			{
				if (i == treeBottom) PlaceBottom(x, i, color, tree, treeHeight == 1);
				else if (i > treeTop) PlaceMiddle(x, i, color, tree);
				else PlaceTop(x, i, color, tree);
			}

			WorldGen.RangeFrame(x - 2, treeTop - 1, x + 2, treeBottom + 1);
			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendTileSquare(-1, x - 1, treeTop, 3, treeHeight, TileChangeType.None);
			}

			return true;
		}

		public static void PlaceBottom(int x, int y, byte color, CustomTree tree, bool top) 
		{
			bool rootRight = WorldGen.genRand.NextBool(tree.RootChance);
			bool rootLeft = WorldGen.genRand.NextBool(tree.RootChance);

			int style = WorldGen.genRand.Next(3);

			if (rootRight) Place(x + 1, y, new(style, TreeTileSide.Right, TreeTileType.Root), color, tree);
			if (rootLeft) Place(x - 1, y, new(style, TreeTileSide.Left, TreeTileType.Root), color, tree);

			if (rootLeft || rootRight)
				Place(x, y, new(style, GetSide(rootLeft, rootRight), top ? TreeTileType.TopWithRoots : TreeTileType.WithRoots), color, tree);
			else PlaceNormal(x, y, color, tree);
		}

		public static void PlaceMiddle(int x, int y, byte color, CustomTree tree)
		{
			bool branchRight = WorldGen.genRand.NextBool(tree.BranchChance);
			bool branchLeft = WorldGen.genRand.NextBool(tree.BranchChance);

			int style = WorldGen.genRand.Next(3);

			if (prevLeftBranch && branchLeft) branchLeft = false;
			if (prevRightBranch && branchRight) branchRight = false;

			prevLeftBranch = branchLeft;
			prevRightBranch = branchRight;

			if (branchRight)
				Place(x + 1, y, 
					new(style, TreeTileSide.Right, 
					WorldGen.genRand.NextBool(tree.NotLeafyBranchChance) ? TreeTileType.Branch : TreeTileType.LeafyBranch),
					color, tree);
			if (branchLeft)
				Place(x - 1, y,
					new(style, TreeTileSide.Left,
					WorldGen.genRand.NextBool(tree.NotLeafyBranchChance) ? TreeTileType.Branch : TreeTileType.LeafyBranch),
					color, tree);

			if (branchRight || branchLeft)
				Place(x, y, new(style, GetSide(branchLeft, branchRight), TreeTileType.WithBranches), color, tree);
			else PlaceNormal(x, y, color, tree);
		}

		public static void PlaceTop(int x, int y, byte color, CustomTree tree)
		{
			if (WorldGen.genRand.NextBool(tree.BrokenTopChance))
				Place(x, y, new(TreeTileType.BrokenTop), color, tree);
			else Place(x, y, new(TreeTileType.LeafyTop), color, tree);
		}

		public static void PlaceNormal(int x, int y, byte color, CustomTree tree) 
		{
			int bark = 0;

			if (WorldGen.genRand.NextBool(tree.LessBarkChance)) bark--;
			if (WorldGen.genRand.NextBool(tree.MoreBarkChance)) bark++;

			TreeTileSide side = WorldGen.genRand.NextBool() ? TreeTileSide.Left : TreeTileSide.Right;

			if (bark == 0) Place(x, y, new(TreeTileType.Normal), color, tree);
			else if (bark < 0) Place(x, y, new(side, TreeTileType.LessBark), color, tree);
			else Place(x, y, new(side, TreeTileType.MoreBark), color, tree);
		}

		public static void Place(int x, int y, TreeTileInfo info, byte color, CustomTree tree) 
		{
			Tile t = Main.tile[x, y];

			t.HasTile = true;
			t.TileType = tree.Tile.Type;
			info.ApplyToTile(t);
			t.TileColor = color;
		}

		public static TreeTileSide GetSide(bool left, bool right)
		{
			if (left && right || !left && !right) return TreeTileSide.Center;
			if (left) return TreeTileSide.Left;
			return TreeTileSide.Right;
		}
	}
}
