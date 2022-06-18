using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
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

        /// <summary/>
        public void Load(Mod mod) => TreeLoader.LoadGlobal(this);
        
        /// <summary/>
        public void Unload() => TreeLoader.UnloadGlobal(this);
    }
}
