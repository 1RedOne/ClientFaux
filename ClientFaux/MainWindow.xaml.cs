using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CERTENROLLLib;
using System.IO;
using System.ComponentModel;
using static CMFaux.CMFauxStatusViewClasses;
using System.Collections.ObjectModel;
using Microsoft.ConfigurationManagement.Messaging.Framework;
using System.Collections.Generic;

namespace CMFaux
{
    // to do : fix discovery 
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();        
        public string Password { get; private set; }
        public string BaseName { get; private set; }
        public string CMServer { get; private set; }
        public string SiteCode { get; private set; }
        public string DomainName { get; private set; }
        public string ExportPath { get; private set; }
        internal ObservableCollection<Device> Devices { get => devices; set => devices = value; }

        private ObservableCollection<Device> devices = new ObservableCollection<Device>();

        private static object _syncLock = new object();

        public List<CustomClientRecord> CustomClientRecords = new List<CustomClientRecord> {
            new CustomClientRecord(){ RecordName="Property1", RecordValue="Value1" },
            new CustomClientRecord(){ RecordName="Property2", RecordValue="Value2" }
        };

        public MainWindow()
        {
            InitializeComponent();
            FilePath.Text = System.IO.Directory.GetCurrentDirectory();
            PasswordBox.Password = "Pa$$w0rd!";
            DomainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            Password = PasswordBox.Password;
            BaseName = NewClientName.Text;
            CMServer = CMServerName.Text;
            SiteCode = CMSiteCode.Text;
            ExportPath = FilePath.Text;
            dgDevices.ItemsSource = Devices;
            BindingOperations.EnableCollectionSynchronization(Devices, _syncLock);            
        }
        
        private void StartBackgroundWork(int thisIndex)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(thisIndex);
        }

        private void CreateClientsButton_Click(object sender, RoutedEventArgs e)
        {            
            DeviceExpander.IsExpanded = true;
            
            string BaseName = NewClientName.Text;
            int CountOfMachines = (Int32.TryParse(NumberOfClients.Text, out int unUsed)) ? Int32.Parse(NumberOfClients.Text) : 1;
            int BeginningWith = (!StartingNumber.Text.Length.Equals(0)) ? Int32.Parse(StartingNumber.Text) : 0;
            for (int i = BeginningWith; i < CountOfMachines; i++)
            {
                string ThisClient = BaseName + i;
                Device ThisDevice = new Device() { Name = ThisClient, Status = "CertCreated", ImageSource = "Images\\step01.png", ProcessProgress = 10};
                Devices.Add(ThisDevice);                

                int thisIndex = devices.IndexOf(ThisDevice);
                StartBackgroundWork(thisIndex);                
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
            var oidlist = new CObjectIds();
            oidlist.Add(oid);
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

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            GetWait();
            string ThisFilePath = System.IO.Directory.GetCurrentDirectory(); 
            Device ThisClient = Devices[(Int32.Parse(e.Argument.ToString()))];
            //Update UI
            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "CreatingCert...");
            X509Certificate2 newCert = CreateSelfSignedCertificate(ThisClient.Name);
            string myPath = ExportCert(newCert, ThisClient.Name, ThisFilePath);
            
            //Update UI
            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "CertCreated!");
            System.Threading.Thread.Sleep(1500);
            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "Starting Inventory...");
            //FauxDeployCMAgent.SimulateClient(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password);
            SmsClientId clientId = FauxDeployCMAgent.RegisterClient(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password);
            
            //Update UI
            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "SendingDiscovery");
            FauxDeployCMAgent.SendDiscovery(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password, clientId);

            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "RequestingPolicy");
            FauxDeployCMAgent.GetPolicy(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password, clientId);

            (sender as BackgroundWorker).ReportProgress(Int32.Parse(e.Argument.ToString()), "SendingCustom");
            FauxDeployCMAgent.SendCustomDiscovery(CMServer, ThisClient.Name, SiteCode, ThisFilePath, CustomClientRecords);
            e.Result = ThisClient;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Device ThisClient = Devices[(Int32.Parse(e.ProgressPercentage.ToString()))];
            string UserState = e.UserState.ToString();
            switch
                (UserState)
            {
                case "CreatingCert...":
                    ThisClient.Status = "Creating Cert...";
                    ThisClient.ProcessProgress = 15;
                    break;
                case "CertCreated":
                    ThisClient.ImageSource = "Images\\step02.png";
                    ThisClient.Status = "Registering...";
                    ThisClient.ProcessProgress = 40;
                    break;
                case "Starting Inventory...":
                    ThisClient.ImageSource = "Images\\step02.png";
                    ThisClient.Status = "Starting Inventory...";
                    ThisClient.ProcessProgress = 50;
                    break;
                case "SendingDiscovery":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Sending Discovery...";
                    ThisClient.ProcessProgress = 75;
                    break;
                case "RequestingPolicy":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Requesting Policy...";
                    ThisClient.ProcessProgress = 85;
                    break;
                default:
                    break;
            }            

        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {            
            Device ThisClient = (Device)e.Result;
            ThisClient.Status = "Complete!";
            ThisClient.ProcessProgress = 100;
            //ThisClient.ImageSource = "Images\\step02.png";
        }
        public string ExportCert(X509Certificate2 newCert, string ThisClient, string FilePath)
        {
            string ExportPath = FilePath + "\\" + ThisClient + ".pfx";
            byte[] certToFile = newCert.Export(X509ContentType.Pfx, PasswordBox.Password);

            File.WriteAllBytes(ExportPath, certToFile); 
            

            return ExportPath;
        }

        private void NewClientName_TextChanged(object sender, TextChangedEventArgs e)
        {
            BaseName = NewClientName.Text;
        }

        private void CMServerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            CMServer= CMServerName.Text;
            if (CMServer.Length.Equals(0))
            {
                CMSettingsStatusLabel.Content = "CM Settings: ❌";
                CreateClientsButton.Background = Brushes.PaleVioletRed;
                CreateClientsButton.IsEnabled = false;
            }
            {
                CMSettingsStatusLabel.Content = "CM Settings: ✔";
                CreateClientsButton.Background = Brushes.LawnGreen;
                CreateClientsButton.IsEnabled = true;
            }
        }

        private void CMSiteCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            SiteCode = CMSiteCode.Text;
        }

        private void FilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            ExportPath = FilePath.Text;
        }
        private void Certificates_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 1;
        }

        private void DeviceNaming_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 2;
        }

        private void SCCMSettings_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 3;
        }

        private void ReadyButton_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 0;
        }

        private void TextBox_OnStartingButtonTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            // Use SelectionStart property to find the caret position.
            // Insert the previewed text into the existing text in the textbox.
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            double val;
            // If parsing is successful, set Handled to false
            e.Handled = !double.TryParse(fullText, out val);
        }

        private void TextBox_OnEndingButtonTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            // Use SelectionStart property to find the caret position.
            // Insert the previewed text into the existing text in the textbox.
            var fullText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            double val;
            // If parsing is successful, set Handled to false
            e.Handled = !double.TryParse(fullText, out val);
        }
        private void textChangedEventHandler(object sender, TextChangedEventArgs args)
        {
            if (StartingNumber.Text.Length.Equals(0) || EndingNumber.Text.Length.Equals(0))
            {
                return;
            }
            int StartingNo = Int32.Parse(StartingNumber.Text);
            int EndingNo = Int32.Parse(EndingNumber.Text);
            NumberOfClients.Text = (Math.Abs(
                (EndingNo - StartingNo))).ToString();
            Console.WriteLine("Generating " + NumberOfClients.Text + " new clients");
        }

        private void PWChanged(object sender, RoutedEventArgs args)
        {
            Password = PasswordBox.Password;
        }

        private void GetWait()
        {
            Random random = new Random();
            int w = random.Next(3, 15);
            System.Threading.Thread.Sleep(100 * w);
        }
    }
}
