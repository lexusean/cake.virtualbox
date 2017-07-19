using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox.Models
{
    /// <summary>
    /// Model for VM
    /// </summary>
    public class VboxVm
    {
        #region Static Methods

        /// <summary>
        /// Convert to Model based on Vm String
        /// </summary>
        /// <param name="vmStr">vm string from vboxmanage list vms</param>
        /// <param name="getInfoStrAction">function for getting VM Info model</param>
        /// <param name="getHddAction">function for getting VM Disk model</param>
        /// <returns></returns>
        public static VboxVm GetVm(
            string vmStr,
            Func<VboxVm, string> getInfoStrAction = null,
            Func<Guid, VboxHdd> getHddAction = null)
        {
            if (string.IsNullOrWhiteSpace(vmStr))
                return null;

            var newVm = new VboxVm()
            {
                GetInfoStrAction = getInfoStrAction,
                GetHddAction = getHddAction
            };

            newVm.Parse(vmStr);

            return newVm;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// VM Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Uuid String For VM
        /// </summary>
        public string UuidStr { get; set; }

        /// <summary>
        /// Uuid string converted to Guid. Default null
        /// </summary>
        public Guid? Uuid
        {
            get
            {
                Guid o = Guid.Empty;
                if (!Guid.TryParse(this.UuidStr, out o))
                    return null;

                return o;
            }
        }

        private VboxVmInfo _vmInfo = null;

        /// <summary>
        /// VM Info model to contain more detail. Lazy loaded
        /// </summary>
        public VboxVmInfo VmInfo
        {
            get
            {
                if (this._vmInfo != null)
                    return this._vmInfo;

                if (this.GetInfoStrAction != null)
                {
                    var str = this.GetInfoStrAction(this);
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        this._vmInfo = VboxVmInfo.GetVmInfo(this, str, this.GetHddAction);
                    }
                }

                return this._vmInfo;
            }
        }

        #endregion

        #region Internal Properties

        internal Func<VboxVm, string> GetInfoStrAction { get; set; }
        internal Func<Guid, VboxHdd> GetHddAction { get; set; }

        #endregion

        #region Private Methods

        private void Parse(string str)
        {
            const string regex = "^(?:\\\")(.*)(?:\\\")(?:\\s+)(?:\\{)(.*)(?:\\})$";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            Match match = Regex.Match(str, regex, options);
            if (!match.Success || match.Groups.Count != 3)
                return;

            var groups = match.Groups.Cast<Group>().ToArray();
            var first = groups[groups.Length - 2];
            var last = groups[groups.Length - 1];

            this.Name = first == null ? string.Empty : first.Value.Trim();
            this.UuidStr = last == null ? string.Empty : last.Value.Trim();
        }

        #endregion
    }
}
