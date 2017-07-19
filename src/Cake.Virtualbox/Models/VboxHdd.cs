using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cake.Core.IO;

namespace Cake.Virtualbox.Models
{ 
    /// <summary>
    /// Model for Vboxmanage Disk Info 
    /// </summary>
    public class VboxHdd
    {
        #region Static Methods

        /// <summary>
        /// Creates Model based on string
        /// </summary>
        /// <param name="hddBlockStr">Disk info block</param>
        /// <returns>Disk model</returns>
        public static VboxHdd GetHdd(string hddBlockStr)
        {
            var newHdd = new VboxHdd();
            newHdd.Parse(hddBlockStr);

            return newHdd;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Uuid String
        /// </summary>
        public string UuidStr { get; set; }

        /// <summary>
        /// Parent Disk Uuid String
        /// </summary>
        public string ParentUuidStr { get; set; }

        /// <summary>
        /// Location String
        /// </summary>
        public string LocationStr { get; set; }

        /// <summary>
        /// UuidStr converted to Guid. Default is null
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

        /// <summary>
        /// ParentUuidStr converted to Guid. Default is null
        /// </summary>
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

        /// <summary>
        /// LocationStr as FilePath 
        /// </summary>
        public FilePath Location
        {
            get
            {
                return new FilePath(this.LocationStr);
            }
        }

        /// <summary>
        /// Is a base drive
        /// </summary>
        public bool IsBase
        {
            get { return this.ParentUuid == null; }
        }

        /// <summary>
        /// Has a parent drive linked clone
        /// </summary>
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
