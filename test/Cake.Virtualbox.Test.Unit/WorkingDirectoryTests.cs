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
    public class WorkingDirectoryTests
    {
        [TestMethod]
        [TestCategory(Global.TestType)]
        public void Should_Set_Valid_DirectoryPath()
        {
            var fixture = new VirtualboxFixture(r => r.FromPath("./vm").DisplayVersion());
            fixture.FileSystem.CreateDirectory("./vm");

            var result = fixture.Run();

            Assert.AreEqual("/Working/vm", result.Process.WorkingDirectory.FullPath, "Failed to set working directory");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        public void Should_Run_With_Defaults()
        {
            var fixture = new VirtualboxFixture(r => r.DisplayVersion());
            var result = fixture.Run();

            Assert.AreEqual("/Working", result.Process.WorkingDirectory.FullPath, "Failed to set default working directory");
        }

        [TestMethod]
        [TestCategory(Global.TestType)]
        [ExpectedException(typeof(System.IO.DirectoryNotFoundException), "Expected DirectoryNotFoundException")]
        public void Should_Throw_On_NonExistent_Directory()
        {
            var fixture = new VirtualboxFixture(r => r.FromPath("./fake").DisplayVersion());
            var result = fixture.Run();
        }
    }
}
