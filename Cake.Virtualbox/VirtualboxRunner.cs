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
using Cake.Virtualbox.Settings;

namespace Cake.Virtualbox
{
    /// <summary>
    /// Wrapper around virtualbox CLI functionality
    /// </summary>
    public class VirtualboxRunner : Tool<VirtualboxSettings>
    {
        #region Static Methods

        public static IEnumerable<VboxVm> GetVms(
            string vmList,
            Func<Guid, string> getVmInfoFunc = null,
            Func<Guid, VboxHdd> getHddFunc = null)
        {
            var split = (vmList ?? string.Empty).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var lines = split.Select(t => (t ?? string.Empty).Trim());

            foreach (var line in lines)
            {
                yield return VboxVm.GetVm(
                    line,
                    vm =>
                    {
                        if (vm.Uuid == null)
                            return null;

                        if (getVmInfoFunc == null)
                            return null;

                        return getVmInfoFunc(vm.Uuid.Value);
                    },
                    hddId =>
                    {
                        if (getHddFunc == null)
                            return null;

                        return getHddFunc(hddId);
                    });
            }
        }

        #endregion

        #region Public Properties

        public VirtualboxHddRunner HddRunner { get; }

        public string Version
        {
            get { return this.GetVersion(); }
        }

        public IEnumerable<VboxVm> Vms
        {
            get
            {
                var hddListStr = this.GetVmList();
                return GetVms(
                    hddListStr,
                    vmId =>
                    {
                        return this.GetVmInfo(vmId);
                    },
                    hddId =>
                    {
                        return this.HddRunner.Hdds
                            .FirstOrDefault(t => t.Uuid == hddId);
                    });
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

            this.HddRunner = new VirtualboxHddRunner(log, this.Run, this.Settings);
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

        public void ShowVmInfo(string vmName, Action<IProcess> callback = null)
        {
            Guid vmId = Guid.Empty;
            if (Guid.TryParse(vmName, out vmId))
            {
                this.ShowVmInfo(vmId);
            }
            else
            {
                var args = new ProcessArgumentBuilder();
                args.Append("showvminfo");
                args.Append(vmName);

                this.Run(this.Settings, args, null, callback);
            }
        }

        public void ShowVmInfo(Guid vmId, Action<IProcess> callback = null)
        {
            var args = new ProcessArgumentBuilder();
            args.Append("showvminfo");
            args.Append(vmId.ToString());

            this.Run(this.Settings, args, null, callback);
        }

        public void CreateVm(Action<CreateVmSettings> configAction = null, Action<IProcess> callback = null)
        {
            var settings = new CreateVmSettings();
            configAction?.Invoke(settings);

            this.RunCreateVm(settings, callback);
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
                if (vm.Uuid == null)
                    continue;

                this.Log.Information("Unregistering vm {0} with UUID: {1}", vm.Name, vm.Uuid.ToString());
                this.UnregisterVm(vm.Uuid.ToString());

                if (vm.VmInfo != null)
                {
                    foreach (var disk in vm.VmInfo.Disks)
                    {
                        if (disk.Uuid == null)
                            continue;

                        this.Log.Information("Removing disk UUID: {0} for vm UUID: {1}", disk.Uuid.ToString(), vm.Uuid.ToString());
                        this.HddRunner.RemoveDisks(disk.Uuid.ToString());
                    }
                }
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

        private string GetVmInfo(Guid vmId)
        {
            var vmOutput = string.Empty;
            this.ShowVmInfo(vmId, proc =>
            {
                if (proc.GetExitCode() == 0)
                    vmOutput = string.Join("\n", proc.GetStandardOutput() ?? Enumerable.Empty<string>());
            });

            return vmOutput;
        }

        private string GetVmInfo(string vmName)
        {
            var vmOutput = string.Empty;
            this.ShowVmInfo(vmName, proc =>
            {
                if (proc.GetExitCode() == 0)
                    vmOutput = string.Join("\n", proc.GetStandardOutput() ?? Enumerable.Empty<string>());
            });

            return vmOutput;
        }

        private void RunCreateHd(CreateVmSettings.VboxDiskSetting diskSetting, Action<IProcess> callback = null)
        {
            if (diskSetting == null)
                return;

            var args = new ProcessArgumentBuilder();
            args.Append("createhd");
            args.Append("--filename");
            args.Append(diskSetting.FileName);
            args.Append("--size");
            args.Append(diskSetting.Size.ToString());

            this.Log.Information("createhd name: {0}", diskSetting.FileName);

            this.Run(this.Settings, args, null, callback);
        }

        private void RunAttachStorage(
            string vmName,
            CreateVmSettings.VboxStorageControllerSetting controllerSetting,
            CreateVmSettings.VboxDiskSetting diskSetting, 
            Action<IProcess> callback = null)
        {
            if (string.IsNullOrWhiteSpace(vmName) ||
                controllerSetting == null ||
                diskSetting == null)
                return;

            var args = new ProcessArgumentBuilder();
            args.Append("storageattach");
            args.Append(vmName);
            args.Append("--storagectl");
            args.Append(controllerSetting.Name);
            args.Append("--port");
            args.Append(diskSetting.Port.ToString());
            args.Append("--device");
            args.Append(diskSetting.Device.ToString());
            args.Append("--type");
            args.Append("hdd");
            args.Append("--medium");
            args.Append(diskSetting.FileName);

            this.Log.Information("storageattach name: {0} to controller: {1}", diskSetting.FileName, controllerSetting.Name);

            this.Run(this.Settings, args, null, callback);
        }

        private void RunCreateStorageCtl(string vmName, CreateVmSettings.VboxStorageControllerSetting controllerSetting, Action<IProcess> callback = null)
        {
            if (string.IsNullOrWhiteSpace(vmName) || controllerSetting == null)
                return;

            var args = new ProcessArgumentBuilder();
            args.Append("storagectl");
            args.Append(vmName);
            args.Append("--name");
            args.Append(controllerSetting.Name);
            args.Append("--add");
            args.Append(controllerSetting.Type);
            args.Append("--controller");
            args.Append(controllerSetting.Controller);

            this.Log.Information("storagectl add name: {0}", controllerSetting.Name);

            this.Run(this.Settings, args, null, proc =>
            {
                if (proc.GetExitCode() != 0)
                {
                    this.Log.Error("Failed to add storagectl for: {0}. Bailing", controllerSetting.Name);
                }
                else
                {
                    foreach (var diskSetting in controllerSetting.DiskSettings)
                    {
                        this.RunCreateHd(diskSetting, diskProc =>
                        {
                            if (diskProc.GetExitCode() != 0)
                            {
                                this.Log.Error("Failed to create disk: {0} for controller: {1}", diskSetting.FileName, controllerSetting.Name);
                            }
                            else
                            {
                                this.RunAttachStorage(vmName, controllerSetting, diskSetting, attachProc =>
                                {
                                    if (attachProc.GetExitCode() != 0)
                                    {
                                        this.Log.Error("Failed to attach disk: {0} for controller: {1}", diskSetting.FileName, controllerSetting.Name);
                                    }
                                });
                            }
                        });
                    }
                }

                callback?.Invoke(proc);
            });
        }

        private void RunCreateVm(CreateVmSettings vmSetting, Action<IProcess> callback = null)
        {
            if (vmSetting == null)
                return;

            var args = new ProcessArgumentBuilder();
            args.Append("createvm");
            args.Append("--name");
            args.Append(vmSetting.VmName);
            args.Append("--ostype");
            args.Append(vmSetting.OsType);
            args.Append("--register");

            this.Log.Information("createvm name: {0}", vmSetting.VmName);

            this.Run(this.Settings, args, null, proc =>
            {
                if (proc.GetExitCode() != 0)
                {
                    this.Log.Error("Failed to create vm for: {0}. Bailing", vmSetting.VmName);
                }
                else
                {
                    foreach (var contollerSetting in vmSetting.ControllerSettings)
                    {
                        this.RunCreateStorageCtl(vmSetting.VmName, contollerSetting, controllerProc =>
                        {
                            if (controllerProc.GetExitCode() != 0)
                            {
                                this.Log.Error("Failed to create controller: {0} for vm: {1}", contollerSetting.Name, vmSetting.VmName);
                            }
                        });
                    }
                }

                callback?.Invoke(proc);
            });
        }

        #endregion
    }
}
