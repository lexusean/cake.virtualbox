using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox.Models
{
    public class VboxVm
    {
        public static VboxVm GetVm(string vmStr)
        {
            var newVm = new VboxVm();
            newVm.Parse(vmStr);

            return newVm;
        }

        public string Name { get; set; }
        public string UuidStr { get; set; }

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
    }
}
