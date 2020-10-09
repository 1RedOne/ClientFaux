using Microsoft.ConfigurationManagement.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace CMFaux
{
    public partial class CMFauxStatusViewClasses
    {
        public static List<String> GetWMIClasses()
        {
            return new List<string> { "Win32_ComputerSystem", "Win32_OperatingSystem", "Win32_BIOS", "Win32_SystemEnclosure","Win32_NetworkAdapter",
                    "Win32_NetworkAdapterConfiguration", "Win32_DiskDrive","Win32_DiskPartition","Win32_Service", "Win32Reg_AddRemovePrograms","CCM_LogicalMemoryConfiguration",
                    "Win32_POTSModem","Win32_DesktopMonitor","Win32_TSLicenseKeyPack","Win32_PhysicalMemory","Win32_ServerFeature","Win32_ParallelPort","Win32Reg_SMSGuestVirtualMachine64",
                    "Win32_USBController","Office365ProPlusConfigurations","Win32_NetworkClient","Win32Reg_SMSWindowsUpdate","Win32_MotherboardDevice","Win32_TSIssuedLicense","Win32_SoundDevice",
                    "Win32Reg_SMSGuestVirtualMachine","Win32Reg_SMSAdvancedClientSSLConfiguration","Win32_IDEController","Win32_VideoController","Win32_SCSIController","Win32_TapeDrive"
                    /*"Win32_LogicalDisk",*//*"Win32_Processor",*//*"Win32_SystemDevices",*//*"Win32_Product","Win32_PnpEntity"*/
                };
        }

        public static string GetOSRealVersionInfo()
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(
                Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.System),
                    "kernel32.dll"));
            return fvi.ProductVersion;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // used to rewrite the OS property reported to CM
        [XmlRoot("CCM_DiscoveryData")] // Must define an XmlRoot that represents the class name
        public sealed class CMFauxStatusViewClassesFixedOSRecord : InventoryInstanceElement
        {
            [XmlElement]
            public string PlatformId { get; set; }

            protected override void DiscoverSelf()
            {
                base.DiscoverSelf();
            }

            public CMFauxStatusViewClassesFixedOSRecord()
                : base(
                    "CCM_DiscoveryData", // associated base class
                    "CCM_DiscoveryData", // associated parent class
                    null /* namespace (scope) */)
            {
            }
        }
    }
}