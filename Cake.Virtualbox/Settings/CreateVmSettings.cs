using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;

namespace Cake.Virtualbox.Settings
{
    public class CreateVmSettings
    {
        #region Custom Types

        public class VboxDiskSetting
        {
            public string FileName { get; set; } = "default.vdi";
            public int Size { get; set; } = 32768;
            public int Port { get; set; } = 0;
            public int Device { get; set; } = 0;
        }

        public class VboxStorageControllerSetting
        {
            public string Name { get; set; } = "default";
            public string Type { get; set; } = "sata";
            public string Controller { get; set; } = "IntelAHCI";

            public List<VboxDiskSetting> DiskSettings { get; private set; } = new List<VboxDiskSetting>();

            public VboxDiskSetting AddDiskSetting(string fileName = "", int? size = null)
            {
                var newSetting = new VboxDiskSetting();
                if (!string.IsNullOrWhiteSpace(fileName))
                    newSetting.FileName = fileName;

                if (size != null && size > 0)
                    newSetting.Size = size.Value;

                if (!this.DiskSettings.Any(t => (t.FileName ?? string.Empty).Equals(newSetting.FileName,
                    StringComparison.InvariantCultureIgnoreCase)))
                {
                    this.DiskSettings.Add(newSetting);
                    return newSetting;
                }

                return null;
            }
        }

        #endregion

        #region Public Properties

        public string VmName { get; set; }
        public string OsType { get; set; } = "Linux";
        public List<VboxStorageControllerSetting> ControllerSettings { get; private set; } = new List<VboxStorageControllerSetting>();

        #endregion

        #region Public Methods

        public VboxStorageControllerSetting AddControllerSetting(
            string name = "", 
            string type = "", 
            string controller = "")
        {
            var newSetting = new VboxStorageControllerSetting();
            if (!string.IsNullOrWhiteSpace(name))
                newSetting.Name = name;

            if (!string.IsNullOrWhiteSpace(type))
                newSetting.Type = type;

            if (!string.IsNullOrWhiteSpace(controller))
                newSetting.Controller = controller;

            if (!this.ControllerSettings.Any(t => (t.Name ?? string.Empty).Equals(newSetting.Name,
                StringComparison.InvariantCultureIgnoreCase)))
            {
                this.ControllerSettings.Add(newSetting);
                return newSetting;
            }

            return null;
        }

        #endregion
    }
}
