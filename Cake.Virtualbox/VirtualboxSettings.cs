using System;
using System.Collections.Generic;
using System.Text;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Virtualbox
{
    /// <summary>
    /// Settings to invoke vboxmange cli
    /// </summary>
    public class VirtualboxSettings : ToolSettings
    {
    }

    internal interface IVirtualboxCommandSettings
    {
        /// <summary>
        /// Gets the command arguments 
        /// </summary>
        /// <returns>An action to add required command arguments</returns>
        Action<ProcessArgumentBuilder> GetToolsArguments();
    }
}
