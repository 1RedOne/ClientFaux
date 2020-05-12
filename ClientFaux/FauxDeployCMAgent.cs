using Microsoft.ConfigurationManagement.Messaging.Framework;
using Microsoft.ConfigurationManagement.Messaging.Messages;
using Microsoft.ConfigurationManagement.Messaging.Sender.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using static CMFaux.CMFauxStatusViewClasses;
using Microsoft.ConfigurationManagement.Messaging.Messages.Server;
using System.IO;
using CERTENROLLLib;
using System.Collections.ObjectModel;
using System.Management;
using System.Security;
using System.Xml;

namespace CMFaux
{
    class FauxDeployCMAgent
    {
        private static readonly HttpSender Sender = new HttpSender();
        
        public static SmsClientId RegisterClient(string CMServerName, string ClientName, string DomainName, string CertPath, SecureString pass) {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))
            {
                // Create a registration request
                ConfigMgrRegistrationRequest registrationRequest = new ConfigMgrRegistrationRequest();

                // Add our certificate for message signing
                registrationRequest.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                // Set the destination hostname
                registrationRequest.Settings.HostName = CMServerName;

                Console.WriteLine("Trying to reach: " + CMServerName);

                // Discover local properties for registration metadata
                registrationRequest.Discover();
                registrationRequest.AgentIdentity = "MyCustomClient";
                registrationRequest.ClientFqdn = ClientName + "." + DomainName;
                registrationRequest.NetBiosName = ClientName;
                //registrationRequest.Settings.SecurityMode = MessageSecurityMode.HttpsMode;
                //registrationRequest.HardwareId = Guid.NewGuid().ToString();
                Console.WriteLine("About to try to register " + registrationRequest.ClientFqdn);

                // Register client and wait for a confirmation with the SMSID

                //registrationRequest.Settings.Security.AuthenticationType = AuthenticationType.WindowsAuth;

                registrationRequest.Settings.Compression = MessageCompression.Zlib;
                registrationRequest.Settings.ReplyCompression = MessageCompression.Zlib;

                SmsClientId testclientId = new SmsClientId();
                try
                {
                    testclientId = registrationRequest.RegisterClient(Sender, TimeSpan.FromMinutes(5));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to enroll with an error");
                    Console.WriteLine(ex.Message);
                    System.Windows.MessageBox.Show(" We failed with " + ex.Message);
                    throw;
                }
                SmsClientId clientId = testclientId;
                Console.WriteLine(@"Got SMSID from registration of: {0}", clientId);
                return clientId;
            }
        }

        
        public static void SendDiscovery(string CMServerName, string clientName, string domainName, string SiteCode,
            string CertPath, SecureString pass, SmsClientId clientId, bool enumerateAndAddCustomDdr = false)
        {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                //X509Certificate2 thisCert = new X509Certificate2(CertPath, pass);

                Console.WriteLine(@"Got SMSID from registration of: {0}", clientId);

                // create base DDR Message
                ConfigMgrDataDiscoveryRecordMessage ddrMessage = new ConfigMgrDataDiscoveryRecordMessage
                {
                    // Add necessary discovery data
                    SmsId = clientId,
                    ADSiteName = "Default-First-Site-Name", //Changed from 'My-AD-SiteName
                    SiteCode = SiteCode,
                    DomainName = domainName,
                    NetBiosName = clientName
                };


                
                ddrMessage.Discover();                
                // Add our certificate for message signing
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing);
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Encryption);
                ddrMessage.Settings.HostName = CMServerName;
                ddrMessage.Settings.Compression = MessageCompression.Zlib;
                ddrMessage.Settings.ReplyCompression = MessageCompression.Zlib;
                Debug.WriteLine("Sending [" + ddrMessage.DdrInstances.Count + "] instances of Discovery data to CM");
                if (enumerateAndAddCustomDdr)
                {
                    //see current value for the DDR message
                    var OSSetting = ddrMessage.DdrInstances.OfType<InventoryInstance>().Where(m => m.Class == "CCM_DiscoveryData");

                    ////retrieve actual setting
                    string osCaption = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                                        select x.GetPropertyValue("Caption")).FirstOrDefault().ToString();

                    XmlDocument xmlDoc = new XmlDocument();

                    ////retrieve reported value
                    xmlDoc.LoadXml(ddrMessage.DdrInstances.OfType<InventoryInstance>().FirstOrDefault(m => m.Class == "CCM_DiscoveryData")?.InstanceDataXml.ToString());

                    ////Set OS to correct setting
                    xmlDoc.SelectSingleNode("/CCM_DiscoveryData/PlatformID").InnerText = "Microsoft Windows NT Server 10.0";

                    ////Remove the instance
                    ddrMessage.DdrInstances.Remove(ddrMessage.DdrInstances.OfType<InventoryInstance>().FirstOrDefault(m => m.Class == "CCM_DiscoveryData"));

                    CMFauxStatusViewClassesFixedOSRecord FixedOSRecord = new CMFauxStatusViewClassesFixedOSRecord
                    {
                        PlatformId = osCaption
                    };
                    InventoryInstance instance = new InventoryInstance(FixedOSRecord);

                    ////Add new instance
                    ddrMessage.DdrInstances.Add(instance);
                }

                //foreach (InventoryReportBodyElement Record in ddrMessage.DdrInstances)
                //{                 
                //    //Debug.WriteLine(Record.ToString());
                //}
                //ddrMessage.DdrInstances.Count
                // Now send the message to the MP (it's asynchronous so there won't be a reply)
                ddrMessage.SendMessage(Sender);

                ConfigMgrHardwareInventoryMessage hinvMessage = new ConfigMgrHardwareInventoryMessage();
                hinvMessage.Settings.HostName = CMServerName;
                hinvMessage.SmsId = clientId;
                hinvMessage.Settings.Compression = MessageCompression.Zlib;
                hinvMessage.Settings.ReplyCompression = MessageCompression.Zlib;
                //hinvMessage.Settings.Security.EncryptMessage = true;
                hinvMessage.Discover();

                var Classes = CMFauxStatusViewClasses.GetWMIClasses();                
                foreach (string Class in Classes)
                {

                    //Console.WriteLine($"---Adding class : [{Class}]");
                    try { hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2", Class)); }
                    catch { Console.WriteLine($"!!!Adding class : [{Class}] :( not found on this system"); }
                }

                var SMSClasses = new List<string> { "SMS_Processor", "CCM_System", "SMS_LogicalDisk" };
                foreach (string Class in SMSClasses)
                {

                    Console.WriteLine($"---Adding class : [{Class}]");
                    try { hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2\sms", Class)); }
                    catch { Console.WriteLine($"!!!Adding class : [{Class}] :( not found on this system"); }
                }                

                hinvMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                hinvMessage.Validate(Sender);
                hinvMessage.SendMessage(Sender);
            };
        }
        public static void SendCustomDiscovery(string CMServerName, string ClientName, string SiteCode, string FilePath, ObservableCollection<CustomClientRecord> customClientRecords)
        {
            string ddmLocal = FilePath + "\\DDRS\\" + ClientName;
            string CMddmInbox = "\\\\" + CMServerName + "\\SMS_" + SiteCode + "\\inboxes\\ddm.box\\" + ClientName + ".DDR";

            DiscoveryDataRecordFile ddrF = new DiscoveryDataRecordFile("ClientFaux")
            {
                SiteCode = SiteCode, Architecture = "System"
            };
            ddrF.AddStringProperty("Name", DdmDiscoveryFlags.Key, 32, ClientName);
            ddrF.AddStringProperty("Netbios Name", DdmDiscoveryFlags.Name, 16, ClientName);

            foreach (CustomClientRecord Record in customClientRecords)
            {
                ddrF.AddStringProperty(Record.RecordName, DdmDiscoveryFlags.None, 32, Record.RecordValue);
            }
            
            System.IO.Directory.CreateDirectory(ddmLocal);
            DirectoryInfo di = new DirectoryInfo(ddmLocal);
            ddrF.SerializeToFile(ddmLocal);

            FileInfo file = di.GetFiles().FirstOrDefault();
            
            File.Copy(file.FullName, CMddmInbox, true);
            System.IO.Directory.Delete(ddmLocal, true);

        }
        public static void GetPolicy(string CMServerName, string SiteCode, string CertPath, SecureString pass, SmsClientId clientId)
        {
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                bool replyCompression = true;
                bool compression = true;
                ConfigMgrPolicyAssignmentRequest userPolicyMessage = new ConfigMgrPolicyAssignmentRequest()
                {
                    ResourceType = PolicyAssignmentResourceType.User,
                    SmsId = clientId,
                    SiteCode = SiteCode

                };
                 userPolicyMessage.Settings.HostName = CMServerName;
                 userPolicyMessage.Settings.Security.AuthenticationScheme = AuthenticationScheme.Ntlm;
                userPolicyMessage.Settings.Security.AuthenticationType = AuthenticationType.WindowsAuth;
                
                userPolicyMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                
                userPolicyMessage.Discover();
                userPolicyMessage.Settings.Security.EncryptMessage = true;
                userPolicyMessage.Settings.ReplyCompression = (true == replyCompression) ? MessageCompression.Zlib : MessageCompression.None;
                userPolicyMessage.Settings.Compression = (true == compression) ? MessageCompression.Zlib : MessageCompression.None;
                userPolicyMessage.SendMessage(Sender);

                ConfigMgrPolicyAssignmentRequest machinePolicyMessage = new ConfigMgrPolicyAssignmentRequest();
                machinePolicyMessage.Settings.HostName = CMServerName;
                machinePolicyMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                machinePolicyMessage.Settings.Security.EncryptMessage = true;
                machinePolicyMessage.Settings.Compression = (true == compression) ? MessageCompression.Zlib : MessageCompression.None;
                machinePolicyMessage.Settings.ReplyCompression = (true == replyCompression) ? MessageCompression.Zlib : MessageCompression.None;
                machinePolicyMessage.ResourceType = PolicyAssignmentResourceType.Machine;
                machinePolicyMessage.SmsId = clientId;
                machinePolicyMessage.SiteCode = SiteCode;
                machinePolicyMessage.Discover();
                //machinePolicyMessage.SendMessage(Sender);
            }

        }
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
        {
            // create DN for subject and issuer
            var dn = new CX500DistinguishedName();
            dn.Encode("CN=" + subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);
            // create a new private key for the certificate
            CX509PrivateKey privateKey = new CX509PrivateKey
            {
                ProviderName = "Microsoft Enhanced RSA and AES Cryptographic Provider",
                MachineContext = false,
                Length = 2048,
                KeySpec = X509KeySpec.XCN_AT_SIGNATURE, // use is not limited
                ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG
            };
            privateKey.Create();

            // Use the stronger SHA512 hashing algorithm
            var hashobj = new CObjectId();
            hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
                ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
                AlgorithmFlags.AlgorithmFlagsNone, "SHA256");

            // add extended key usage if you want - look at MSDN for a list of possible OIDs
            var oid = new CObjectId();
            oid.InitializeFromValue("1.3.6.1.5.5.7.3.1"); // SSL server
            var oidlist = new CObjectIds {oid};
            var eku = new CX509ExtensionEnhancedKeyUsage();
            eku.InitializeEncode(oidlist);

            // Create the self signing request
            var cert = new CX509CertificateRequestCertificate();
            cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser, privateKey, "");
            cert.Subject = dn;
            cert.Issuer = dn; // the issuer and the subject are the same
            cert.NotBefore = DateTime.Now;
            // this cert expires immediately. Change to whatever makes sense for you
            cert.NotAfter = DateTime.Now.AddYears(1);
            cert.X509Extensions.Add((CX509Extension)eku); // add the EKU
            cert.HashAlgorithm = hashobj; // Specify the hashing algorithm
            cert.Encode(); // encode the certificate

            // Do the final enrollment process
            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(cert); // load the certificate
            enroll.CertificateFriendlyName = subjectName; // Optional: add a friendly name
            string csr = enroll.CreateRequest(); // Output the request in base64
                                                 // and install it back as the response
            enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate,
                csr, EncodingType.XCN_CRYPT_STRING_BASE64, ""); // no password
                                                                // output a base64 encoded PKCS#12 so we can import it back to the .Net security classes
            var base64encoded = enroll.CreatePFX("", // no password, this is for internal consumption
                PFXExportOptions.PFXExportChainWithRoot);

            // instantiate the target class with the PKCS#12 data (and the empty password)
            return new System.Security.Cryptography.X509Certificates.X509Certificate2(
                System.Convert.FromBase64String(base64encoded), "",
                // mark the private key as exportable (this is usually what you want to do)
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable
            );
        }
    }   
}

