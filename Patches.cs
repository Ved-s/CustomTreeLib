using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;
using MonoMod.RuntimeDetour.HookGen;
using System;
using Terraria.ModLoader.IO;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria.GameContent;

namespace CustomTreeLib
{
    internal class Patches : ILoadable
    {
        public Mod Mod { get; private set; }
        public static Patches Instance => ModContent.GetInstance<Patches>();

        public delegate void TileDrawing_AddSpecialPointDelegate(TileDrawing self, int x, int y, int type);
        public TileDrawing_AddSpecialPointDelegate TileDrawing_AddSpecialPoint;

        public void Load(Mod mod)
        {
            Mod = mod;

            On.Terraria.WorldGen.IsTileTypeFitForTree += WorldGen_IsTileTypeFitForTree;
            On.Terraria.GameContent.Drawing.TileDrawing.CacheSpecialDraws += TileDrawing_CacheSpecialDraws;
            On.Terraria.WorldGen.GrowTreeSettings.Profiles.TryGetFromItemId += Profiles_TryGetFromItemId;
            On.Terraria.WorldGen.GetTreeLeaf += WorldGen_GetTreeLeaf;
            On.Terraria.WorldGen.GetTreeType += WorldGen_GetTreeType;
            On.Terraria.Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool += Item_NewItem;
            On.Terraria.WorldGen.GetTreeBottom += WorldGen_GetTreeBottom;
            On.Terraria.WorldGen.IsTileALeafyTreeTop += WorldGen_IsTileALeafyTreeTop;

            IL.Terraria.WorldGen.ShakeTree += WorldGen_ShakeTree;
            IL.Terraria.GameContent.Drawing.TileDrawing.DrawTrees += TileDrawing_DrawTrees;

            TileDrawing_AddSpecialPoint = (TileDrawing_AddSpecialPointDelegate)typeof(TileDrawing)
                .GetMethod("AddSpecialPoint", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(TileDrawing_AddSpecialPointDelegate));
        }

        public void Unload()
        {
            On.Terraria.WorldGen.IsTileTypeFitForTree -= WorldGen_IsTileTypeFitForTree;
            On.Terraria.GameContent.Drawing.TileDrawing.CacheSpecialDraws -= TileDrawing_CacheSpecialDraws;
            On.Terraria.WorldGen.GrowTreeSettings.Profiles.TryGetFromItemId -= Profiles_TryGetFromItemId;
            On.Terraria.WorldGen.GetTreeType -= WorldGen_GetTreeType;
            On.Terraria.Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool -= Item_NewItem;
            On.Terraria.WorldGen.GetTreeBottom -= WorldGen_GetTreeBottom;
            On.Terraria.WorldGen.IsTileALeafyTreeTop -= WorldGen_IsTileALeafyTreeTop;

            IL.Terraria.WorldGen.ShakeTree -= WorldGen_ShakeTree;
            IL.Terraria.GameContent.Drawing.TileDrawing.DrawTrees -= TileDrawing_DrawTrees;
        }

        private void TileDrawing_CacheSpecialDraws(On.Terraria.GameContent.Drawing.TileDrawing.orig_CacheSpecialDraws orig, Terraria.GameContent.Drawing.TileDrawing self, int tileX, int tileY, Terraria.DataStructures.TileDrawInfo drawData)
        {
            orig(self, tileX, tileY, drawData);

            if (CustomTree.ByTileType.ContainsKey(drawData.typeCache) && drawData.tileFrameY >= 198 && drawData.tileFrameX >= 22)
            {
                TileDrawing_AddSpecialPoint(self, tileX, tileY, 0);
            }
        }
        private bool WorldGen_IsTileTypeFitForTree(On.Terraria.WorldGen.orig_IsTileTypeFitForTree orig, ushort type)
        {
            foreach (CustomTree tree in CustomTree.LoadedTrees)
            {
                if (type == tree.Tile.Type) return true;
            }
            return orig(type);
        }
        private bool Profiles_TryGetFromItemId(On.Terraria.WorldGen.GrowTreeSettings.Profiles.orig_TryGetFromItemId orig, int itemType, out WorldGen.GrowTreeSettings profile)
        {
            if (CustomTree.ByAcornType.TryGetValue(itemType, out CustomTree tree))
            {
                profile = tree.GetVanillaTreeGrowSettings();
                return true;
            }
            return orig(itemType, out profile);
        }
        private void WorldGen_GetTreeLeaf(On.Terraria.WorldGen.orig_GetTreeLeaf orig, int x, Tile topTile, Tile t, ref int treeHeight, out int treeFrame, out int passStyle)
        {
            if (CustomTree.ByTileType.TryGetValue(topTile.TileType, out CustomTree tree))
            {
                tree.GetTreeLeaf(x, topTile, t, ref treeHeight, out treeFrame, out passStyle);
                return;
            }
            orig(x, topTile, t, ref treeHeight, out treeFrame, out passStyle);
        }
        private TreeTypes WorldGen_GetTreeType(On.Terraria.WorldGen.orig_GetTreeType orig, int tileType)
        {
            if (CustomTree.ByTileType.TryGetValue(tileType, out CustomTree tree))
                return tree.TreeType;
            return orig(tileType);
        }
        private int Item_NewItem(On.Terraria.Item.orig_NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool orig, IEntitySource source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup)
        {
            if (source is EntitySource_ShakeTree shakeTree)
            {
                Tile t = Framing.GetTileSafely(shakeTree.TileCoords);
                if (CustomTree.ByTileType.TryGetValue(t.TileType, out CustomTree tree) && !tree.FilterDefaultTreeShakeItemDrop(Type))
                {
                    return 400;
                }
            }
            return orig(source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
        }
        private void WorldGen_GetTreeBottom(On.Terraria.WorldGen.orig_GetTreeBottom orig, int i, int j, out int x, out int y)
        {
            if (CustomTree.ByTileType.ContainsKey(Framing.GetTileSafely(i, j).TileType))
            {
                x = i;
                y = j;
                TreeGrowing.GetTreeBottom(ref x, ref y);
                y--;
                return;
            }

            orig(i, j, out x, out y);
        }

        private bool WorldGen_IsTileALeafyTreeTop(On.Terraria.WorldGen.orig_IsTileALeafyTreeTop orig, int i, int j)
        {
            if (CustomTree.ByTileType.ContainsKey(Framing.GetTileSafely(i, j).TileType))
                return TreeTileInfo.GetInfo(i, j).Type == TreeTileType.LeafyTop;

            return orig(i, j);
        }

        private void WorldGen_ShakeTree(ILContext il)
        {
            ILCursor c = new(il);

            /*
              IL_00D1: ldloc.0
	          IL_00D2: ldloc.1
	          IL_00D3: call      bool Terraria.WorldGen::IsTileALeafyTreeTop(int32, int32)
             */

            int x = -1, y = -1;

            if (!c.TryGotoNext(
                i => i.MatchLdloc(out x),
                i => i.MatchLdloc(out y),
                i => i.MatchCall("Terraria.WorldGen", "IsTileALeafyTreeTop")
                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (ShakeTree:GetXY)", il.Method.FullName);
                return;
            }

            /*
              IL_014C: call      uint8 Terraria.Player::FindClosest(valuetype [FNA]Microsoft.Xna.Framework.Vector2, int32, int32)
	          IL_0151: ldc.r4    0.0
	          IL_0156: ldc.r4    0.0
	          IL_015B: call      int32 Terraria.Projectile::NewProjectile(class Terraria.DataStructures.IEntitySource, float32, float32, float32, float32, int32, int32, float32, int32, float32, float32)
	          IL_0160: pop

	          IL_0161: br        IL_0DAF
             */

            ILLabel ifEnd = null;

            if (!c.TryGotoNext(
                i => i.MatchCall<Player>("FindClosest"),
                i => i.MatchLdcR4(0),
                i => i.MatchLdcR4(0),
                i => i.MatchCall<Projectile>("NewProjectile"),
                i => i.MatchPop(),
                i => i.MatchBr(out ifEnd)
                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (ShakeTree:FindIfChain)", il.Method.FullName);
                return;
            }

            int createLeaves = -1;
            /*
              IL_00EF: stloc.s   flag
              
              IL_00F1: ldsfld    bool Terraria.Main::getGoodWorld
              IL_00F6: brfalse.s IL_0166
             */

            if (!c.TryGotoPrev(
                i => i.MatchStloc(out createLeaves),
                i => i.MatchLdsfld("Terraria.Main", "getGoodWorld"),
                i => i.MatchBrfalse(out _)
                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (ShakeTree:FindPatchPlace)", il.Method.FullName);
                return;
            }
            c.Index++;

            c.Emit(OpCodes.Ldloc, x);
            c.Emit(OpCodes.Ldloc, y);
            c.Emit(OpCodes.Ldloca, createLeaves);
            c.Emit<Patches>(OpCodes.Call, nameof(ShakeTreeHook));
            c.Emit(OpCodes.Brfalse, ifEnd);
        }
        private void TileDrawing_DrawTrees(ILContext il)
        {
            PatchTreeFrame(il);
            PatchFoliageRendering(il);

            ILCursor c = new(il);
            /*
              IL_00A5: stloc.s   getTreeFoliageDataMethod

              IL_00A7: ldc.i4.0
			  IL_00A8: stloc.s   flag2
              
			  IL_00AA: ldloc.s   'type'
			  IL_00AC: ldc.i4    589
             */

            int getTreeFoliageDataMethod = -1,
                success = -1,
                type = -1;

            if (!c.TryGotoNext(
                x => x.MatchStloc(out getTreeFoliageDataMethod),

                x => x.MatchLdcI4(0),
                x => x.MatchStloc(out success),

                x => x.MatchLdloc(out type),
                x => x.MatchLdcI4(589)
                ))
            {
                Mod.Logger.WarnFormat("Patch error: {0} (DrawTrees:FoliagePatch)");
                return;
            }

            c.Index += 3;

            c.Emit(OpCodes.Ldloc, type);
            c.Emit(OpCodes.Ldloca, success);
            c.Emit<Patches>(OpCodes.Call, nameof(GetFoliageHook));
            c.Emit(OpCodes.Stloc, getTreeFoliageDataMethod);

            int topPatches = 0, branchPatches = 0;

            /*
              IL_051D: call      instance class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D Terraria.GameContent.Drawing.TileDrawing::GetTreeBranchTexture(int32, int32, uint8)
			  IL_0522: stloc.s   treeBranchTexture
             */

            c.Index = 0;

            int color = -1, texture = -1;

            while (c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(out color),
                x => x.MatchCall<TileDrawing>("GetTreeBranchTexture"),
                x => x.MatchStloc(out texture)
                ))
            {
                c.Emit(OpCodes.Ldloc, type);
                c.Emit(OpCodes.Ldc_I4_1);
                c.Emit(OpCodes.Ldloca, texture);
                c.Emit<Patches>(OpCodes.Call, nameof(GetTexturesHook));

                branchPatches++;

                //if (c.TryGotoNext(x => x.MatchCallvirt<SpriteBatch>("Draw")))
                //{
                //    c.Emit(OpCodes.Ldloc, type);
                //    c.Emit(OpCodes.Ldc_I4_1);
                //    c.Emit(OpCodes.Ldloc, color);
                //    c.Emit<Patches>(OpCodes.Call, nameof(BeforeDrawTexture));
                //    c.Index++;
                //    c.Emit(OpCodes.Ldloc, type);
                //    c.Emit(OpCodes.Ldc_I4_1);
                //    c.Emit(OpCodes.Ldloc, color);
                //    c.Emit<Patches>(OpCodes.Call, nameof(AfterDrawTexture));
                //}
                //else
                //{
                //    Mod.Logger.WarnFormat("Patch error: expected SpriteBatch.Draw after GetTreeBranchTexture in DrawTrees:HookTopDraws");
                //}
            }

            c.Index = 0;

            while (c.TryGotoNext(
                MoveType.After,
                x => x.MatchLdloc(out color),
                x => x.MatchCall<TileDrawing>("GetTreeTopTexture"),
                x => x.MatchStloc(out texture)
                ))
            {
                c.Emit(OpCodes.Ldloc, type);
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Ldloca, texture);
                c.Emit<Patches>(OpCodes.Call, nameof(GetTexturesHook));

                topPatches++;

                //if (c.TryGotoNext(x => x.MatchCallvirt<SpriteBatch>("Draw")))
                //{
                //    c.Emit(OpCodes.Ldloc, type);
                //    c.Emit(OpCodes.Ldc_I4_0);
                //    c.Emit(OpCodes.Ldloc, color);
                //    c.Emit<Patches>(OpCodes.Call, nameof(BeforeDrawTexture));
                //    c.Index++;
                //    c.Emit(OpCodes.Ldloc, type);
                //    c.Emit(OpCodes.Ldc_I4_0);
                //    c.Emit(OpCodes.Ldloc, color);
                //    c.Emit<Patches>(OpCodes.Call, nameof(AfterDrawTexture));
                //}
                //else
                //{
                //    Mod.Logger.WarnFormat("Patch error: expected SpriteBatch.Draw after GetTreeTopTexture in DrawTrees:HookTopDraws");
                //}
            }

            if (branchPatches < 2)
            {
                if (branchPatches == 0) Mod.Logger.Warn("Patch error: DrawTrees:GetTreeBranchTexturePatch");
                else Mod.Logger.WarnFormat("Patch warning: expected 2 patches in DrawTrees:GetTreeBranchTexturePatch, got {1}", topPatches);
            }
            if (topPatches < 2)
            {
                if (topPatches == 0) Mod.Logger.Warn("Patch error: DrawTrees:GetTreeTopTexturePatch");
                else Mod.Logger.WarnFormat("Patch warning: expected 2 patches in DrawTrees:GetTreeTopTexturePatch, got {1}", topPatches);
            }
        }

        private void PatchTreeFrame(ILContext il) 
        {
            ILCursor c = new(il);
            /*
		      IL_0083: ldloca.s  tile
		      IL_0085: call      instance int16& Terraria.Tile::get_frameX()
		      IL_008A: ldind.i2
		      IL_008B: stloc.s   frameX
		      
		      IL_008D: ldloca.s  tile
		      IL_008F: call      instance int16& Terraria.Tile::get_frameY()
		      IL_0094: ldind.i2
		      IL_0095: stloc.s   frameY
             */

            int tile = -1,
                frameX = -1,
                frameY = -1;

            if (!c.TryGotoNext(
                MoveType.After,

                x=>x.MatchLdloca(out tile),
                x=>x.MatchCall<Tile>("get_frameX"),
                x=>x.MatchLdindI2(),
                x=>x.MatchStloc(out frameX),

                x => x.MatchLdloca(tile),
                x => x.MatchCall<Tile>("get_frameY"),
                x => x.MatchLdindI2(),
                x => x.MatchStloc(out frameY)
                ))
            {
                Mod.Logger.Warn("Patch error: DrawTrees:PatchTreeFrame");
                return;
            }

            c.Emit(OpCodes.Ldloc, tile);
            c.Emit(OpCodes.Ldloca, frameX);
            c.Emit(OpCodes.Ldloca, frameY);
            c.Emit<Patches>(OpCodes.Call, nameof(TreeFrameHook));
        }
        private void PatchFoliageRendering(ILContext il)
        {
            ILCursor c = new(il);

            /*
              IL_0079: ldloca.s  tile
		      IL_007B: call      instance uint16& Terraria.Tile::get_type()
             */

            int tile = -1;

            if (!c.TryGotoNext(
                x => x.MatchLdloca(out tile),
                x => x.MatchCall<Tile>("get_type")
                ))
            {
                Mod.Logger.Warn("Patch error: DrawTrees:GetTreeTypeForFoliageRender");
                return;
            }

            /*
			  IL_05C9: ldloc.s   treeBranchTexture
			  IL_05CB: ldloc.s   position

			  IL_05CD: ldc.i4.s  42 // frameOffsetX
			  IL_05CF: ldloc.s   treeFrame
			  IL_05D1: ldc.i4.s  42
			  IL_05D3: mul
			  IL_05D4: ldc.i4.s  40
			  IL_05D6: ldc.i4.s  40
			  IL_05D8: newobj    instance void [FNA]Microsoft.Xna.Framework.Rectangle::.ctor(int32, int32, int32, int32)

			  IL_05DD: newobj    instance void valuetype [System.Runtime]System.Nullable`1<valuetype [FNA]Microsoft.Xna.Framework.Rectangle>::.ctor(!0)

			  IL_05E2: ldloc.s   color

			  IL_05E4: ldloc.s   num8
			  IL_05E6: ldloc.s   num4
			  IL_05E8: mul

			  IL_05E9: ldc.r4    0.0
			  IL_05EE: ldc.r4    30
			  IL_05F3: newobj    instance void [FNA]Microsoft.Xna.Framework.Vector2::.ctor(float32, float32)

			  IL_05F8: ldc.r4    1
			  IL_05FD: ldc.i4.0
			  IL_05FE: ldc.r4    0.0
			  IL_0603: callvirt  instance void [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D, valuetype [FNA]Microsoft.Xna.Framework.Vector2, valuetype [System.Runtime]System.Nullable`1<valuetype [FNA]Microsoft.Xna.Framework.Rectangle>, valuetype [FNA]Microsoft.Xna.Framework.Color, float32, valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32, valuetype [FNA]Microsoft.Xna.Framework.Graphics.SpriteEffects, float32)
             */

            int branchPatches = 0;

            int position = -1,
                sizeX = -1,
                sizeY = -1,
                frameOffsetX = -1,
                treeFrame = -1,
                color = -1,
                rotationA = -1,
                rotationB = -1;

            float originX = -1,
                  originY = -1;

            while (c.TryGotoNext(
                MoveType.Before,
                x => x.MatchLdloc(out _),
                x => x.MatchLdloc(out position),

                x => x.MatchLdcI4(out frameOffsetX),
                x => x.MatchLdloc(out treeFrame),
                x => x.MatchLdcI4(out _),
                x => x.MatchMul(),
                x => x.MatchLdcI4(out sizeX),
                x => x.MatchLdcI4(out sizeY),
                x => x.MatchNewobj(out _),

                x => x.MatchNewobj(out _),

                x => x.MatchLdloc(out color),

                x => x.MatchLdloc(out rotationA),
                x => x.MatchLdloc(out rotationB),
                x => x.MatchMul(),

                x => x.MatchLdcR4(out originX),
                x => x.MatchLdcR4(out originY),
                x => x.MatchNewobj(out _),

                x => x.MatchLdcR4(out _),
                x => x.MatchLdcI4(out _),
                x => x.MatchLdcR4(out _),
                x => x.MatchCallvirt<SpriteBatch>("Draw")
                ))
            {
                ILLabel skipBranchDraw = c.DefineLabel();

                c.Emit(OpCodes.Pop); // remove spriteBatch, there is an if jumping to prev instruction

                c.Emit(OpCodes.Ldloc, tile);
                c.Emit(OpCodes.Ldloc, position);

                c.Emit(OpCodes.Ldc_I4, sizeX);
                c.Emit(OpCodes.Ldc_I4, sizeY);
                c.Emit(OpCodes.Newobj, typeof(Point).GetConstructor(new[] { typeof(int), typeof(int) }));

                c.Emit(OpCodes.Ldc_I4, frameOffsetX < 42 ? 1 : 2);
                c.Emit(OpCodes.Ldloc, treeFrame);

                c.Emit(OpCodes.Ldc_R4, originX);
                c.Emit(OpCodes.Ldc_R4, originY);
                c.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) }));

                c.Emit(OpCodes.Ldloc, color);

                c.Emit(OpCodes.Ldloc, rotationA);
                c.Emit(OpCodes.Ldloc, rotationB);
                c.Emit(OpCodes.Mul);

                c.Emit<Patches>(OpCodes.Call, nameof(BeforeDrawTreeFoliage));
                c.Emit(OpCodes.Brfalse, skipBranchDraw);

                c.Emit<Main>(OpCodes.Ldsfld, "spriteBatch"); // add it back

                c.Index += 21;

                c.Emit(OpCodes.Ldloc, tile);
                c.Emit(OpCodes.Ldloc, position);

                c.Emit(OpCodes.Ldc_I4, sizeX);
                c.Emit(OpCodes.Ldc_I4, sizeY);
                c.Emit(OpCodes.Newobj, typeof(Point).GetConstructor(new[] { typeof(int), typeof(int) }));

                c.Emit(OpCodes.Ldc_I4, frameOffsetX < 42 ? 1 : 2);
                c.Emit(OpCodes.Ldloc, treeFrame);

                c.Emit(OpCodes.Ldc_R4, originX);
                c.Emit(OpCodes.Ldc_R4, originY);
                c.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) }));

                c.Emit(OpCodes.Ldloc, color);

                c.Emit(OpCodes.Ldloc, rotationA);
                c.Emit(OpCodes.Ldloc, rotationB);
                c.Emit(OpCodes.Mul);
                c.Emit<Patches>(OpCodes.Call, nameof(AfterDrawTreeFoliage));

                c.MarkLabel(skipBranchDraw);

                branchPatches++;
            }

            if (branchPatches < 2)
            {
                if (branchPatches == 0) Mod.Logger.Warn("Patch error: DrawTrees:HookBranchDrawing");
                else Mod.Logger.WarnFormat("Patch warning: expected 2 patches in DrawTrees:HookBranchDrawing, got {1}", branchPatches);
            }

            /*
              IL_02A1: ldloc.s   treeTopTexture
			  IL_02A3: ldloc.s   position2

			  IL_02A5: ldloc.s   treeFrame
			  IL_02A7: ldloc.s   topTextureFrameWidth2
			  IL_02A9: ldc.i4.2
			  IL_02AA: add
			  IL_02AB: mul
			  IL_02AC: ldc.i4.0
			  IL_02AD: ldloc.s   topTextureFrameWidth2
			  IL_02AF: ldloc.s   topTextureFrameHeight2
			  IL_02B1: newobj    instance void [FNA]Microsoft.Xna.Framework.Rectangle::.ctor(int32, int32, int32, int32)
			  IL_02B6: newobj    instance void valuetype [System.Runtime]System.Nullable`1<valuetype [FNA]Microsoft.Xna.Framework.Rectangle>::.ctor(!0)

			  IL_02BB: ldloc.s   color2

			  IL_02BD: ldloc.s   num11
			  IL_02BF: ldloc.s   num3
			  IL_02C1: mul

			  IL_02C2: ldloc.s   topTextureFrameWidth2
			  IL_02C4: ldc.i4.2
			  IL_02C5: div
			  IL_02C6: conv.r4
			  IL_02C7: ldloc.s   topTextureFrameHeight2
			  IL_02C9: conv.r4
			  IL_02CA: newobj    instance void [FNA]Microsoft.Xna.Framework.Vector2::.ctor(float32, float32)

			  IL_02CF: ldc.r4    1
			  IL_02D4: ldc.i4.0
			  IL_02D5: ldc.r4    0.0
			  IL_02DA: callvirt  instance void [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch::Draw(class [FNA]Microsoft.Xna.Framework.Graphics.Texture2D, valuetype [FNA]Microsoft.Xna.Framework.Vector2, valuetype [System.Runtime]System.Nullable`1<valuetype [FNA]Microsoft.Xna.Framework.Rectangle>, valuetype [FNA]Microsoft.Xna.Framework.Color, float32, valuetype [FNA]Microsoft.Xna.Framework.Vector2, float32, valuetype [FNA]Microsoft.Xna.Framework.Graphics.SpriteEffects, float32)
             */

            c.Index = 0;

            int topTextureFrameWidth = -1,
                topTextureFrameHeight = -1;

            if (!c.TryGotoNext(
                MoveType.Before,
                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out position),

                x=>x.MatchLdloc(out treeFrame),
                x=>x.MatchLdloc(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchAdd(),
                x=>x.MatchMul(),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchLdloc(out _),
                x=>x.MatchNewobj(out _),
                x=>x.MatchNewobj(out _),

                x=>x.MatchLdloc(out color),

                x=>x.MatchLdloc(out rotationA),
                x=>x.MatchLdloc(out rotationB),
                x=>x.MatchMul(),

                x=>x.MatchLdloc(out topTextureFrameWidth),
                x=>x.MatchLdcI4(2),
                x=>x.MatchDiv(),
                x=>x.MatchConvR4(),
                x=>x.MatchLdloc(out topTextureFrameHeight),
                x=>x.MatchConvR4(),
                x=>x.MatchNewobj(out _),

                x=>x.MatchLdcR4(out _),
                x=>x.MatchLdcI4(out _),
                x=>x.MatchLdcR4(out _),
                x=>x.MatchCallvirt<SpriteBatch>("Draw")
                ))
            {
                Mod.Logger.Warn("Patch error: DrawTrees:HookTopDrawing");
                return;
            }
            ILLabel skipDraw = c.DefineLabel();

            c.Emit(OpCodes.Pop);

            c.Emit(OpCodes.Ldloc, tile);
            c.Emit(OpCodes.Ldloc, position);

            c.Emit(OpCodes.Ldloc, topTextureFrameWidth);
            c.Emit(OpCodes.Ldloc, topTextureFrameHeight);
            c.Emit(OpCodes.Newobj, typeof(Point).GetConstructor(new[] { typeof(int), typeof(int) }));

            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Ldloc, treeFrame);

            c.Emit(OpCodes.Ldloc, topTextureFrameWidth);
            c.Emit(OpCodes.Ldc_I4_2);
            c.Emit(OpCodes.Div);
            c.Emit(OpCodes.Conv_R4);
            c.Emit(OpCodes.Ldloc, topTextureFrameHeight);
            c.Emit(OpCodes.Conv_R4);
            c.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) }));

            c.Emit(OpCodes.Ldloc, color);

            c.Emit(OpCodes.Ldloc, rotationA);
            c.Emit(OpCodes.Ldloc, rotationB);
            c.Emit(OpCodes.Mul);

            c.Emit<Patches>(OpCodes.Call, nameof(BeforeDrawTreeFoliage));
            c.Emit(OpCodes.Brfalse, skipDraw);

            c.Emit<Main>(OpCodes.Ldsfld, "spriteBatch");

            c.Index += 27;

            c.Emit(OpCodes.Ldloc, tile);
            c.Emit(OpCodes.Ldloc, position);

            c.Emit(OpCodes.Ldloc, topTextureFrameWidth);
            c.Emit(OpCodes.Ldloc, topTextureFrameHeight);
            c.Emit(OpCodes.Newobj, typeof(Point).GetConstructor(new[] { typeof(int), typeof(int) }));

            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Ldloc, treeFrame);

            c.Emit(OpCodes.Ldloc, topTextureFrameWidth);
            c.Emit(OpCodes.Ldc_I4_2);
            c.Emit(OpCodes.Div);
            c.Emit(OpCodes.Conv_R4);
            c.Emit(OpCodes.Ldloc, topTextureFrameHeight);
            c.Emit(OpCodes.Conv_R4);
            c.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor(new[] { typeof(float), typeof(float) }));

            c.Emit(OpCodes.Ldloc, color);

            c.Emit(OpCodes.Ldloc, rotationA);
            c.Emit(OpCodes.Ldloc, rotationB);
            c.Emit(OpCodes.Mul);

            c.Emit<Patches>(OpCodes.Call, nameof(AfterDrawTreeFoliage));

            c.MarkLabel(skipDraw);
        }

        private static void TreeFrameHook(Tile tile, ref short frameX, ref short frameY)
        {
            if (!CustomTree.ByTileType.ContainsKey(tile.TileType)) return;

            frameX = (short)(frameX / 18 * 22);
            frameY = (short)(frameY / 18 * 22);
        }
        private static bool ShakeTreeHook(int x, int y, ref bool createLeaves)
        {
            return TreeLoader.Shake(x, y, ref createLeaves);
        }
        private static WorldGen.GetTreeFoliageDataMethod GetFoliageHook(ushort type, ref bool success)
        {
            if (CustomTree.ByTileType.TryGetValue(type, out CustomTree tree))
            {
                success = true;
                return (int i, int j, int xoffset, ref int treeFrame, ref int treeStyle, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight) =>
                {
                    treeStyle = tree.FoliageStyleFallback;
                    return tree.GetTreeFoliageData(i, j, xoffset, ref treeFrame, out floorY, out topTextureFrameWidth, out topTextureFrameHeight);
                };
            }
            return null;
        }
        private static void GetTexturesHook(ushort type, bool branch, ref Texture2D texture)
        {
            if (CustomTree.ByTileType.TryGetValue(type, out CustomTree tree))
            {
                texture = tree.GetFoliageTexture(branch);
            }
        }

        private static bool BeforeDrawTreeFoliage(Tile tile, Vector2 position, Point size, int type, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            if (!TreeLoader.PreDrawFoliage(tile.TileType, position, size, (TreeFoliageType)type, treeFrame, origin, color, rotation))
                return false;

            if (!CustomTree.ByTileType.TryGetValue(tile.TileType, out CustomTree tree) || tree?.PaintingSettings is null)
                return true;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            tree.PaintingSettings.ApplyShader(tile.TileColor, Main.tileShader);
            return true;
        }

        private static void AfterDrawTreeFoliage(Tile tile, Vector2 position, Point size, int type, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            TreeLoader.PostDrawFoliage(tile.TileType, position, size, (TreeFoliageType)type, treeFrame, origin, color, rotation);

            if (!CustomTree.ByTileType.ContainsKey(tile.TileType)) return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
    }
}
