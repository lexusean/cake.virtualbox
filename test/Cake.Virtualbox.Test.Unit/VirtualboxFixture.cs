using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Testing;
using Cake.Testing.Fixtures;

namespace Cake.Virtualbox.Test.Unit
{
    public class VirtualboxFixture : ToolFixture<VirtualboxSettings>
    {
        #region Private Properties

        private Action<VirtualboxRunner> RunAction { get; }

        #endregion

        #region Override Members

        public VirtualboxFixture(Action<VirtualboxRunner> runAction) 
            : base("vboxmanage.exe")
        {
            this.RunAction = runAction;
        }

        protected override void RunTool()
        {
            var tool = new VirtualboxRunner(this.FileSystem, this.Environment, this.ProcessRunner, this.Tools, new FakeLog());
            this.RunAction?.Invoke(tool);
        }

        #endregion
    }
}
