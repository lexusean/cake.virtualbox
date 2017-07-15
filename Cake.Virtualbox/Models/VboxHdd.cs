using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox.Models
{
    public class VboxHdd
    {
        #region Static Methods

        public static VboxHdd GetHdd(string hddBlockStr)
        {
            var newHdd = new VboxHdd();
            newHdd.Parse(hddBlockStr);

            return newHdd;
        }

        #endregion

        #region Public Properties

        public string UuidStr { get; set; }
        public string ParentUuidStr { get; set; }
        public string LocationStr { get; set; }

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

        public Guid? ParentUuid
        {
            get
            {
                Guid o = Guid.Empty;
                if (!Guid.TryParse(this.ParentUuidStr, out o))
                    return null;

                return o;
            }
        }

        public FileInfo Location
        {
            get
            {
                return new FileInfo(this.LocationStr);
            }
        }

        public bool IsBase
        {
            get { return this.ParentUuid == null; }
        }

        public bool HasParent
        {
            get { return this.ParentUuid != null; }
        }

        #endregion

        #region Private Methods

        private void Parse(string blockStr)
        {
            this.ParseUuid(blockStr);
            this.ParseParentUuid(blockStr);
            this.ParseLocation(blockStr);
        }

        private void ParseUuid(string blockStr)
        {
            const string regex = @"(?:^UUID\:[\s]+)(.*)(?:$)";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            Match match = Regex.Match(blockStr, regex, options);
            if (!match.Success)
                return;

            var last = match.Groups.Cast<Group>().Last();
            this.UuidStr = last == null ? string.Empty : last.Value.Trim();
        }

        private void ParseParentUuid(string blockStr)
        {
            const string regex = @"(?:^Parent UUID\:[\s]+)(.*)(?:$)";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            Match match = Regex.Match(blockStr, regex, options);
            if (!match.Success)
                return;

            var last = match.Groups.Cast<Group>().Last();
            this.ParentUuidStr = last == null ? string.Empty : last.Value.Trim();
        }

        private void ParseLocation(string blockStr)
        {
            const string regex = @"(?:^Location\:[\s]+)(.*)(?:$)";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            Match match = Regex.Match(blockStr, regex, options);
            if (!match.Success)
                return;

            var last = match.Groups.Cast<Group>().Last();
            this.LocationStr = last == null ? string.Empty : last.Value.Trim();
        }

        #endregion
    }
}
