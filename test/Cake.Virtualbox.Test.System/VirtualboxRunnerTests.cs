using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cake.Virtualbox.Test.System
{
    [TestClass]
    public class VirtualboxRunnerTests
    {
        #region Test Setup Members

        public TestContext TestContext { get; set; }

        private static TestVboxHelper TestBoxHelper { get; set; }

        private static TestVboxHelper.TestMachine TestMachine
        {
            get { return TestBoxHelper?.CurrentTestMachine; }
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctxt)
        {
            var workingDir = new DirectoryPath(ctxt.DeploymentDirectory);
            TestBoxHelper = new TestVboxHelper()
            {
                WorkingDirectory = workingDir
            };

            TestBoxHelper.TestBoxName = "VboxRunnerTests";
            TestBoxHelper.CreateTestMachine();
        }

        [ClassCleanup]
        public static void ClassClean()
        {
            TestBoxHelper?.Dispose();
        }

        #endregion

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void DisplayVersion_Works()
        {
            var runner = this.GetRunner();
            var version = runner.Version;

            Assert.IsFalse(string.IsNullOrWhiteSpace(version), "version should be something");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void GetVms_Works()
        {
            var fs = CakeFixtures.CkFileSystem;
            var runner = this.GetRunner();
            var vms = runner.Vms.ToList();

            Assert.IsTrue(vms.Any(), "should have vms defined");
            Assert.IsTrue(vms.Any(t => t.Name == TestMachine.Name), "Missing test VM");

            var testVm = vms.First(t => t.Name == TestMachine.Name);
            Assert.IsNotNull(testVm.VmInfo, "missing vminfo for test vm");
            Assert.AreEqual(1, testVm.VmInfo.Disks.Count, "should have 1 disk defined");

            var testDisk = testVm.VmInfo.Disks.Single();
            Assert.IsNotNull(testDisk.Location, "should have fileinfo for disk location");
            Assert.IsTrue(fs.Exist(testDisk.Location), "disk file should exist");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void CreateVm_RemoveVm_Works()
        {
            var fs = CakeFixtures.CkFileSystem;
            var workingDir = new DirectoryPath(this.TestContext.DeploymentDirectory);
            var vmName = string.Format(TestVboxHelper.TestBoxNameTemplate, "CreateVm_Works");
            var diskName = $"{vmName}.vdi";

            var machine = new TestVboxHelper.TestMachine()
            {
                Created = true,
                HdFile = diskName,
                Name = vmName
            };

            using (var boxHelper = new TestVboxHelper(machine, workingDir))
            {

                var runner = this.GetRunner();

                Assert.IsFalse(runner.Vms.Any(t => t.Name.Equals(vmName)), "Test Vm Shouldn't Exist Yet");

                IProcess processResult = null;

                runner
                    .FromPath(workingDir)
                    .CreateVm(config =>
                        {
                            config.VmName = vmName;
                            config.OsType = "Linux";
                            var ctlSetting = config.AddControllerSetting("SATA", "sata", "IntelAHCI");
                            ctlSetting.AddDiskSetting($"{vmName}.vdi");
                        },
                        proc =>
                        {
                            processResult = proc;
                        });

                Assert.IsNotNull(processResult, "Failed to get process result after create");
                Assert.AreEqual(0, processResult.GetExitCode(), "Error while creating VM");
                Assert.IsTrue(fs.Exist(workingDir.CombineWithFilePath(new FilePath(diskName))),
                    "VM disk is missing");

                Assert.IsTrue(runner.Vms.Any(t => t.Name.Equals(vmName)), "Test Vm Should Exist Now");

                processResult = null;
                runner.RemoveVm(vmName, proc =>
                {
                    processResult = proc;

                    Assert.IsNotNull(processResult, "Failed to get process result after unregister");
                    Assert.AreEqual(0, processResult.GetExitCode(), "Error while removing VM");
                    Assert.IsFalse(fs.Exist(workingDir.CombineWithFilePath(new FilePath(diskName))),
                        "VM disk should be missing");
                });

                Assert.IsFalse(runner.Vms.Any(t => t.Name.Equals(vmName)), "Test Vm Be Removed Now");
            }
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        [ExpectedException(typeof(Cake.Core.CakeException))]
        public void RemoveVm_MissingName()
        {
            var fs = CakeFixtures.CkFileSystem;
            var workingDir = new DirectoryPath(this.TestContext.DeploymentDirectory);
            var vmName = string.Format(TestVboxHelper.TestBoxNameTemplate, "UnRegister_MissingName");
            var diskName = $"{vmName}.vdi";

            var machine = new TestVboxHelper.TestMachine()
            {
                Created = true,
                HdFile = diskName,
                Name = vmName
            };

            using (var boxHelper = new TestVboxHelper(machine, workingDir))
            {
                var runner = this.GetRunner();

                Assert.IsFalse(runner.Vms.Any(t => t.Name.Equals(vmName)), "Test Vm Shouldn't Exist");

                IProcess processResult = null;
                runner.RemoveVm(vmName, proc =>
                {
                    processResult = proc;
                });
            }
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void RemoveVm_DiskOnly()
        {
            var fs = CakeFixtures.CkFileSystem;
            var workingDir = new DirectoryPath(this.TestContext.DeploymentDirectory);
            var vmName = string.Format(TestVboxHelper.TestBoxNameTemplate, "RemoveVm_DiskOnly");
            workingDir = workingDir.Combine(new DirectoryPath(vmName));
            var dir = fs.GetDirectory(workingDir);
            dir.Create();

            var diskName = $"{vmName}.vdi";

            var machine = new TestVboxHelper.TestMachine()
            {
                Created = true,
                HdFile = diskName,
                Name = vmName
            };

            using (var boxHelper = new TestVboxHelper(machine, workingDir))
            {
                boxHelper.CreateDisk(diskName);
                var runner = this.GetRunner();

                Assert.IsFalse(runner.Vms.Any(t => t.Name.Equals(vmName)), "Test Vm Shouldn't Exist");
                Assert.IsTrue(fs.Exist(workingDir.CombineWithFilePath(new FilePath(diskName))),
                    "VM disk is missing");

                IProcess processResult = null;
                runner.RemoveVm(vmName, proc =>
                {
                    processResult = proc;

                    var stderr = string.Join("\n", processResult.GetStandardError());
                    Assert.AreEqual(0, processResult.GetExitCode(), "Process result return non zero code.");
                });

                Assert.IsFalse(fs.Exist(workingDir.CombineWithFilePath(new FilePath(diskName))),
                    "VM disk should be removed");
            }
        }

        #region Helper Methods

        private VirtualboxRunner GetRunner()
        {
            return new VirtualboxRunner(
                CakeFixtures.CkFileSystem,
                CakeFixtures.CkEnv,
                CakeFixtures.CkProcRunner,
                CakeFixtures.CkToolLocator,
                CakeFixtures.CkLog);
        }

        #endregion
    }
}
