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
		public static void SetSide(TreeTileSide side, out bool left, out bool right)
		{
			left = right = false;

			if (side == TreeTileSide.Center) left = right = true;
			else if (side == TreeTileSide.Left) left = true;
			else right = true;
		}

		public static TreeStats GetTreeStats(int x, int y) 
		{
			TreeStats stats = new();

			HashSet<Point> done = new();
			Queue<Point> queue = new();

			queue.Enqueue(new Point(x, y));

			while (queue.Count > 0)
			{
				Point p = queue.Dequeue();
				if (done.Contains(p)) 
					continue;

				done.Add(p);

				Tile t = Main.tile[p.X, p.Y];
				if (!t.HasTile || !TileID.Sets.IsATreeTrunk[t.TileType]) continue;

				stats.TotalBlocks++;

				TreeTileInfo info = TreeTileInfo.GetInfo(t);

				bool left = false;
				bool right = false;
				bool up = true;
				bool down = true;

                switch (info.Type)
                {
                    case TreeTileType.WithBranches:
						SetSide(info.Side, out left, out right);
                        break;

                    case TreeTileType.Branch:
						up = down = false;
						SetSide(info.Side, out right, out left);
						stats.TotalBranches++;
                        break;

                    case TreeTileType.LeafyBranch:
						up = down = false;
						SetSide(info.Side, out right, out left);
						stats.TotalBranches++;
						stats.LeafyBranches++;
						break;

                    case TreeTileType.WithRoots:
						down = false;
						SetSide(info.Side, out left, out right);
						break;

                    case TreeTileType.Root:
						up = down = false;
						SetSide(info.Side, out right, out left);
						if (info.Side == TreeTileSide.Left) stats.LeftRoot = true;
						else stats.RightRoot = true;
                        break;

                    case TreeTileType.BrokenTop:
						up = false;
						stats.HasTop = true;
						stats.BrokenTop = true;
                        break;

                    case TreeTileType.LeafyTop:
						up = false;
						stats.HasTop = true;
						break;
                }

				if (up) queue.Enqueue(new(p.X, p.Y - 1));
				if (down) queue.Enqueue(new(p.X, p.Y + 1));
				if (left) queue.Enqueue(new(p.X - 1, p.Y));
				if (right) queue.Enqueue(new(p.X + 1, p.Y));
            }

			return stats;
        }

		public static bool TryGrowHigher(int topX, int topY, CustomTree tree)
		{
			if (!WorldGen.EmptyTileCheck(topX - 2, topX + 2, topY - 1 - tree.TopPadding, topY - 1, 20))
			{
				return false;
			}

			Tile t = Main.tile[topX, topY];

			TreeTileInfo below = TreeTileInfo.GetInfo(topX, topY+1);

			if (below.Type == TreeTileType.WithBranches)
				SetSide(below.Side, out prevLeftBranch, out prevRightBranch);

			TreeTileInfo info = TreeTileInfo.GetInfo(t);

			PlaceMiddle(topX, topY, t.TileColor, tree);
			Place(topX, topY-1, info, t.TileColor, tree);

			WorldGen.SectionTileFrame(topX - 2, topY - 2, topX + 2, topY + 1);
			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendTileSquare(-1, topX - 1, topY - 1, 3, 2, TileChangeType.None);
			}

			return true;
		}
    }

    public struct TreeStats 
	{
		public int TotalBlocks;
		public int TotalBranches;
		public int LeafyBranches;

		public bool HasTop;
		public bool BrokenTop;

		public bool LeftRoot;
		public bool RightRoot;

        public override string ToString()
        {
			return $"T: {TotalBlocks} " +
				$"B: {TotalBranches} " +
				$"BL: {LeafyBranches} " +
				$"{(HasTop?(BrokenTop?"TB ":"TL "):"")}" +
				$"{(LeftRoot?"Rl ":"")}" +
				$"{(RightRoot?"Rr ":"")}";
        }
    }
}
