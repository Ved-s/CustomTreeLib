using CustomTreeLib.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static List<CustomTree> LoadedTrees = new();

        public static Dictionary<int, CustomTree> ByTileType = new();
        public static Dictionary<int, CustomTree> ByAcornType = new();
        public static Dictionary<int, CustomTree> BySaplingType = new();
        public static Dictionary<int, CustomTree> ByCustomLeafType = new();

        /// <summary>
        /// Internal tree name
        /// </summary>
        public virtual string Name => GetType().Name;

        public abstract string SaplingTexture { get; }
        public abstract string AcornTexture { get; }
        public abstract string TileTexture { get; }
        public abstract string TopTexture { get; }
        public abstract string BranchTexture { get; }

        /// <summary>
        /// If this is provided, new leaf ModGore will be registered and used as LeafType
        /// </summary>
        public virtual string LeafTexture { get; }

        /// <summary>
        /// Tree type for vanilla tree shake system
        /// </summary>
        public virtual TreeTypes TreeType { get; set; } = TreeTypes.Forest;

        /// <summary>
        /// Override this to use other type of leaf (Gore)
        /// </summary>
        public virtual int LeafType => Leaf is null ? GoreID.TreeLeaf_Normal : Leaf.Type;

        /// <summary>
        /// Used for sapling and tree ground tile type checks
        /// </summary>
        public abstract int[] ValidGroundTiles { get; }

        /// <summary>
        /// Used for sapling wall type checks
        /// </summary>
        public virtual int[] ValidWalls { get; } = new int[] { 0 };

        public virtual CustomTreeSapling Sapling { get; } = new();
        public virtual CustomTreeAcorn Acorn { get; } = new();
        public virtual CustomTreeTile Tile { get; } = new();
        public virtual CustomTreeLeaf Leaf { get; protected set; }

        /// <summary>
        /// If something goes wrong, this fallback will be used for all tree foliage
        /// </summary>
        public virtual int FoliageStyleFallback => 0;

        /// <summary>
        /// 1 in X chance of tree growing from sapling per random tick
        /// </summary>
        public virtual int GrowChance { get; set; } = 5;

        /// <summary>
        /// 1 in X chance of not generating roots
        /// </summary>
        public virtual int NoRootChance { get; set; } = 3;

        /// <summary>
        /// 1 in X chance of generating more bark on tile
        /// </summary>
        public virtual int MoreBarkChance { get; set; } = 7;

        /// <summary>
        /// 1 in X chance of generating less bark on tile
        /// </summary>
        public virtual int LessBarkChance { get; set; } = 7;

        /// <summary>
        /// 1 in X chance of generating branch
        /// </summary>
        public virtual int BranchChance { get; set; } = 4;

        /// <summary>
        /// 1 in X chance that generated branch will not have leaves
        /// </summary>
        public virtual int NotLeafyBranchChance { get; set; } = 3;

        /// <summary>
        /// 1 in X chance that generated top will be broken
        /// </summary>
        public virtual int BrokenTopChance { get; set; } = 13;

        /// <summary>
        /// Minimum tree height for growing from sapling (not growing over time)
        /// </summary>
        public virtual int MinHeight { get; set; } = 5;

        /// <summary>
        /// Maximum tree height for growing from sapling (not growing over time)
        /// </summary>
        public virtual int MaxHeight { get; set; } = 12;

        /// <summary>
        /// How many tiles after tree top tile are additionally checked if empty
        /// </summary>
        public virtual int TopPadding { get; set; } = 4;

        /// <summary>
        /// How many styles sapling tile has
        /// </summary>
        public virtual int SaplingStyles { get; set; } = 1;

        /// <summary>
        /// Tree tile map color
        /// </summary>
        public virtual Color? MapColor { get; set; }

        /// <summary>
        /// Tree tile map name
        /// </summary>
        public virtual string MapName { get; set; }

        /// <summary>
        /// Sapling tile map color
        /// </summary>
        public virtual Color? SaplingMapColor { get; set; }

        /// <summary>
        /// Sapling tile map name
        /// </summary>
        public virtual string SaplingMapName { get; set; }

        /// <summary>
        /// Default name for acorn item
        /// </summary>
        public virtual string DefaultAcornName { get; set; } = null;

        public Texture2D TopTextureCache;
        public Texture2D BranchTextureCache;

        public TreePaintingSettings PaintingSettings = new()
        {
            UseSpecialGroups = false,
        };

        public virtual void Load(Mod mod)
        {
            LoadedTrees.Add(this);

            if (LeafTexture is not null)
                Leaf = new();

            if (Sapling is not null)
            {
                if (!Sapling.Loaded) 
                    mod.AddContent(Sapling);
                BySaplingType[Sapling.Type] = this;
            }

            if (Acorn is not null)
            {
                if (!Acorn.Loaded)
                    mod.AddContent(Acorn);
                ByAcornType[Acorn.Type] = this;
            }

            if (Tile is not null)
            {
                if (!Tile.Loaded)
                    mod.AddContent(Tile);
                ByTileType[Tile.Type] = this;
            }

            if (Leaf is not null)
            {
                mod.AddContent(Leaf);
                ByCustomLeafType[Leaf.Type] = this;
            }
        }

        public virtual void Unload()
        {
            LoadedTrees.Remove(this);
            ByTileType.Remove(Tile.Type);
            ByAcornType.Remove(Acorn.Type);
            BySaplingType.Remove(Sapling.Type);

            if (Leaf is not null)
                ByCustomLeafType.Remove(Leaf.Type);
        }

        /// <summary>
        /// Used for smart cursor with acorns
        /// </summary>
        public WorldGen.GrowTreeSettings GetVanillaTreeGrowSettings()
        {
            return new()
            {
                SaplingTileType = Sapling.Type,
                TreeTileType = Tile.Type,
                GroundTest = ValidGroundType,
                TreeHeightMax = MaxHeight,
                TreeHeightMin = MinHeight,
                TreeTopPaddingNeeded = TopPadding,
                WallTest = ValidWallType
            };
        }

        /// <summary>
        /// Gets current tree settings
        /// </summary>
        public TreeSettings GetTreeSettings()
        {
            return new()
            {
                MaxHeight = MaxHeight,
                MinHeight = MinHeight,

                TreeTileType = Tile.Type,

                GroundTypeCheck = ValidGroundType,
                WallTypeCheck = ValidWallType,

                TopPaddingNeeded = TopPadding,

                BranchChance = BranchChance,
                BrokenTopChance = BrokenTopChance,
                LessBarkChance = LessBarkChance,
                MoreBarkChance = MoreBarkChance,
                NotLeafyBranchChance = NotLeafyBranchChance,
                NoRootChance = NoRootChance,
            };
        }

        /// <summary>
        /// Return true if tile type is valid for tree to grow on
        /// </summary>
        public virtual bool ValidGroundType(int tile) => ValidGroundTiles.Contains(tile);

        /// <summary>
        /// Return true if wall type is valid for tree
        /// </summary>
        public virtual bool ValidWallType(int tile) => ValidWalls.Contains(tile);

        /// <summary>
        /// Called in world generaion process or by <b>ctl gen</b> command<br/>
        /// Return true if tree was generated
        /// </summary>
        /// <param name="x">Ground tile X position</param>
        /// <param name="y">Ground tile Y position</param>
        /// <returns></returns>
        public virtual bool TryGenerate(int x, int y)
        {
            return false;
        }

        /// <summary>
        /// Override fur custom growing from sapling
        /// </summary>
        public virtual void Grow(int x, int y)
        {
            if (TreeGrowing.GrowTree(x, y, GetTreeSettings()) && WorldGen.PlayerLOS(x, y))
            {
                WorldGen.TreeGrowFXCheck(x, y);
            }
        }

        /// <summary>
        /// Return true is tree can grow more over time
        /// </summary>
        public virtual bool CanGrowMore(Point topPos, TreeSettings settings, TreeStats stats)
        {
            return stats.LeafyBranches < 3 && stats.TotalBranches < 5 && stats.TotalBlocks < 25;
        }

        /// <summary>
        /// Filter vanilla item drops from shaking this tree
        /// </summary>
        public virtual bool FilterDefaultTreeShakeItemDrop(int item) => item != ItemID.Acorn;

        /// <summary>
        /// This is executed when tree is being shook, return true to continue vanilla code for tree shaking
        /// </summary>
        public virtual bool Shake(int x, int y, ref bool createLeaves) => true;

        /// <summary>
        /// This is executed per tree tile, when they're broken, return true to default to vanilla behavior.
        /// Use TreeTileInfo.GetInfo to determine tile type
        /// </summary>
        public virtual bool Drop(int x, int y) => true;

        /// <summary>
        /// This is executed per tree tile, when their frame needs to be updated
        /// </summary>
        public virtual void TileFrame(int x, int y)
        {
            TreeGrowing.CheckTree(x, y, GetTreeSettings());
        }

        /// <summary>
        /// Randomly executed on every tree tile
        /// </summary>
        public virtual void RandomUpdate(int x, int y)
        {
        }

        /// <summary>
        /// Gets tree leaf gore id and tree height for falling leaves
        /// </summary>
        public virtual void GetTreeLeaf(int x, Tile topTile, Tile t, ref int treeHeight, out int treeFrame, out int passStyle)
        {
            treeFrame = 0;
            passStyle = LeafType;
        }

        /// <summary>
        /// Woks the same as ModTile.CreateDust<br/>
        /// Allows to change dust type created or to disable tile dust
        /// </summary>
        /// <param name="x">Tile X position</param>
        /// <param name="y">Tile Y position</param>
        /// <param name="dustType">Tile dust type</param>
        /// <returns>False to stop dust from creating</returns>
        public virtual bool CreateDust(int x, int y, ref int dustType)
        {
            return true;
        }

        /// <summary>
        /// Gets texture coordinates data for custom top texture
        /// </summary>
        public virtual bool GetTreeFoliageData(int i, int j, int xoffset, ref int treeFrame, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight)
        {
            int v = 0;
            return WorldGen.GetCommonTreeFoliageData(i, j, xoffset, ref treeFrame, ref v, out floorY, out topTextureFrameWidth, out topTextureFrameHeight);
        }

        /// <summary>
        /// Executed before drawing tree foliage<br/>
        /// Return false to prevent foliage drawing
        /// </summary>
        /// <param name="position">Draw position</param>
        /// <param name="size">Draw size</param>
        /// <param name="foliageType">Foliage type</param>
        /// <param name="treeFrame">Tree style frame</param>
        /// <param name="origin">Draw origin</param>
        /// <param name="color">Draw color</param>
        /// <param name="rotation">Draw rotation</param>
        /// <returns></returns>
        public virtual bool PreDrawFoliage(Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            return true;
        }

        /// <summary>
        /// Executed after drawing tree foliage
        /// </summary>
        /// <param name="position">Draw position</param>
        /// <param name="size">Draw size</param>
        /// <param name="foliageType">Foliage type</param>
        /// <param name="treeFrame">Tree style frame</param>
        /// <param name="origin">Draw origin</param>
        /// <param name="color">Draw color</param>
        /// <param name="rotation">Draw rotation</param>
        public virtual void PostDrawFoliage(Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
        }

        /// <summary>
        /// Method for getting top and branch textures
        /// </summary>
        /// <param name="branch">Is branch texture</param>
        /// <returns></returns>
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
    }

    public abstract class CustomTree<TAcornItem> : CustomTree where TAcornItem : CustomTreeAcorn, new()
    {
        public override TAcornItem Acorn { get; } = new();
        public sealed override string AcornTexture => null;
    }
    public abstract class CustomTree<TAcornItem, TSaplingTile> : CustomTree
        where TAcornItem : CustomTreeAcorn, new()
        where TSaplingTile : CustomTreeSapling, new()
    {
        public override TAcornItem Acorn { get; } = new();
        public override TSaplingTile Sapling { get; } = new();

        public override string AcornTexture => null;
        public override string SaplingTexture => null;
    }
    public abstract class CustomTree<TAcornItem, TSaplingTile, TTreeTile> : CustomTree
        where TAcornItem : CustomTreeAcorn, new()
        where TSaplingTile : CustomTreeSapling, new()
        where TTreeTile : CustomTreeTile, new()
    {
        public override TAcornItem Acorn { get; } = new();
        public override TSaplingTile Sapling { get; } = new();
        public override TTreeTile Tile { get; } = new();

        public override string AcornTexture => null;
        public override string SaplingTexture => null;
        public override string TileTexture => null;
    }

    [Autoload(false)]
    public class CustomTreeSapling : ModTile
    {
        public bool IsDefault => GetType() == typeof(CustomTreeSapling);

        public override string Name => IsDefault ? Tree.Name + "Sapling" : base.Name;
        public override string Texture => IsDefault ? Tree.SaplingTexture : base.Texture;

        public CustomTree Tree => CustomTree.BySaplingType.TryGetValue(Type, out var tree) ? tree 
            : CustomTree.LoadedTrees.FirstOrDefault(t => t.Sapling.Type == Type);

        public bool Loaded { get; private set; }

        public override void Load()
        {
            Loaded = true;
        }

        public override void Unload()
        {
            Loaded = false;
        }

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
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.newTile.HookCheckIfCanPlace = new((x,y,type,style,dir,alt) => HookCanPlace(x,y,style,dir,alt) ? 1 : 0, 0, 0, true);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="style"></param>
        /// <param name="dir"></param>
        /// <param name="alternate"></param>
        /// <returns></returns>
        public virtual bool HookCanPlace(int x, int y, int style, int dir, int alternate)
        {
            return Tree.ValidWallType(Framing.GetTileSafely(x, y).WallType) && Tree.ValidGroundType(Framing.GetTileSafely(x, y + 1).TileType);
        }
    }

    [Autoload(false)]
    public class CustomTreeAcorn : ModItem
    {
        public bool IsDefault => GetType() == typeof(CustomTreeAcorn);

        public override string Name => IsDefault && Tree is not null ? Tree.Name + "Acorn" : base.Name;
        public override string Texture => IsDefault ? Tree.AcornTexture : base.Texture;

        public CustomTree Tree => CustomTree.ByAcornType.TryGetValue(Type, out var tree) ? tree
            : CustomTree.LoadedTrees.FirstOrDefault(t => t.Acorn.Type == Type);

        public ushort PlaceTileType => Tree.Sapling.Type;
        public virtual int PlaceTileStyle => 0;

        public bool Loaded { get; private set; }

        public override void Load()
        {
            Loaded = true;
        }

        public override void Unload()
        {
            Loaded = false;
        }

        public override void SetStaticDefaults()
        {
            if (Tree.DefaultAcornName is not null)
                DisplayName.SetDefault(Tree.DefaultAcornName);
        }

        public override void SetDefaults()
        {
            if (Tree is null) return;
            Item.DefaultToPlaceableTile(PlaceTileType, PlaceTileStyle);
        }
    }

    [Autoload(false)]
    public class CustomTreeTile : ModTile
    {
        public bool IsDefault => GetType() == typeof(CustomTreeTile);

        public override string Name => IsDefault ? Tree.Name + "Tile" : base.Name;
        public override string Texture => IsDefault ? Tree.TileTexture : base.Texture;

        public CustomTree Tree => CustomTree.ByTileType.TryGetValue(Type, out var tree) ? tree
            : CustomTree.LoadedTrees.FirstOrDefault(t => t.Tile.Type == Type);

        public bool Loaded { get; private set; }

        public override void Load()
        {
            Loaded = true;
        }

        public override void Unload()
        {
            Loaded = false;
        }

        public override void SetStaticDefaults()
        {
            Main.tileAxe[Type] = true;
            Main.tileFrameImportant[Type] = true;

            TileID.Sets.IsATreeTrunk[Type] = true;
            TileID.Sets.IsShakeable[Type] = true;
            TileID.Sets.GetsDestroyedForMeteors[Type] = true;
            TileID.Sets.GetsCheckedForLeaves[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileReplaceIfOnTopOfIt[Type] = true;

            //TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            //TileObjectData.newTile.CoordinateWidth = 20;
            //TileObjectData.newTile.CoordinateHeights = new[] { 20 };
            //TileObjectData.addTile(Type);

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

        public override void RandomUpdate(int i, int j)
        {
            Tree.RandomUpdate(i, j);
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tree.TileFrame(i, j);
            return false;
        }

        public override bool Drop(int i, int j) => Tree.Drop(i, j);

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            width = 20;
            height = 20;
            tileFrameX = (short)(tileFrameX / 18 * 22);
            tileFrameY = (short)(tileFrameY / 18 * 22);
        }

        public override bool CreateDust(int i, int j, ref int type) => Tree?.CreateDust(i, j, ref type) ?? true;
    }

    [Autoload(false)]
    public class CustomTreeLeaf : ModGore
    {
        public bool IsDefault => GetType() == typeof(CustomTreeLeaf);

        public override string Name => IsDefault ? Tree.Name + "Tile" : base.Name;
        public override string Texture => IsDefault ? Tree.LeafTexture : base.Texture;

        public CustomTree Tree => CustomTree.ByCustomLeafType.TryGetValue(Type, out var tree) ? tree
            : CustomTree.LoadedTrees.FirstOrDefault(t => t.Leaf?.Type == Type);

        public bool Loaded { get; private set; }

        public override void Load()
        {
            Loaded = true;
        }

        public override void Unload()
        {
            Loaded = false;
        }

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
