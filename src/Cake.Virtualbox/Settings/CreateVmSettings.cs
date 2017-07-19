using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core.IO;

namespace Cake.Virtualbox.Settings
{
    /// <summary>
    /// Settings Object for CreateVm operation
    /// </summary>
    public class CreateVmSettings
    {
        #region Custom Types

        /// <summary>
        /// Disk Setting
        /// </summary>
        public class VboxDiskSetting
        {
            /// <summary>
            /// File name for disk
            /// </summary>
            public string FileName { get; set; } = "default.vdi";

            /// <summary>
            /// Size in MB
            /// </summary>
            public int Size { get; set; } = 32768;

            /// <summary>
            /// Controller Port
            /// </summary>
            public int Port { get; set; } = 0;

            /// <summary>
            /// Controller Device
            /// </summary>
            public int Device { get; set; } = 0;
        }

        /// <summary>
        /// VM Controller Setting
        /// </summary>
        public class VboxStorageControllerSetting
        {
            /// <summary>
            /// Controller Name
            /// </summary>
            public string Name { get; set; } = "default";

            /// <summary>
            /// Storage Controller Type (sata/ide) --add sata
            /// </summary>
            public string Type { get; set; } = "sata";

            /// <summary>
            /// Controller type --controller IntelAHCI
            /// </summary>
            public string Controller { get; set; } = "IntelAHCI";

            /// <summary>
            /// Disk Settings for controller
            /// </summary>
            public List<VboxDiskSetting> DiskSettings { get; private set; } = new List<VboxDiskSetting>();

            /// <summary>
            /// Adds a disk to the controller setting
            /// </summary>
            /// <param name="fileName">disk file name</param>
            /// <param name="size">disk file size in MB</param>
            /// <returns>Added disk setting</returns>
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

        /// <summary>
        /// VM Name
        /// </summary>
        public string VmName { get; set; }

        /// <summary>
        /// OS Type
        /// </summary>
        public string OsType { get; set; } = "Linux";

        /// <summary>
        /// List of Controller settings
        /// </summary>
        public List<VboxStorageControllerSetting> ControllerSettings { get; private set; } = new List<VboxStorageControllerSetting>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds controller to CreateVm settings
        /// </summary>
        /// <param name="name">Controller name</param>
        /// <param name="type">Storage Controller Type</param>
        /// <param name="controller">Controller Type</param>
        /// <returns>Added Controller Setting</returns>
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
