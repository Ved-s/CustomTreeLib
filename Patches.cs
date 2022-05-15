using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace CustomTreeLib
{
    public class Patches : ILoadable
    {
        public Mod Mod { get; private set; }

        delegate void TileDrawing_AddSpecialPointDelegate(TileDrawing self, int x, int y, int type);
        TileDrawing_AddSpecialPointDelegate TileDrawing_AddSpecialPoint;

        public void Load(Mod mod)
        {
            Mod = mod;

            On.Terraria.WorldGen.IsTileTypeFitForTree += WorldGen_IsTileTypeFitForTree;
            On.Terraria.GameContent.Drawing.TileDrawing.CacheSpecialDraws += TileDrawing_CacheSpecialDraws;
            On.Terraria.GameContent.Drawing.TileDrawing.GetTileDrawData += TileDrawing_GetTileDrawData;
            On.Terraria.WorldGen.GrowTreeSettings.Profiles.TryGetFromItemId += Profiles_TryGetFromItemId;
            On.Terraria.WorldGen.GetTreeLeaf += WorldGen_GetTreeLeaf;

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
            On.Terraria.GameContent.Drawing.TileDrawing.GetTileDrawData -= TileDrawing_GetTileDrawData;
            On.Terraria.WorldGen.GrowTreeSettings.Profiles.TryGetFromItemId -= Profiles_TryGetFromItemId;

            IL.Terraria.WorldGen.ShakeTree -= WorldGen_ShakeTree;
            IL.Terraria.GameContent.Drawing.TileDrawing.DrawTrees -= TileDrawing_DrawTrees;
        }

        private void TileDrawing_GetTileDrawData(On.Terraria.GameContent.Drawing.TileDrawing.orig_GetTileDrawData orig, TileDrawing self, int x, int y, Terraria.Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, out int tileWidth, out int tileHeight, out int tileTop, out int halfBrickHeight, out int addFrX, out int addFrY, out Microsoft.Xna.Framework.Graphics.SpriteEffects tileSpriteEffect, out Microsoft.Xna.Framework.Graphics.Texture2D glowTexture, out Microsoft.Xna.Framework.Rectangle glowSourceRect, out Microsoft.Xna.Framework.Color glowColor)
        {
            orig(self, x, y, tileCache, typeCache, ref tileFrameX, ref tileFrameY, out tileWidth, out tileHeight, out tileTop, out halfBrickHeight, out addFrX, out addFrY, out tileSpriteEffect, out glowTexture, out glowSourceRect, out glowColor);

            if (CustomTree.ByTileType.ContainsKey(typeCache))
            {
                tileWidth = 20;
                tileHeight = 20;
            }
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
                profile = tree.GetTreeGrowSettings();
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
                x=>x.MatchStloc(out getTreeFoliageDataMethod),

                x=>x.MatchLdcI4(0),
                x=>x.MatchStloc(out success),

                x=>x.MatchLdloc(out type),
                x=>x.MatchLdcI4(589)
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

            int texture = -1;

            while (c.TryGotoNext(
                MoveType.After,
                x=>x.MatchCall<TileDrawing>("GetTreeBranchTexture"),
                x=>x.MatchStloc(out texture)
                ))
            {
                c.Emit(OpCodes.Ldloc, type);
                c.Emit(OpCodes.Ldc_I4_1);
                c.Emit(OpCodes.Ldloca, texture);
                c.Emit<Patches>(OpCodes.Call, nameof(GetTexturesHook));

                branchPatches++;
            }

            c.Index = 0;

            while (c.TryGotoNext(
                MoveType.After,
                x => x.MatchCall<TileDrawing>("GetTreeTopTexture"),
                x => x.MatchStloc(out texture)
                ))
            {
                c.Emit(OpCodes.Ldloc, type);
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(OpCodes.Ldloca, texture);
                c.Emit<Patches>(OpCodes.Call, nameof(GetTexturesHook));

                topPatches++;
            }

            if (branchPatches < 2)
            {
                if (branchPatches == 0) Mod.Logger.WarnFormat("Patch error: {0} (DrawTrees:GetTreeBranchTexturePatch)", il.Method.FullName);
                else Mod.Logger.WarnFormat("Patch warning: expected 2 patches in {0} (DrawTrees:GetTreeBranchTexturePatch), got {1}", il.Method.FullName, topPatches);
            }
            if (topPatches < 2)
            {
                if (topPatches == 0) Mod.Logger.WarnFormat("Patch error: {0} (DrawTrees:GetTreeTopTexturePatch)", il.Method.FullName);
                else Mod.Logger.WarnFormat("Patch warning: expected 2 patches in {0} (DrawTrees:GetTreeTopTexturePatch), got {1}", il.Method.FullName, topPatches);
            }
        }

        // true -> vanilla behavior
        private static bool ShakeTreeHook(int x, int y, ref bool createLeaves) 
        {
            Tile tile = Main.tile[x, y];
            if (CustomTree.ByTileType.TryGetValue(tile.TileType, out CustomTree tree))
            {
                return tree.Shake(x, y, ref createLeaves);
            }
            return true;
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
    }
}
