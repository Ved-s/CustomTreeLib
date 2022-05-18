using Microsoft.Xna.Framework;
using Terraria;

namespace CustomTreeLib
{
    public struct TreeTileInfo
    {
        public int Style;
        public TreeTileSide Side;
        public TreeTileType Type;

        public bool IsLeafy => Type == TreeTileType.LeafyBranch || Type == TreeTileType.LeafyTop;
        public bool IsWoody => !IsLeafy;

        public bool WithBranches => Type == TreeTileType.WithBranches || Type == TreeTileType.TopWithBranches;
        public bool WithRoots => Type == TreeTileType.WithRoots || Type == TreeTileType.TopWithRoots;

        public bool IsTop =>
               Type == TreeTileType.Top
            || Type == TreeTileType.TopWithBranches
            || Type == TreeTileType.TopWithRoots
            || Type == TreeTileType.BrokenTop
            || Type == TreeTileType.LeafyTop;

        public TreeTileInfo(int style, TreeTileSide side, TreeTileType type)
        {
            Style = style;
            Side = side;
            Type = type;
        }

        public TreeTileInfo(TreeTileSide side, TreeTileType type)
        {
            Style = WorldGen.genRand.Next(3);
            Side = side;
            Type = type;
        }

        public TreeTileInfo(TreeTileType type)
        {
            Style = WorldGen.genRand.Next(3);
            Side = TreeTileSide.Center;
            Type = type;
        }

        public static TreeTileInfo GetInfo(int x, int y) => GetInfo(Main.tile[x, y]);

        public static TreeTileInfo GetInfo(Tile t)
        {
            Point frame = new(t.TileFrameX, t.TileFrameY);

            int style = (frame.Y & 66) / 22;
            frame.Y /= 66;
            frame.X /= 22;

            switch (frame.X)
            {
                case 0:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Center, TreeTileType.Normal);
                        case 1: return new(style, TreeTileSide.Left, TreeTileType.LessBark);
                        case 2: return new(style, TreeTileSide.Right, TreeTileType.WithRoots);
                        case 3: return new(style, TreeTileSide.Center, TreeTileType.BrokenTop);
                    }
                    break;

                case 1:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Right, TreeTileType.LessBark);
                        case 1: return new(style, TreeTileSide.Right, TreeTileType.MoreBark);
                        case 2: return new(style, TreeTileSide.Right, TreeTileType.Root);
                        case 3: return new(style, TreeTileSide.Center, TreeTileType.LeafyTop);
                    }
                    break;

                case 2:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Right, TreeTileType.WithBranches);
                        case 1: return new(style, TreeTileSide.Left, TreeTileType.MoreBark);
                        case 2: return new(style, TreeTileSide.Left, TreeTileType.Root);
                        case 3: return new(style, TreeTileSide.Left, TreeTileType.LeafyBranch);
                    }
                    break;

                case 3:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Left, TreeTileType.Branch);
                        case 1: return new(style, TreeTileSide.Right, TreeTileType.WithBranches);
                        case 2: return new(style, TreeTileSide.Left, TreeTileType.WithRoots);
                        case 3: return new(style, TreeTileSide.Right, TreeTileType.LeafyBranch);
                    }
                    break;

                case 4:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Left, TreeTileType.WithBranches);
                        case 1: return new(style, TreeTileSide.Right, TreeTileType.Branch);
                        case 2: return new(style, TreeTileSide.Center, TreeTileType.WithRoots);
                    }
                    break;

                case 5:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Center, TreeTileType.Top);
                        case 1: return new(style, TreeTileSide.Center, TreeTileType.WithBranches);
                    }
                    break;

                case 6:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Left, TreeTileType.TopWithBranches);
                        case 1: return new(style, TreeTileSide.Right, TreeTileType.TopWithBranches);
                        case 2: return new(style, TreeTileSide.Center, TreeTileType.TopWithBranches);
                    }
                    break;

                case 7:
                    switch (frame.Y)
                    {
                        case 0: return new(style, TreeTileSide.Left, TreeTileType.TopWithRoots);
                        case 1: return new(style, TreeTileSide.Right, TreeTileType.TopWithRoots);
                        case 2: return new(style, TreeTileSide.Center, TreeTileType.TopWithRoots);
                    }
                    break;
            }

            return new(style, TreeTileSide.Center, TreeTileType.None);
        }

        public void ApplyToTile(int x, int y) => ApplyToTile(Main.tile[x, y]);

        public void ApplyToTile(Tile t)
        {
            Point frame = default;

            switch (Type)
            {
                case TreeTileType.LessBark:
                    if (Side == TreeTileSide.Left) frame = new(0, 3);
                    else frame = new(1, 0);
                    break;

                case TreeTileType.Branch:
                    if (Side == TreeTileSide.Left) frame = new(3, 0);
                    else frame = new(4, 3);
                    break;

                case TreeTileType.BrokenTop:
                    frame = new(0, 9);
                    break;

                case TreeTileType.MoreBark:
                    if (Side == TreeTileSide.Left) frame = new(1, 3);
                    else frame = new(2, 3);
                    break;

                case TreeTileType.WithBranches:
                    if (Side == TreeTileSide.Left) frame = new(4, 0);
                    else if (Side == TreeTileSide.Right) frame = new(3, 3);
                    else frame = new(5, 3);
                    break;

                case TreeTileType.LeafyBranch:
                    if (Side == TreeTileSide.Left) frame = new(2, 9);
                    else frame = new(3, 9);
                    break;

                case TreeTileType.WithRoots:
                    if (Side == TreeTileSide.Left) frame = new(3, 6);
                    else if (Side == TreeTileSide.Right) frame = new(0, 6);
                    else frame = new(4, 6);
                    break;

                case TreeTileType.Root:
                    if (Side == TreeTileSide.Left) frame = new(2, 6);
                    else frame = new(1, 6);
                    break;

                case TreeTileType.Top:
                    frame = new(5, 0);
                    break;

                case TreeTileType.TopWithBranches:
                    if (Side == TreeTileSide.Left) frame = new(6, 0);
                    else if (Side == TreeTileSide.Right) frame = new(6, 3);
                    else frame = new(6, 6);
                    break;

                case TreeTileType.TopWithRoots:
                    if (Side == TreeTileSide.Left) frame = new(7, 0);
                    else if (Side == TreeTileSide.Right) frame = new(7, 3);
                    else frame = new(7, 6);
                    break;

                case TreeTileType.LeafyTop:
                    frame = new(1, 9);
                    break;
            }

            frame.Y += Style;

            t.TileFrameX = (short)(frame.X * 22);
            t.TileFrameY = (short)(frame.Y * 22);
        }

        public override string ToString()
        {
            return $"{Side} {Type} ({Style})";
        }

    }
    public enum TreeTileSide { Left, Center, Right }
    public enum TreeTileType
    {
        None,
        Normal,
        LessBark,
        MoreBark,
        WithBranches,
        Branch,
        LeafyBranch,
        WithRoots,
        Root,
        Top,
        TopWithBranches,
        TopWithRoots,
        BrokenTop,
        LeafyTop
    }

}
