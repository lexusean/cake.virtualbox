using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cake.Virtualbox
{
    /// <summary>
    /// Helper class for vboxmange stuff
    /// </summary>
    public static class VboxHelper
    {
        #region ExistingMachineSettingsFile 

        /// <summary>
        /// Checks for first occurence of disk file in list of error strings
        /// </summary>
        /// <param name="errStrs">list of error strings</param>
        /// <returns>File location string</returns>
        public static string ExistingMachineSettingsFile(IEnumerable<string> errStrs)
        {
            return errStrs
                .Select(ExistingMachineSettingsFile)
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));
        }

        /// <summary>
        /// Checks error string for disk file
        /// </summary>
        /// <param name="errStr">Usually in the format of 'vboxmanage.exe: Machine settings file: 'file.vdi' already exists'</param>
        /// <returns>File location string</returns>
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

            return file?.Value ?? string.Empty;
        }

        #endregion
    }
}
