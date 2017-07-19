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

        /// <summary>
        /// Run vboxmanage command
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="args"></param>
        protected void Run(
            VirtualboxSettings settings,
            ProcessArgumentBuilder args)
        {
            this.Run(settings, args, null, null);
        }

        /// <summary>
        /// Run vboxmanage command with process settings and callback
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="args"></param>
        /// <param name="procSettings"></param>
        /// <param name="procCallback"></param>
        protected void Run(
            VirtualboxSettings settings,
            ProcessArgumentBuilder args,
            ProcessSettings procSettings,
            Action<IProcess> procCallback)
        {
            
            this.Runner.Invoke(settings, args, this.GetProcessSettings(procSettings, procCallback != null), procCallback);
        }

        /// <summary>
        /// Get process settings. Creates default process settings if not included and add redirection.
        /// </summary>
        /// <param name="originalSettings"></param>
        /// <param name="isRedirected"></param>
        /// <returns></returns>
        protected ProcessSettings GetProcessSettings(ProcessSettings originalSettings = null, bool isRedirected = false)
        {
            var settings = originalSettings != null ? originalSettings : this.CreateRedirectedProcessSettings();
            settings.RedirectStandardOutput = isRedirected;
            settings.RedirectStandardError = isRedirected;

            return settings;
        }

        /// <summary>
        /// Gets default process settings
        /// </summary>
        /// <returns></returns>
        protected ProcessSettings CreateRedirectedProcessSettings()
        {
            return new ProcessSettings()
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
        }

        #endregion
    }
}