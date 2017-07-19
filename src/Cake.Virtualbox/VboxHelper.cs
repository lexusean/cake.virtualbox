using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox
{
    public static class VboxHelper
    {
        #region ExistingMachineSettingsFile 

        public static string ExistingMachineSettingsFile(IEnumerable<string> errStrs)
        {
            return errStrs
                .Select(ExistingMachineSettingsFile)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        }

        public static string ExistingMachineSettingsFile(string errStr)
        {
            const string pattern =
                "(?:^vboxmanage\\.exe:.*[\\s]+Machine[\\s]+settings[\\s]+file[\\s]+[']?)([a-zA-Z0-9:\\\\\\/-_.]*)(?:[']?[\\s]+already[\\s]+exists$)";
            var options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            Match match = Regex.Match(errStr, pattern, options);
            if (!match.Success || match.Groups.Count != 2)
                return string.Empty;

            var groups = match.Groups.Cast<Group>().ToArray();
            var file = groups[groups.Length - 1];

            return file == null ? string.Empty : file.Value;
        }

        #endregion
    }
}
