using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CustomTreeLib
{
    /// <summary>
    /// Class for overriding behavior for all trees
    /// </summary>
    public abstract class GlobalTree : ILoadable
    {
        /// <summary>
        /// Executed when tree is being shook, return true to continue vanilla code for tree shaking
        /// </summary>
        public virtual bool Shake(int x, int y, int type, ref bool createLeaves)
        {
            return true;
        }

        /// <summary>
        /// Use this to modify tree settings
        /// </summary>
        public virtual void ModifyTreeSettings(int x, int y, int type, ref TreeSettings settings) 
        {
        }

        /// <summary>
        /// Executed when tree randomly decides to grow higher<br/>
        /// Return false to prevent it from growing
        /// </summary>
        public virtual bool CanGrowMore(Point topPos, TreeSettings settings, TreeStats stats)
        {
            return true;
        }

        /// <summary>
        /// Executed before drawing tree foliage<br/>
        /// Return false to prevent foliage drawing
        /// </summary>
        /// <param name="type">Tree tile type</param>
        /// <param name="position">Draw position</param>
        /// <param name="size">Draw size</param>
        /// <param name="foliageType">Foliage type</param>
        /// <param name="treeFrame">Tree style frame</param>
        /// <param name="origin">Draw origin</param>
        /// <param name="color">Draw color</param>
        /// <param name="rotation">Draw rotation</param>
        /// <returns></returns>
        public virtual bool PreDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            return true;
        }

        /// <summary>
        /// Executed after drawing tree foliage
        /// </summary>
        /// <param name="type">Tree tile type</param>
        /// <param name="position">Draw position</param>
        /// <param name="size">Draw size</param>
        /// <param name="foliageType">Foliage type</param>
        /// <param name="treeFrame">Tree style frame</param>
        /// <param name="origin">Draw origin</param>
        /// <param name="color">Draw color</param>
        /// <param name="rotation">Draw rotation</param>
        public virtual void PostDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
        }

        /// <summary/>
        public void Load(Mod mod) => TreeLoader.LoadGlobal(this);
        
        /// <summary/>
        public void Unload() => TreeLoader.UnloadGlobal(this);
    }

    public class TestGlobalTree : GlobalTree 
    {
        public override bool PreDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            return foliageType != TreeFoliageType.Top || treeFrame == 0;
        }

        public override void PostDrawFoliage(int type, Vector2 position, Point size, TreeFoliageType foliageType, int treeFrame, Vector2 origin, Color color, float rotation)
        {
            Color c = foliageType switch
            {
                TreeFoliageType.Top => Color.Red,
                TreeFoliageType.LeftBranch => Color.Green,
                TreeFoliageType.RightBranch => Color.Blue,
                _ => Color.White
            };

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, size.X, size.Y), c * .3f, rotation, origin, 1f, SpriteEffects.None, 1f);
        }
    }
}
