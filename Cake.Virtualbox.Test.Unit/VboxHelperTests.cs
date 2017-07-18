using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Testing;
using Cake.Virtualbox.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cake.Virtualbox.Test.Unit
{
    [TestClass]
    public class VboxHelperTests
    {
        #region Constants

        private const string ExistingMachineSettingsFile_Works_String = "vboxmanage.exe: error: Machine settings file \'Y:\\VirtualboxVms\\TestBox_default\\TestBox_default.vbox\' already exists";
        private const string ExistingMachineSettingsFile_NoWorky_String = "vboxmanage.exe: error: Details: code VBOX_E_FILE_ERROR (0x80bb0004), component MachineWrap, interface IMachine, callee IUnknown";

        #endregion

        #region Test Methods

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void ExistingMachineSettingsFile_Single_Works()
        {
            var file = VboxHelper.ExistingMachineSettingsFile(ExistingMachineSettingsFile_Works_String);

            Assert.IsFalse(string.IsNullOrWhiteSpace(file), "should have found file");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void ExistingMachineSettingsFile_Multi_Works()
        {
            var strings = new string[]
            {
                ExistingMachineSettingsFile_Works_String,
                ExistingMachineSettingsFile_NoWorky_String
            };

            var file = VboxHelper.ExistingMachineSettingsFile(strings);

            Assert.IsFalse(string.IsNullOrWhiteSpace(file), "should have found file");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void ExistingMachineSettingsFile_Single_NoWorky()
        {
            var file = VboxHelper.ExistingMachineSettingsFile(ExistingMachineSettingsFile_NoWorky_String);

            Assert.IsTrue(string.IsNullOrWhiteSpace(file), "should not have found file");
        }

        #endregion
    }
}
