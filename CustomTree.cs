using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace CustomTreeLib
{
    public abstract class CustomTree : ILoadable
    {
        //TODO: tree map color

        public static List<CustomTree> LoadedTrees = new();

        public static Dictionary<int, CustomTree> ByTileType = new();
        public static Dictionary<int, CustomTree> ByAcornType = new();

        public virtual string Name => GetType().Name;

        public abstract string SaplingTexture { get; }
        public abstract string AcornTexture { get; }
        public abstract string TileTexture { get; }
        public abstract string TopTexture { get; }
        public abstract string BranchTexture { get; }
        public virtual string LeafTexture { get; }

        public virtual int LeafType => Leaf is null? GoreID.TreeLeaf_Normal : Leaf.Type;
        public virtual byte? LeafColorOverride => null;

        public abstract int[] ValidGroundTiles { get; }
        public virtual int[] ValidWalls { get; } = new int[] { 0 };

        public virtual CustomTreeSapling Sapling { get; } = new();
        public virtual CustomTreeAcorn Acorn { get; } = new();
        public virtual CustomTreeTile Tile { get; } = new();
        public virtual CustomTreeLeaf Leaf { get; protected set; }

        public virtual int FoliageStyleFallback => 0;

        public virtual int GrowChance { get; set; } = 5;

        public virtual  int MinHeight { get; set; } = 8;
        public virtual  int MaxHeight { get; set; } = 20;

        public virtual int TopPadding { get; set; } = 4;
        public virtual int SaplingStyles { get; set; } = 1;

        public virtual Color? MapColor { get; set; }
        public virtual string MapName { get; set; }

        public virtual Color? SaplingMapColor { get; set; }
        public virtual string SaplingMapName { get; set; }

        public virtual string DefaultAcornName { get; set; } = null;

        public Texture2D TopTextureCache;
        public Texture2D BranchTextureCache;

        public TreePaintingSettings PaintingSettings = new()
        {
            UseSpecialGroups = false,
        };

        public virtual void Load(Mod mod)
        {
            Sapling.Tree = this;
            Acorn.Tree = this;
            Tile.Tree = this;

            if (LeafTexture is not null)
            {
                Leaf = new();
                Leaf.Tree = this;
            }

            mod.AddContent(Sapling);
            mod.AddContent(Acorn);
            mod.AddContent(Tile);
            if (Leaf is not null) 
                mod.AddContent(Leaf);

            LoadedTrees.Add(this);
            ByTileType[Tile.Type] = this;
            ByAcornType[Acorn.Type] = this;
        }

        public virtual void Unload()
        {
            LoadedTrees.Remove(this);
            ByTileType.Remove(Tile.Type);
            ByAcornType.Remove(Acorn.Type);
        }

        public WorldGen.GrowTreeSettings GetTreeGrowSettings()
        {
            return new()
            {
                SaplingTileType = Sapling.Type,
                TreeTileType = Tile.Type,
                GroundTest = ValidGroundTile,
                TreeHeightMax = MaxHeight,
                TreeHeightMin = MinHeight,
                TreeTopPaddingNeeded = TopPadding,
                WallTest = ValidWallType
            };
        }

        public virtual bool ValidGroundTile(int tile) => ValidGroundTiles.Contains(tile);
        public virtual bool ValidWallType(int tile) => ValidWalls.Contains(tile);

        public virtual void Grow(int x, int y) 
        {
            if (WorldGen.GrowTreeWithSettings(x, y, GetTreeGrowSettings()) && WorldGen.PlayerLOS(x, y))
            {
                WorldGen.TreeGrowFXCheck(x, y);
            }
        }
        public virtual bool Shake(int x, int y, ref bool createLeaves) => true;
        public virtual bool Drop(int x, int y) => true;
        public virtual void GetTreeLeaf(int x, Tile topTile, Tile t, ref int treeHeight, out int treeFrame, out int passStyle) 
        {
            treeFrame = 0;
            passStyle = LeafType;
        }

        public virtual bool GetTreeFoliageData(int i, int j, int xoffset, ref int treeFrame, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight)
        {
            int v = 0;
            return WorldGen.GetCommonTreeFoliageData(i, j, xoffset, ref treeFrame, ref v, out floorY, out topTextureFrameWidth, out topTextureFrameHeight);
        }
        public Texture2D GetFoliageTexture(bool branch) 
        {
            if (branch)
            {
                if (BranchTextureCache is null)
                    BranchTextureCache = ModContent.Request<Texture2D>(BranchTexture, AssetRequestMode.ImmediateLoad).Value;
                return BranchTextureCache;
            }

            if (TopTextureCache is null)
                TopTextureCache = ModContent.Request<Texture2D>(TopTexture, AssetRequestMode.ImmediateLoad).Value;
            return TopTextureCache;

        }

        public bool TileFrame(int i, int j)
        {
            WorldGen.CheckTreeWithSettings(i, j, new() { IsGroundValid = ValidGroundTile });
            return false;
        }

        public static bool IsBranchTile(int x, int y)
        {
            Tile t = Main.tile[x, y];
            return t.TileFrameX >= 22 && t.TileFrameY >= 198;
        }
    }

    public abstract class CustomTree<TAcornItem> : CustomTree where TAcornItem : CustomTreeAcorn, new()
    {
        public override TAcornItem Acorn { get; }
        public sealed override string AcornTexture => null;

        public CustomTree()
        {
            Acorn = new();
            Acorn.Tree = this;
        }
    }
    public abstract class CustomTree<TAcornItem, TSaplingTile> : CustomTree
        where TAcornItem : CustomTreeAcorn, new()
        where TSaplingTile : CustomTreeSapling, new()
    {
        public override TAcornItem Acorn { get; }
        public override TSaplingTile Sapling { get; }

        public override string AcornTexture => null;
        public override string SaplingTexture => null;

        public CustomTree()
        {
            Acorn = new();
            Acorn.Tree = this;

            Sapling = new();
            Sapling.Tree = this;
        }
    }
    public abstract class CustomTree<TAcornItem, TSaplingTile, TTreeTile> : CustomTree
        where TAcornItem : CustomTreeAcorn, new()
        where TSaplingTile : CustomTreeSapling, new()
        where TTreeTile : CustomTreeTile, new()
    {
        public override TAcornItem Acorn { get; }
        public override TSaplingTile Sapling { get; }
        public override TTreeTile Tile { get; }

        public override string AcornTexture => null;
        public override string SaplingTexture => null;
        public override string TileTexture => null;

        public CustomTree()
        {
            Acorn = new();
            Acorn.Tree = this;

            Sapling = new();
            Sapling.Tree = this;

            Tile = new();
            Tile.Tree = this;
        }
    }

    [Autoload(false)]
    public class CustomTreeSapling : ModTile
    {
        public bool IsDefault => GetType() == typeof(CustomTreeSapling);

        public override string Name => IsDefault ? Tree.Name + "Sapling" : base.Name;
        public override string Texture => IsDefault ? Tree.SaplingTexture : base.Texture;

        public CustomTree Tree { get; internal set; }
        
        public override void SetStaticDefaults()
        {
            TileID.Sets.CommonSapling[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;

            TileObjectData.newTile.Width = 1;
            TileObjectData.newTile.Height = 2;
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.AnchorValidTiles = Tree.ValidGroundTiles;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.DrawFlipHorizontal = true;
            TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.RandomStyleRange = Tree.SaplingStyles;
            TileObjectData.newTile.StyleMultiplier = 1;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);

            if (Tree.SaplingMapColor.HasValue)
            {
                if (Tree.SaplingMapName is not null)
                {
                    if (Tree.SaplingMapName.Any(c => char.IsWhiteSpace(c)))
                    {
                        ModTranslation entry = CreateMapEntryName();
                        entry.SetDefault(Tree.SaplingMapName);
                        AddMapEntry(Tree.SaplingMapColor.Value, entry);
                    }
                    else AddMapEntry(Tree.SaplingMapColor.Value, LocalizationLoader.CreateTranslation(Tree.SaplingMapName));
                }
                else AddMapEntry(Tree.SaplingMapColor.Value);
            }
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            WorldGen.Check1x2(i, j, Type);
            return false;
        }

        public override void RandomUpdate(int i, int j)
        {
            if (Main.rand.NextBool(Tree.GrowChance))
                Tree.Grow(i, j);
        }
    }

    [Autoload(false)]
    public class CustomTreeAcorn : ModItem
    {
        public bool IsDefault => GetType() == typeof(CustomTreeAcorn);

        public override string Name => IsDefault ? Tree.Name + "Acorn" : base.Name;
        public override string Texture => IsDefault ? Tree.AcornTexture : base.Texture;

        public CustomTree Tree { get; internal set; }

        public ushort PlaceTileType => Tree.Sapling.Type;
        public virtual int PlaceTileStyle => 0;

        public override void SetStaticDefaults()
        {
            if (Tree.DefaultAcornName is not null)
                DisplayName.SetDefault(Tree.DefaultAcornName);
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(PlaceTileType, PlaceTileStyle);
        }
    }

    [Autoload(false)]
    public class CustomTreeTile : ModTile
    {
        public bool IsDefault => GetType() == typeof(CustomTreeTile);

        public override string Name => IsDefault ? Tree.Name + "Tile" : base.Name;
        public override string Texture => IsDefault ? Tree.TileTexture : base.Texture;

        public CustomTree Tree { get; internal set; }

        public override void SetStaticDefaults()
        {
            Main.npcCatchable[Type] = true;
            Main.tileAxe[Type] = true;
            Main.tileFrameImportant[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.IsShakeable[Type] = true;
            TileID.Sets.GetsDestroyedForMeteors[Type] = true;
            TileID.Sets.GetsCheckedForLeaves[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;

            if (Tree.MapColor.HasValue)
            {
                if (Tree.MapName is not null)
                {
                    if (Tree.MapName.Any(c => char.IsWhiteSpace(c)))
                    {
                        ModTranslation entry = CreateMapEntryName();
                        entry.SetDefault(Tree.MapName);
                        AddMapEntry(Tree.MapColor.Value, entry);
                    }
                    else AddMapEntry(Tree.MapColor.Value, LocalizationLoader.CreateTranslation(Tree.MapName));
                }
                else AddMapEntry(Tree.MapColor.Value);
            }
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => Tree.TileFrame(i, j);
        public override bool Drop(int i, int j) => Tree.Drop(i, j);
    }

    [Autoload(false)]
    public class CustomTreeLeaf : ModGore
    {
        public bool IsDefault => GetType() == typeof(CustomTreeLeaf);

        public override string Name => IsDefault ? Tree.Name + "Tile" : base.Name;
        public override string Texture => IsDefault ? Tree.LeafTexture : base.Texture;

        public CustomTree Tree { get; internal set; }

        public override void SetStaticDefaults()
        {
            GoreID.Sets.SpecialAI[Type] = 3;
        }

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            base.OnSpawn(gore, source);
        }
    }
}
