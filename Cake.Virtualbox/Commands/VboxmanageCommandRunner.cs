using System;
using System.Collections.Generic;
using System.Text;
using Cake.Core.Diagnostics;
using Cake.Core.IO;

namespace Cake.Virtualbox.Commands
{
    /// <summary>
    /// base class for virtualbox commands
    /// </summary>
    public abstract class VboxmanageCommandRunner
    {
        #region Protected Properties

        /// <summary>
        /// Settings for vboxmanage cli
        /// </summary>
        protected VirtualboxSettings Settings { get; set; }

        /// <summary>
        /// Logging Output
        /// </summary>
        protected ICakeLog Log { get; set; }

        /// <summary>
        /// Action to trigger invocation of cli
        /// </summary>
        protected Action<VirtualboxSettings, ProcessArgumentBuilder, ProcessSettings, Action<IProcess>> Runner { get; set; }

        #endregion

        #region Ctor

        internal VboxmanageCommandRunner(
            ICakeLog log,
            Action<VirtualboxSettings, ProcessArgumentBuilder, ProcessSettings, Action<IProcess>> runCallback,
            VirtualboxSettings settings)
        {
            this.Log = log;
            this.Runner = runCallback;
            this.Settings = settings;
        }

        #endregion

        #region Protected Methods

        protected void Run(
            VirtualboxSettings settings,
            ProcessArgumentBuilder args)
        {
            this.Run(settings, args, null, null);
        }

        protected void Run(
            VirtualboxSettings settings,
            ProcessArgumentBuilder args,
            ProcessSettings procSettings,
            Action<IProcess> procCallback)
        {
            this.Runner.Invoke(settings, args, procSettings, procCallback);
        }

        #endregion
    }
}
