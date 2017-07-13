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
    public class VirtualboxHddRunner : VboxmanageCommandRunner
    {
        #region Static Methods

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

        public void List(Action<IProcess> postAction = null)
        {
            var args = new ProcessArgumentBuilder();
            args.Append("list");
            args.Append("hdds");

            this.Run(this.Settings, args, null, postAction);
        }

        public void RemoveDisks(string nameOrUuid, Action<IProcess> callback = null)
        {
            if(string.IsNullOrWhiteSpace(nameOrUuid))
                throw new ArgumentNullException(nameof(nameOrUuid), "the name or uuid of the disk cannot be empty");

            var args = new ProcessArgumentBuilder();
            args.Append("closemedium");
            args.Append("disk");
            args.Append(nameOrUuid);
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
