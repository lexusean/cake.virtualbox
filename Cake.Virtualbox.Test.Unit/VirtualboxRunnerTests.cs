using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cake.Virtualbox.Test.Unit
{
    [TestClass]
    public class VirtualboxRunnerTests
    {
        [TestMethod]
        [TestCategory(Global.UnitTest)]
        public void DisplayVersion_CheckArg_Works()
        {
            var fixture = new VirtualboxFixture(r => r.DisplayVersion());

            var result = fixture.Run();

            Assert.AreEqual(1, result.Process.Arguments.Count, "only 1 argument defined");

            var arg = result.Process.Arguments.First();
            Assert.AreEqual("--version", arg.Render(), "failed to set version option");
        }

        [TestMethod]
        [TestCategory(Global.UnitTest)]
        public void Version_CheckArg_Works()
        {
            var version = "test";
            var fixture = new VirtualboxFixture(r => version = r.Version);

            var result = fixture.Run();

            Assert.AreEqual(string.Empty, version, "version expected to be empty");
        }

        [TestMethod]
        [TestCategory(Global.UnitTest)]
        public void Unregistervm_CheckArg_Ok()
        {
            var fixture = new VirtualboxFixture(r => r.UnregisterVm("testuuid"));

            var result = fixture.Run();

            Assert.AreEqual(3, result.Process.Arguments.Count, "should only have 3 argument defined");

            var args = result.Process.Arguments.ToList();
            Assert.AreEqual("unregistervm", args[0].Render(), "failed to set unregistervm argument");
            Assert.AreEqual("testuuid", args[1].Render(), "failed to set nameOrUuid argument");
            Assert.AreEqual("--delete", args[2].Render(), "failed to set --delete argument");
        }

        [TestMethod]
        [TestCategory(Global.UnitTest)]
        [ExpectedException(typeof(ArgumentNullException), "Failed to throw exception when missing vm name parameter")]
        public void Unregistervm_CheckArg_Fail()
        {
            var fixture = new VirtualboxFixture(r => r.UnregisterVm(null));

            var result = fixture.Run();
        }

        [TestMethod]
        [TestCategory(Global.UnitTest)]
        public void ValidateVms_Parsing_Ok()
        {
            const string testStr =
                "\"VECTOR-SBORDEN-DEV\" {266b0698-a32d-40c8-872c-d798a6849a89}\r\n\"master.artifactory.test\" {acd110a6-b343-4299-a6c8-5ba8b58843cc}\r\n\"default\" {23e0e823-ee8f-438d-9ba8-c8ee8d6fad79}\r\n\"micronbase\" {701bd1a8-bfa4-4e15-9134-07e34e282815}\r\n\"micronw7base_1499294072087_17502\" {7f567bfd-4eae-4a68-adee-01c87ed49778}\r\n\"<inaccessible>\" {20373ab6-b101-4c9c-afea-bf1323324823}\r\n\"micronw7base\" {19c3ab26-0746-4ddd-97ae-37552fdea6e8}\r\n\"temp_clone_1499962078732_3547\" {7118eed6-21d2-4a6b-8934-b43ef32d6d9b}";

            var uniqueId = new Guid("266b0698-a32d-40c8-872c-d798a6849a89");

            var vms = VirtualboxRunner.GetVms(testStr).ToArray();

            Assert.AreEqual(8, vms.Length, "expected 8 vms from string");
            Assert.AreEqual(1, vms.Count(t => t.Uuid == uniqueId), "expected 1 with unique id");
        }

        [TestMethod]
        [TestCategory(Global.UnitTest)]
        public void ListVms_CheckArg_Ok()
        {
            var fixture = new VirtualboxFixture(r => r.ListVms());

            var result = fixture.Run();

            Assert.AreEqual(2, result.Process.Arguments.Count, "should only have 2 argument defined");

            var args = result.Process.Arguments.ToList();
            Assert.AreEqual("list", args[0].Render(), "failed to set list argument");
            Assert.AreEqual("vms", args[1].Render(), "failed to set vms argument");
        }
    }
}
