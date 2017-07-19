using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Virtualbox.Models;

namespace Cake.Virtualbox.Commands
{
    /// <summary>
    /// Handler for vboxmanage disk operations
    /// </summary>
    public class VirtualboxHddRunner : VboxmanageCommandRunner
    {
        #region Static Methods

        /// <summary>
        /// Gets list of disk models for given hd list vboxmanage list hdds
        /// </summary>
        /// <param name="hddList">response from vboxmanage list hdds</param>
        /// <returns>Disk Models</returns>
        public static IEnumerable<VboxHdd> GetHdds(string hddList)
        {
            const string hddsBlockRegex = @"^(UUID:)(?:.(?!^\s*$)|\n(?!^\s*$))*";
            var hddsBlockOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            var blockRegex = new StringBuilder();
            blockRegex.Append(hddsBlockRegex);
            
            var matches = Regex.Matches(hddList, blockRegex.ToString(), hddsBlockOptions);
            if (matches.Count <= 0)
                yield break;

            foreach (Match match in matches)
            {
                yield return VboxHdd.GetHdd(match.Value);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// All disks in vboxmanage list hdds command response. Is lazy loaded.
        /// </summary>
        public IEnumerable<VboxHdd> Hdds
        {
            get
            {
                var hddListStr = this.GetHddList();
                return GetHdds(hddListStr);
            }
        }

        #endregion

        #region Ctor

        internal VirtualboxHddRunner(
            ICakeLog log, 
            Action<VirtualboxSettings, ProcessArgumentBuilder, ProcessSettings, Action<IProcess>> runCallback,
            VirtualboxSettings settings) 
            : base(log, runCallback, settings)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows List of hdds registered in vboxmanage
        /// </summary>
        /// <param name="callback"></param>
        public void List(Action<IProcess> callback = null)
        {
            var args = new ProcessArgumentBuilder();
            args.Append("list");
            args.Append("hdds");

            this.Run(this.Settings, args, null, callback);
        }

        /// <summary>
        /// Removes the disk from vboxmanage
        /// </summary>
        /// <param name="nameOrUuid">This can be the UUID of the disk or string that is present in disk location (file path)</param>
        /// <param name="callback"></param>
        public void RemoveDisks(string nameOrUuid, Action<IProcess> callback = null)
        {
            if(string.IsNullOrWhiteSpace(nameOrUuid))
                throw new ArgumentNullException(nameof(nameOrUuid), "the name or uuid of the disk cannot be empty");

            var args = new ProcessArgumentBuilder();
            args.Append("closemedium");
            args.Append("disk");
            args.Append($"\"{nameOrUuid}\"");
            args.Append("--delete");

            this.Run(this.Settings, args, null, callback);
        }

        #endregion

        #region Private Methods

        private string GetHddList()
        {
            var hddOutput = string.Empty;
            this.List(proc =>
            {
                if (proc.GetExitCode() == 0)
                    hddOutput = string.Join("\n", proc.GetStandardOutput() ?? Enumerable.Empty<string>());
            });

            return hddOutput;
        }

        #endregion
    }
}