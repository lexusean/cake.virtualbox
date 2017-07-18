using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.IO;

namespace Cake.Virtualbox.Test.System
{
    public class TestVboxHelper : IDisposable
    {
        #region Custom Types

        public class TestMachine
        {
            public string Name { get; set; }
            public string HdFile { get; set; }
            public bool Created { get; set; }
        }

        public class TestCreateVmException : Exception
        {
            public IEnumerable<string> ErrorLines { get; set; }

            public TestCreateVmException(
                string message,
                Exception innerException = null)
                : base(message, innerException)
            {
            }
        }

        #endregion

        #region Constants

        public const string TestBoxNameTemplate = "TestBox_{0}";
        public const string TestBoxNameDefault = "default";
        public const string VboxmanageFilePath = "vboxmanage.exe";
        public const int BoxSizeDefault = 32768;

        #endregion

        #region Public Properties

        private string _TestBoxName = TestBoxNameDefault;
        public string TestBoxName
        {
            get { return string.Format(TestBoxNameTemplate, this._TestBoxName ?? TestBoxNameDefault); }
            set { this._TestBoxName = value; }
        }

        public DirectoryPath WorkingDirectory { get; set; }

        public TestMachine CurrentTestMachine { get; private set; }

        public List<string> DisksCreated { get; } = new List<string>();

        #endregion

        #region Ctor

        public TestVboxHelper()
        {
        }

        public TestVboxHelper(TestMachine machine, DirectoryPath cwd)
        {
            this.CurrentTestMachine = machine;
            this.WorkingDirectory = cwd;
        }

        #endregion

        #region Public Methods

        public void CreateDisk(string hdName)
        {
            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "createhd");
            this.AddArgument(procSettings, "--filename");
            this.AddArgument(procSettings, $"\"{hdName}\"");
            this.AddArgument(procSettings, "--size");
            this.AddArgument(procSettings, BoxSizeDefault.ToString());

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                var stderr = string.Join("\n", proc.GetStandardError());
                throw new Exception(string.Format("Failed to CreateDisk: Error: {0}", stderr));
            }
        }

        public TestMachine CreateTestMachine()
        {
            if (this.CurrentTestMachine != null)
                return this.CurrentTestMachine;

            var vmName = this.TestBoxName;
            var hdName = string.Format("{0}.vdi", vmName);
            var controllerName = "SATA Controller";

            this.CurrentTestMachine = new TestMachine()
            {
                Name = vmName,
                HdFile = hdName
            };
            
            this.TryCreateVm(vmName);
            this.CreateController(vmName, controllerName);
            this.CreateHd(hdName);
            this.AttachStorage(vmName, controllerName, hdName);

            this.CurrentTestMachine.Created = true;

            return this.CurrentTestMachine;
        }

        public void DestroyTestMachine(string vmName)
        {
            this.DestroyTestMachine(new TestMachine()
            {
                Created = true,
                Name = vmName
            });
        }

        public void DestroyTestMachine(TestMachine machine = null)
        {
            machine = machine ?? this.CurrentTestMachine;

            this.DestroyMachine(machine, true);
        }

        #endregion

        #region Private Methods

        private IProcessRunner GetProcRunner()
        {
            return CakeFixtures.CkProcRunner;
        }

        private ProcessSettings CreateProcessSettings()
        {
            return new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder(),
                WorkingDirectory = this.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Silent = false
            };
        }

        private void AddArgument(ProcessSettings setting, string arg)
        {
            if (setting == null)
                return;

            setting.Arguments = setting.Arguments ?? new ProcessArgumentBuilder();

            if (string.IsNullOrWhiteSpace(arg))
                return;

            setting.Arguments.Append(arg);
        }

        private void CreateHd(string hdName)
        {
            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "createhd");
            this.AddArgument(procSettings, "--filename");
            this.AddArgument(procSettings, $"\"{hdName}\"");
            this.AddArgument(procSettings, "--size");
            this.AddArgument(procSettings, BoxSizeDefault.ToString());

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                throw new Exception(string.Format("Failed to createhd: Error: {0}", string.Join("\n", proc.GetStandardError())));
            }

            this.DisksCreated.Add(hdName);
        }

        private void TryCreateVm(string vmName)
        {
            try
            {
                this.CreateVm(vmName);
            }
            catch (TestCreateVmException ex)
            {
                var machine = new TestMachine()
                {
                    Created = true,
                    HdFile = string.Empty,
                    Name = vmName
                };

                this.HaltMachine(machine, true);
                Thread.Sleep(3000);
                this.DestroyMachine(machine, true);

                var vmFile = VboxHelper.ExistingMachineSettingsFile(ex.ErrorLines);
                if (!string.IsNullOrWhiteSpace(vmFile))
                {
                    var vmFilePath = new FilePath(vmFile);
                    var dirPath = vmFilePath.GetDirectory();
                    var fs = CakeFixtures.CkFileSystem;
                    var file = fs.GetFile(vmFilePath);
                    if(file.Exists)
                        file.Delete();

                    var dir = fs.GetDirectory(dirPath);
                    if(dir.Exists)
                        dir.Delete(true);
                }

                this.CreateVm(vmName);
            }
        }

        private void CreateVm(string vmName)
        {
            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "createvm");
            this.AddArgument(procSettings, "--name");
            this.AddArgument(procSettings, $"\"{vmName}\"");
            this.AddArgument(procSettings, "--ostype");
            this.AddArgument(procSettings, "Linux");
            this.AddArgument(procSettings, "--register");

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                var errLines = proc.GetStandardError().ToArray();
                var stderr = string.Join("\n", errLines);
                var ex =
                    new TestCreateVmException(string.Format("Failed to createvm: Error: {0}", stderr))
                    {
                        ErrorLines = errLines
                    };

                throw ex;
            }
        }

        private void CreateController(string vmName, string controllerName)
        {
            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "storagectl");
            this.AddArgument(procSettings, $"\"{vmName}\"");
            this.AddArgument(procSettings, "--name");
            this.AddArgument(procSettings, $"\"{controllerName}\"");
            this.AddArgument(procSettings, "--add");
            this.AddArgument(procSettings, "sata");
            this.AddArgument(procSettings, "--controller");
            this.AddArgument(procSettings, "IntelAHCI");

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                var stdout = string.Join("\n", proc.GetStandardOutput());
                var stderr = string.Join("\n", proc.GetStandardError());
                throw new Exception(string.Format("Failed to storagectl create Controller: Error: {0}", stderr));
            }
        }

        private void AttachStorage(string vmName, string controllerName, string hdName)
        {
            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "storageattach");
            this.AddArgument(procSettings, $"\"{vmName}\"");
            this.AddArgument(procSettings, "--storagectl");
            this.AddArgument(procSettings, $"\"{controllerName}\"");
            this.AddArgument(procSettings, "--port");
            this.AddArgument(procSettings, "0");
            this.AddArgument(procSettings, "--device");
            this.AddArgument(procSettings, "0");
            this.AddArgument(procSettings, "--type");
            this.AddArgument(procSettings, "hdd");
            this.AddArgument(procSettings, "--medium");
            this.AddArgument(procSettings, hdName);

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                throw new Exception(string.Format("Failed to storageattach: Error: {0}", string.Join("\n", proc.GetStandardError())));
            }
        }

        private void HaltMachine(TestMachine machine, bool ignoreError = false)
        {
            if (machine == null)
                return;

            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "controlvm");
            this.AddArgument(procSettings, $"\"{machine.Name}\"");
            this.AddArgument(procSettings, "poweroff");

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                var stderr = string.Join("\n", proc.GetStandardError());

                if (!ignoreError)
                {
                    throw new Exception(string.Format("Failed to controlvm poweroff: Error: {0}", stderr));
                }
            }
        }

        private void DestroyMachine(TestMachine machine, bool ignoreError = false)
        {
            if (machine == null)
                return;

            var runner = this.GetProcRunner();
            var procSettings = this.CreateProcessSettings();
            this.AddArgument(procSettings, "unregistervm");
            this.AddArgument(procSettings, $"\"{machine.Name}\"");
            this.AddArgument(procSettings, "--delete");

            var proc = runner.Start(VboxmanageFilePath, procSettings);
            proc.WaitForExit();

            if (proc.GetExitCode() != 0)
            {
                var stderr = string.Join("\n", proc.GetStandardError());

                if (!ignoreError)
                {
                    throw new Exception(string.Format("Failed to unregistervm: Error: {0}", stderr));
                }
            }
        }

        #endregion

        #region IDisposable Members

        private bool IsDisposed { get; set; }

        private void ReleaseUnmanagedResources()
        {
            if (this.CurrentTestMachine == null)
                return;

            this.HaltMachine(this.CurrentTestMachine, true);
            Thread.Sleep(3000);
            this.DestroyMachine(this.CurrentTestMachine, true);

            var fs = CakeFixtures.CkFileSystem;
            foreach (var disk in this.DisksCreated)
            {
                var file = this.WorkingDirectory.CombineWithFilePath(new FilePath(disk));
                if (fs.Exist(file))
                {
                    fs.GetFile(file).Delete();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            this.ReleaseUnmanagedResources();

            if (disposing)
            {
            }

            this.IsDisposed = true;
        }

        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        ~TestVboxHelper()
        {
            if (!this.IsDisposed)
            {
                this.Dispose(false);
            }
        }

        #endregion
    }
}
