using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox.Models
{
    public class VboxVmInfo
    {
        #region Static Methods

        public static VboxVmInfo GetVmInfo(
            VboxVm vm, 
            string vmStr, 
            Func<Guid, VboxHdd> getHddInfoAction = null)
        {
            var newVm = new VboxVmInfo(vm)
            {
                GetHddInfoAction = getHddInfoAction
            };

            newVm.Parse(vmStr);

            return newVm;
        }

        #endregion

        #region Public Properties

        public VboxVm Vm { get; }
        public List<Guid> HddIds { get; private set; } = new List<Guid>();

        private List<VboxHdd> _disks = null;
        public List<VboxHdd> Disks
        {
            get
            {
                if (this._disks != null)
                    return this._disks;

                if (this.GetHddInfoAction != null &&
                    this.HddIds != null &&
                    this.HddIds.Any())
                {
                    this._disks = this.HddIds
                        .Select(t => this.GetHddInfoAction(t))
                        .Where(t => t != null)
                        .ToList();
                }

                return this._disks ?? Enumerable.Empty<VboxHdd>().ToList();
            }
        }

        #endregion

        #region Internal Properties
        
        internal Func<Guid, VboxHdd> GetHddInfoAction { get; set; }

        #endregion

        #region Ctor

        internal VboxVmInfo(VboxVm vm)
        {
            this.Vm = vm ?? throw new ArgumentNullException(nameof(vm), "Info object requires parent vm object");
        }

        #endregion

        #region Private Methods

        private void Parse(string str)
        {
            this.ParseHdds(str);
        }

        private void ParseHdds(string str)
        {
            const string regex = @"(?:^.+[\s]*\([0-9]+[,\s]+[0-9]+\)\:[\s]*.*UUID:[\s]+)([a-zA-Z0-9\-]+)(?:\)[\s]*$)";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            var matches = Regex.Matches(str, regex, options);
            foreach (Match match in matches)
            {
                if (!match.Success || match.Groups.Count != 2)
                    return;

                var groups = match.Groups.Cast<Group>().ToArray();
                var uuidMatch = groups[groups.Length - 1];

                Guid uuid = Guid.Empty;
                if (uuidMatch != null)
                    Guid.TryParse(uuidMatch.Value.Trim(), out uuid);

                if (uuid == Guid.Empty)
                    return;

                this.HddIds.Add(uuid);
            }
        }

        #endregion
    }
}
