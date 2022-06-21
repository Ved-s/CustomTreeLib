using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace CustomTreeLib
{
    /// <summary/>
    public class CustomTreeLib : Mod
	{
        /// <summary/>
        public static bool DebugMode => Debugger.IsAttached;
    }
}