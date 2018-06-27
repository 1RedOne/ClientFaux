using System;
using System.IO;
using Microsoft.ConfigurationManagement.Messaging.Framework;
using Microsoft.ConfigurationManagement.Messaging.Messages;
using Microsoft.ConfigurationManagement.Messaging.Sender.Http;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

namespace SimulateClient

{

    class Program

    {
        private static readonly HttpSender Sender = new HttpSender();
        private static string clientName;
        private static string CertPath;
        private static string pass;
        private static string SiteCode;
        private static string MPHostName;

        private static void Initialize()
        {
            Sender.OnSend += OnSend;
            Sender.OnReceived += OnReceived;

        }

        private static void Help()
        {
            Console.WriteLine("{0} <ClientName> <certPW> <SiteCode> <HostName> [-v]", Environment.GetCommandLineArgs()[0]);
            Console.WriteLine("-v: verbose");
            Environment.Exit(1);
        }

        static void Main(string[] args)

        {
            if (args.Length < 3)
            {
                Help();
            }

            clientName = args[0];
            CertPath = args[1];
            pass = args[2];
            SiteCode = args[3];
            MPHostName = args[4];

            // Creates the text file that the trace listener will write to. @"FolderIcon\Folder.ico"
            var outPutDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var Tick = DateTime.Now.Ticks.GetHashCode().ToString("x").ToUpper();
            var logPath = outPutDirectory + "\\ClientFaux_" + Tick + ".txt";

            if (File.Exists(logPath))
            {
                logPath = outPutDirectory + "\\ClientFaux_" + Tick + "_1.txt";
                Console.WriteLine("Logging as " + logPath);
            }
            Console.WriteLine("Logging to: " + logPath);
            System.IO.FileStream myTraceLog = new System.IO.FileStream(logPath, System.IO.FileMode.OpenOrCreate);
            
            // Creates the new trace listener.
            System.Diagnostics.TextWriterTraceListener myListener = new System.Diagnostics.TextWriterTraceListener(myTraceLog);
            Trace.Listeners.Add(myListener);

            //Get the domain name and cert path
            string DomainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            string CMServerName = MPHostName +"." + DomainName;
            //string CertPath = outPutDirectory + "\\MixedModeTestCert.pfx";
            
            //check for a cert file, if we don't see it, give up
            if (File.Exists(CertPath))
            {
                Console.WriteLine("Found cert file at " + CertPath);
            }
            else
            {
                Console.WriteLine("Could not find a cert PFX file at " + CertPath);
                return;
            }


            String machineName = System.Environment.MachineName;
            Console.WriteLine("Running on system[" + machineName + "], registering as [" + clientName + "]");

            //string ClientName = machineName;
            SimulateClient(CMServerName, clientName, DomainName, SiteCode);

        }

        private static void OnReceived(object sender, MessageSenderEventArgs e)
        {
            

            Console.WriteLine(@"Received payload from MP:");
            Console.WriteLine(e.Message.Body.Payload);
        }

        /// <summary>
        /// Captures data sent by HTTP sender and logs if verbose is enabled
        /// </summary>
        private static void OnSend(object sender, MessageSenderEventArgs e)
        {
            

            Console.WriteLine(@"Sending payload to MP:");
            Console.WriteLine(e.Message.Body.Payload);

            if (e.Message.Attachments.Count > 0)
            {
                if (e.Message is ConfigMgrFileCollectionMessage ||
                    e.Message is ConfigMgrUploadRequest)
                {
                    Console.WriteLine(@"Not writing attachments for {0} message to console", e.Message);
                    return;
                }

                for (int i = 0; i < e.Message.Attachments.Count; i++)
                {
                    Console.WriteLine(@"+======== ATTACHMENT #{0} (name: {1})========+", i + 1, e.Message.Attachments[i].Name);
                    Console.WriteLine(e.Message.Attachments[i].Body.Payload);
                }
            }
        }

        static void SimulateClient(string CMServerName, string ClientName, string DomainName, string SiteCode)
        {
            //HttpSender sender = new HttpSender();

            // Load the certificate for client authentication
            //Password for excerpted cert
            using (MessageCertificateX509Volatile certificate = new MessageCertificateX509Volatile(CertPath, pass))

            {
                X509Certificate2 thisCert = new X509Certificate2(CertPath, pass);

                                
                Console.WriteLine(@"Using certificate for client authentication with thumbprint of '{0}'", certificate.Thumbprint);
                Console.WriteLine("Signature Algorithm: " + thisCert.SignatureAlgorithm.FriendlyName);

                if (thisCert.SignatureAlgorithm.FriendlyName == "sha256RSA")
                {
                    Console.WriteLine("Cert has a valid sha256RSA Signature Algorithm, proceeding");

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ConfigMgr requires a Sha256 Cert, try recreating cert with:");
                    string multiline = @"
    New-SelfSignedCertificate `
    -KeyLength 2048 -HashAlgorithm ""SHA256"" 
    - Provider  ""Microsoft Enhanced RSA and AES Cryptographic Provider"" 
    -KeyExportPolicy Exportable - KeySpec KeyExchange `
    -Subject ""SCCM Test Certificate"" - KeyUsageProperty All - Verbose
";

                    Console.Write(multiline);
                    return;
                }
                
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
                catch(Exception ex)
                {
                    Console.WriteLine("Failed to enroll with an error");
                    Console.WriteLine(ex.Message);
                    return;

                }
                SmsClientId clientId = testclientId; 
                Console.WriteLine(@"Got SMSID from registration of: {0}", clientId);

                // Send data to the site
                ConfigMgrDataDiscoveryRecordMessage ddrMessage = new ConfigMgrDataDiscoveryRecordMessage();


                // Add necessary discovery data
                ddrMessage.SmsId = clientId;
                ddrMessage.ADSiteName = "Default-First-Site-Name"; //Changed from 'My-AD-SiteName
                ddrMessage.SiteCode = SiteCode;
                ddrMessage.DomainName = DomainName;
                ddrMessage.NetBiosName = ClientName;
                Console.WriteLine("ddrSettings clientID: " + clientId);
                Console.WriteLine("ddrSettings SiteCode: " + ddrMessage.SiteCode);
                Console.WriteLine("ddrSettings ADSiteNa: " + ddrMessage.ADSiteName);
                Console.WriteLine("ddrSettings DomainNa: " + ddrMessage.DomainName);
                Console.WriteLine("ddrSettings FakeName: " + ddrMessage.NetBiosName);
                Console.WriteLine("Message MPHostName  : " + CMServerName);

                // Now create inventory records from the discovered data (optional)
                ddrMessage.Discover();
                
                // Add our certificate for message signing
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing);
                ddrMessage.AddCertificateToMessage(certificate, CertificatePurposes.Encryption);
                ddrMessage.Settings.HostName = CMServerName;
                // Now send the message to the MP (it's asynchronous so there won't be a reply)
                ddrMessage.SendMessage(Sender);

                //todo add as a param 
                /*
                ConfigMgrHardwareInventoryMessage hinvMessage = new ConfigMgrHardwareInventoryMessage();
                hinvMessage.Settings.HostName = CMServerName;
                hinvMessage.AddCertificateToMessage(certificate, CertificatePurposes.Signing | CertificatePurposes.Encryption);
                hinvMessage.SmsId = clientId;
                hinvMessage.SiteCode = SiteCode;
                hinvMessage.NetBiosName = ClientName;
                hinvMessage.DomainName = DomainName;
                hinvMessage.Settings.Compression = MessageCompression.Zlib;
                hinvMessage.Settings.Security.EncryptMessage = true;
                hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2", "Win32_LogicalDisk", @"root\cimv2\sms", "SMS_LogicalDisk"));
                hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2", "Win32_Processor", @"root\cimv2\sms", "SMS_Processor"));
                hinvMessage.AddInstancesToInventory(WmiClassToInventoryReportInstance.WmiClassToInventoryInstances(@"root\cimv2", "Win32_SystemDevices", @"root\cimv2\sms", "SMS_SystemDevices"));
                hinvMessage.SendMessage(Sender);
                Console.WriteLine("Sending: " + hinvMessage.HardwareInventoryInstances.Count + "instances of HWinv data to CM");*/
            }
        }
    }
}


