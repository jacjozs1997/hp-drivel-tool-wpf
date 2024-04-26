using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HP_Driver_Tool.Models
{
    internal static class OperatingSystemFunctions
    {
        public static string GetName(this OSVersionExtension.OperatingSystem type)
        {
            switch (type)
            {
                case OSVersionExtension.OperatingSystem.Windows2000:
                    return "Windows 2000";
                case OSVersionExtension.OperatingSystem.WindowsVista:
                    return "Windows Vista";
                case OSVersionExtension.OperatingSystem.Windows7:
                    return "Windows 7";
                case OSVersionExtension.OperatingSystem.Windows8:
                    return "Windows 8";
                case OSVersionExtension.OperatingSystem.Windows81:
                    return "Windows 8.1";
                case OSVersionExtension.OperatingSystem.Windows10:
                    return "Windows 10";
                case OSVersionExtension.OperatingSystem.Windows11:
                    return "Windows 11";
            }
            return null;
        }
    }
}
