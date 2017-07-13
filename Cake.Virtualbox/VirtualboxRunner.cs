using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.Virtualbox.Commands;
using Cake.Virtualbox.Models;

namespace Cake.Virtualbox
{
    /// <summary>
    /// Wrapper around virtualbox CLI functionality
    /// </summary>
    public class VirtualboxRunner : Tool<VirtualboxSettings>
    {
        #region Static Methods

        public static IEnumerable<VboxVm> GetVms(string vmList)
        {
            var split = (vmList ?? string.Empty).Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            var lines = split.Select(t => (t ?? string.Empty).Trim());

            foreach (var line in lines)
            {
                yield return VboxVm.GetVm(line);
            }
        }

        #endregion

        #region Public Properties

        public VirtualboxHddRunner Hdd { get; }

        public string Version
        {
            get { return this.GetVersion(); }
        }

        public IEnumerable<VboxVm> Vms
        {
            get
            {
                var hddListStr = this.GetVmList();
                return GetVms(hddListStr);
            }
        }

        #endregion

        #region Private Properties

        private ICakeLog Log { get; }
        private IFileSystem FileSystem { get; }
        private VirtualboxSettings Settings { get; }

        private DirectoryPath WorkingDirectory { get; set; }

        #endregion

        #region Ctor

        public VirtualboxRunner(
            IFileSystem fileSystem,
            ICakeEnvironment environment,
            IProcessRunner processRunner,
            IToolLocator tools,
            ICakeLog log)
            : base(fileSystem, environment, processRunner, tools)
        {
            this.FileSystem = fileSystem;
            this.Log = log;

            this.Settings = new VirtualboxSettings();

            this.Hdd = new VirtualboxHddRunner(log, this.Run, this.Settings);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets the working directory for virtualbox commands
        /// </summary>
        /// <param name="path">The directory path to run virtualbox commands from</param>
        /// <returns>The command runner</returns>
        /// <example>
        ///     <code><![CDATA[
        /// Virtualbox.FromPath("./path/to/dir").Up();
        /// ]]></code>
        /// </example>
        public VirtualboxRunner FromPath(DirectoryPath path)
        {
            this.WorkingDirectory = path;
            return this;
        }

        public void DisplayVersion(Action<IProcess> callback = null)
        {
            var args = new ProcessArgumentBuilder();
            args.Append("--version");

            this.Run(this.Settings, args, null, callback);
        }

        public void ListVms(Action<IProcess> callback = null)
        {
            var args = new ProcessArgumentBuilder();
            args.Append("list");
            args.Append("vms");

            this.Run(this.Settings, args, null, callback);
        }

        public void UnregisterVm(string nameOrUuid, Action<IProcess> callback = null)
        {
            if (string.IsNullOrWhiteSpace(nameOrUuid))
                throw new ArgumentNullException(nameof(nameOrUuid), "the name or uuid of the vm cannot be empty");

            var args = new ProcessArgumentBuilder();
            args.Append("unregistervm");
            args.Append(nameOrUuid);
            args.Append("--delete");

            this.Run(this.Settings, args, null, callback);
        }

        public void RemoveBoxByName(string boxName)
        {
            //begin removing vms
            var vmList = this.Vms.ToArray();
            var vmsThatMatchBox = vmList
                .Where(t => (t.Name ?? string.Empty).Equals(boxName, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            this.Log.Information("Found {0} vms that match box: {1}", vmsThatMatchBox.Length, boxName);

            foreach (var vm in vmsThatMatchBox)
            {
                if(vm.Uuid == null)
                    continue;
                
                this.Log.Information("Unregistering vm {0} with UUID: {1}", vm.Name, vm.Uuid.ToString());
                this.UnregisterVm(vm.Uuid.ToString());
            }

            //then disks
        }

        #endregion

        #region Override Members

        protected override string GetToolName()
        {
            return "VirtualBox by Oracle";
        }

        protected override DirectoryPath GetWorkingDirectory(VirtualboxSettings settings)
        {
            if (this.WorkingDirectory == null)
                return base.GetWorkingDirectory(settings);

            if (!this.FileSystem.Exist(this.WorkingDirectory))
                throw new DirectoryNotFoundException($"Working directory path not found [{this.WorkingDirectory.FullPath}]");

            return this.WorkingDirectory;
        }

        protected override IEnumerable<string> GetToolExecutableNames()
        {
            yield return "vboxmanage.exe";
        }

        #endregion

        #region Private Methods

        private string GetVersion()
        {
            var resultString = string.Empty;

            this.DisplayVersion(process =>
            {
                if(process.GetExitCode() == 0)
                    resultString = string.Join(";", process.GetStandardOutput() ?? Enumerable.Empty<string>());
            });

            return resultString;
        }

        private string GetVmList()
        {
            var vmOutput = string.Empty;
            this.ListVms(proc =>
            {
                if (proc.GetExitCode() == 0)
                    vmOutput = string.Join("\n", proc.GetStandardOutput() ?? Enumerable.Empty<string>());
            });

            return vmOutput;
        }

        #endregion
    }
}
