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

        public static bool GrowTree(int x, int y, TreeSettings settings)
        {
			prevLeftBranch = false;
			prevRightBranch = false;

			int groundY = y;
			while (TileID.Sets.TreeSapling[Main.tile[x, groundY].TileType] || TileID.Sets.CommonSapling[Main.tile[x, groundY].TileType])
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
			if (!settings.GroundTypeCheck(ground.TileType) || !settings.WallTypeCheck(Main.tile[x, groundY - 1].WallType))
			{
				return false;
			}

            Tile groundLeft = Main.tile[x - 1, groundY];
            Tile groundRight = Main.tile[x + 1, groundY];
            if (
				   (!groundLeft.HasTile  || !settings.GroundTypeCheck(groundLeft.TileType)) 
				&& (!groundRight.HasTile || !settings.GroundTypeCheck(groundRight.TileType)))
			{
				return false;
			}
			byte color = Main.tile[x, groundY].TileColor;
			int treeHeight = WorldGen.genRand.Next(settings.MinHeight, settings.MaxHeight);
			int treeHeightWithTop = treeHeight + settings.TopPaddingNeeded;
			if (!WorldGen.EmptyTileCheck(x - 2, x + 2, groundY - treeHeightWithTop, groundY - 1, 20))
			{
				return false;
			}

			int treeBottom = groundY - 1;
			int treeTop = treeBottom - treeHeight;

			for (int i = treeBottom; i >= treeTop; i--)
			{
				if (i == treeBottom) PlaceBottom(x, i, color, settings, treeHeight == 1);
				else if (i > treeTop) PlaceMiddle(x, i, color, settings);
				else PlaceTop(x, i, color, settings);
			}

			WorldGen.RangeFrame(x - 2, treeTop - 1, x + 2, treeBottom + 1);
			if (Main.netMode == NetmodeID.Server)
			{
				NetMessage.SendTileSquare(-1, x - 1, treeTop, 3, treeHeight, TileChangeType.None);
			}

			return true;
		}

		public static void PlaceBottom(int x, int y, byte color, TreeSettings settings, bool top) 
		{
			bool rootRight = !WorldGen.genRand.NextBool(settings.NoRootChance);
			bool rootLeft = !WorldGen.genRand.NextBool(settings.NoRootChance);

			int style = WorldGen.genRand.Next(3);

			if (rootRight) Place(x + 1, y, new(style, TreeTileSide.Right, TreeTileType.Root), color, settings);
			if (rootLeft) Place(x - 1, y, new(style, TreeTileSide.Left, TreeTileType.Root), color, settings);

			if (rootLeft || rootRight)
				Place(x, y, new(style, GetSide(rootLeft, rootRight), top ? TreeTileType.TopWithRoots : TreeTileType.WithRoots), color, settings);
			else PlaceNormal(x, y, color, settings);
		}

		public static void PlaceMiddle(int x, int y, byte color, TreeSettings settings)
		{
			bool branchRight = WorldGen.genRand.NextBool(settings.BranchChance);
			bool branchLeft = WorldGen.genRand.NextBool(settings.BranchChance);

			int style = WorldGen.genRand.Next(3);

			if (prevLeftBranch && branchLeft) branchLeft = false;
			if (prevRightBranch && branchRight) branchRight = false;

			prevLeftBranch = branchLeft;
			prevRightBranch = branchRight;

			if (branchRight)
				Place(x + 1, y, 
					new(style, TreeTileSide.Right, 
					WorldGen.genRand.NextBool(settings.NotLeafyBranchChance) ? TreeTileType.Branch : TreeTileType.LeafyBranch),
					color, settings);
			if (branchLeft)
				Place(x - 1, y,
					new(style, TreeTileSide.Left,
					WorldGen.genRand.NextBool(settings.NotLeafyBranchChance) ? TreeTileType.Branch : TreeTileType.LeafyBranch),
					color, settings);

			if (branchRight || branchLeft)
				Place(x, y, new(style, GetSide(branchLeft, branchRight), TreeTileType.WithBranches), color, settings);
			else PlaceNormal(x, y, color, settings);
		}

		public static void PlaceTop(int x, int y, byte color, TreeSettings settings)
		{
			if (WorldGen.genRand.NextBool(settings.BrokenTopChance))
				Place(x, y, new(TreeTileType.BrokenTop), color, settings);
			else Place(x, y, new(TreeTileType.LeafyTop), color, settings);
		}

		public static void PlaceNormal(int x, int y, byte color, TreeSettings settings) 
		{
			int bark = 0;

			if (WorldGen.genRand.NextBool(settings.LessBarkChance)) bark--;
			if (WorldGen.genRand.NextBool(settings.MoreBarkChance)) bark++;

			TreeTileSide side = WorldGen.genRand.NextBool() ? TreeTileSide.Left : TreeTileSide.Right;

			if (bark == 0) Place(x, y, new(TreeTileType.Normal), color, settings);
			else if (bark < 0) Place(x, y, new(side, TreeTileType.LessBark), color, settings);
			else Place(x, y, new(side, TreeTileType.MoreBark), color, settings);
		}

		public static void Place(int x, int y, TreeTileInfo info, byte color, TreeSettings settings) 
		{
			Tile t = Main.tile[x, y];

			t.HasTile = true;
			t.TileType = settings.TreeTileType;
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

			foreach (PositionedTreeTile tile in EnumerateTreeTiles(x, y)) 
			{
				stats.TotalBlocks++;

				switch (tile.Info.Type)
				{
					case TreeTileType.Branch:
						stats.TotalBranches++;
						break;

					case TreeTileType.LeafyBranch:
						stats.TotalBranches++;
						stats.LeafyBranches++;
						break;

					case TreeTileType.Root:
						if (tile.Info.Side == TreeTileSide.Left) stats.LeftRoot = true;
						else stats.RightRoot = true;
						break;

					case TreeTileType.BrokenTop:
						stats.HasTop = true;
						stats.BrokenTop = true;
						break;

					case TreeTileType.LeafyTop:
						stats.HasTop = true;
						break;
				}
				if (tile.Info.IsCenter) 
				{
					stats.Bottom.X = tile.Pos.X;
					stats.Top.X = tile.Pos.X;

					stats.Top.Y = Math.Min(tile.Pos.Y, stats.Top.Y);
					stats.Bottom.Y = Math.Max(tile.Pos.Y, stats.Bottom.Y);
				}
			}
			stats.GroundType = Framing.GetTileSafely(stats.Bottom.X, stats.Bottom.Y + 1).TileType;
			return stats;
        }

		public static IEnumerable<PositionedTreeTile> EnumerateTreeTiles(int x, int y) 
		{
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

				TreeTileInfo info = TreeTileInfo.GetInfo(t);

				yield return new(p, info);

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
						break;

					case TreeTileType.LeafyBranch:
						up = down = false;
						SetSide(info.Side, out right, out left);
						break;

					case TreeTileType.WithRoots:
						down = false;
						SetSide(info.Side, out left, out right);
						break;

					case TreeTileType.Root:
						up = down = false;
						SetSide(info.Side, out right, out left);
						break;

					case TreeTileType.BrokenTop:
						up = false;
						break;

					case TreeTileType.LeafyTop:
						up = false;
						break;
				}

				if (up) queue.Enqueue(new(p.X, p.Y - 1));
				if (down) queue.Enqueue(new(p.X, p.Y + 1));
				if (left) queue.Enqueue(new(p.X - 1, p.Y));
				if (right) queue.Enqueue(new(p.X + 1, p.Y));
			}
		}

		public static bool TryGrowHigher(int topX, int topY, TreeSettings settings)
		{
			if (!WorldGen.EmptyTileCheck(topX - 2, topX + 2, topY - 1 - settings.TopPaddingNeeded, topY - 1, 20))
			{
				return false;
			}

			Tile t = Main.tile[topX, topY];

			TreeTileInfo below = TreeTileInfo.GetInfo(topX, topY+1);

			if (below.Type == TreeTileType.WithBranches)
				SetSide(below.Side, out prevLeftBranch, out prevRightBranch);

			TreeTileInfo info = TreeTileInfo.GetInfo(t);

			PlaceMiddle(topX, topY, t.TileColor, settings);
			Place(topX, topY-1, info, t.TileColor, settings);

			if (Main.netMode != NetmodeID.Server)
			{
				Patches.Instance.TileDrawing_AddSpecialPoint(Main.instance.TilesRenderer, topX, topY - 1, 0);
			}

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

		public Point Top = new(0, int.MaxValue);
		public Point Bottom;
		public ushort GroundType;

        public override string ToString()
        {
			return $"T:{TotalBlocks} " +
				$"B:{TotalBranches} " +
				$"BL:{LeafyBranches} " +
				$"{(HasTop?(BrokenTop?"TB ":"TL "):"")}" +
				$"{(LeftRoot?"Rl ":"")}" +
				$"{(RightRoot?"Rr ":"")}" +
				$"X:{Top.X} Y:{Top.Y}-{Bottom.Y} G:{GroundType}";
        }
    }

	public record struct PositionedTreeTile(Point Pos, TreeTileInfo Info);
}
