using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class VboxHdd
    {
        public static VboxHdd GetHdd(string hddBlockStr)
        {
            var newHdd = new VboxHdd();
            newHdd.Parse(hddBlockStr);

            return newHdd;
        }

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
    }

    public class Vboxmange
    {
        public IEnumerable<VboxHdd> GetTipHdds(string hddList, string boxFilter = "")
        {
            const string boxFilterRegex = @"(?={0}(([_]+[0-9]+)+|[\/\\]+)).*";
            const string hddsBlockRegex =
                @"^(UUID:)[\s]+(?:(.(?!(ocation|(arent UUID\:[\s]+base))))|\n)*(Location:)[\s]+.*";
            var hddsBlockOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            var blockRegex = new StringBuilder();
            blockRegex.Append(hddsBlockRegex);

            if (!string.IsNullOrWhiteSpace(boxFilter))
            {
                var filter = string.Format(boxFilterRegex, boxFilter);
                blockRegex.Append(filter);
            }

            blockRegex.Append("$");

            var matches = Regex.Matches(hddList, blockRegex.ToString(), hddsBlockOptions);
            if(matches.Count <= 0)
                yield break;

            foreach (Match match in matches)
            {
                yield return VboxHdd.GetHdd(match.Value);
            }
        }
    }
}
