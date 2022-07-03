using CustomTreeLib.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace CustomTreeLib.AshTree
{
    internal class AshTree : CustomTree
    {
        public override string SaplingTexture => "CustomTreeLib/AshTree/AshTree_Sapling";
        public override string AcornTexture   => "CustomTreeLib/AshTree/AshTree_Acorn";
        public override string TileTexture    => "CustomTreeLib/AshTree/AshTree";
        public override string TopTexture     => "CustomTreeLib/AshTree/AshTree_Tops";
        public override string BranchTexture  => "CustomTreeLib/AshTree/AshTree_Branches";
        public override string LeafTexture    => "CustomTreeLib/AshTree/AshTree_Leaf";
        
        public override int[] ValidGroundTiles => new int[] { TileID.Ash };

        public override Color? MapColor => new(48, 48, 48);
        public override Color? SaplingMapColor => new(30, 30, 30);

        public override bool TryGenerate(int x, int y)
        {
            if (!Main.rand.NextBool(3))
                return false;

            return TreeGrowing.GrowTree(x, y, GetTreeSettings());
        }

        public override bool Shake(int x, int y, ref bool createLeaves)
        {
            if (Main.rand.NextBool(10))
            {
                createLeaves = true;
                NPC.NewNPC(WorldGen.GetItemSource_FromTreeShake(x, y), x * 16, y * 16, NPCID.LavaSlime);
                return false;
            }

            if (Main.rand.NextBool(5))
            {
                createLeaves = true;
                Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16, ItemID.AshBlock, Main.rand.Next(1, 3));
                return false;
            }

            if (Main.rand.NextBool(8))
            {
                createLeaves = true;
                Item.NewItem(WorldGen.GetItemSource_FromTreeShake(x, y), new Vector2(x, y) * 16, ItemID.Obsidian, Main.rand.Next(1, 2));
                return false;
            }

            return true;
        }

        public override bool Drop(int x, int y)
        {
            TreeTileInfo info = TreeTileInfo.GetInfo(x, y);

            if (info.IsLeafy && Main.rand.NextBool(2))
            {
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Vector2(x, y) * 16, Acorn.Type);
            }

            if (info.IsWoody)
            {
                int drop = Main.rand.Next(0, 3);

                if (drop == 1)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Vector2(x, y) * 16, ItemID.AshBlock);
                else if (drop == 2)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(x, y), new Vector2(x, y) * 16, ItemID.SiltBlock);
            }

            return false;
        }

        public override bool GetTreeFoliageData(int i, int j, int xoffset, ref int treeFrame, out int floorY, out int topTextureFrameWidth, out int topTextureFrameHeight)
        {
            Lighting.AddLight(i, j, 1f, .5f, 0);

            if (xoffset != 0 && Main.rand.NextBool(400))
            {
                Vector2 off = new Vector2(xoffset * -10, 0);
                
                if (treeFrame == 0 && xoffset > 0 || treeFrame == 2 && xoffset < 0)
                    off.Y += 10;
                
                Gore drip = Gore.NewGoreDirect(new EntitySource_TileUpdate(i, j), new Vector2(i, j) * 16 + off, default, GoreID.LavaDrip);
                drip.velocity *= 0f;
                drip.frame = 7;
            }
            else if (xoffset == 0 && Main.rand.NextBool(400))
            {
                Vector2 off = new(Main.rand.Next(-32, 32) , 0);

                Gore drip = Gore.NewGoreDirect(new EntitySource_TileUpdate(i, j), new Vector2(i, j) * 16 + off, default, GoreID.LavaDrip);
                drip.velocity *= 0f;
                drip.frame = 7;
            }

            topTextureFrameWidth = 80;
            topTextureFrameHeight = 80;
            floorY = 0;
            return true;
        }
    }
}
