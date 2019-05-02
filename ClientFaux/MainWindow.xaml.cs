using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;
using static CMFaux.CMFauxStatusViewClasses;
using System.Collections.ObjectModel;
using Microsoft.ConfigurationManagement.Messaging.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMFaux
{

    // to do : fix discovery 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly BackgroundWorker worker = new BackgroundWorker();        
        public string Password { get; private set; }
        public string BaseName { get; private set; }
        public string CMServer { get; private set; }
        public string SiteCode { get; private set; }
        public string DomainName { get; private set; }
        public string ExportPath { get; private set; }
        public int maxThreads { get; private set; }
        private int _idCounter;
        public int IdCounter
        {
            get { return _idCounter; }
            set
            {
                if (value != _idCounter)
                {
                    _idCounter = value;
                    OnPropertyChanged("IdCounter");
                }
            }
        }

        private int _DeviceCounter { get; set; }
        public int DeviceCounter
        {
            get { return _DeviceCounter; }
            set
            {
                if (value != _DeviceCounter)
                {
                    _DeviceCounter = value;
                    OnPropertyChanged("DeviceCounter");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
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
            MaximumThreads.Text = "7";
            DomainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            Password = PasswordBox.Password;
            BaseName = NewClientName.Text;
            CMServer = CMServerName.Text;
            SiteCode = CMSiteCode.Text;
            ExportPath = FilePath.Text;
            maxThreads = int.Parse(MaximumThreads.Text);
            dgDevices.ItemsSource = Devices;
            BindingOperations.EnableCollectionSynchronization(Devices, _syncLock);
            Counter.SetBinding(ContentProperty, new Binding("IdCounter"));
            DataContext = this;
            DeviceCounter = 0;
        }
        private void FireProgress(int thisIndex, string statusMessage)
        {
            Device ThisClient = Devices[thisIndex];
            
            switch
                (statusMessage)
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
                case "SendingCustom":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Sending Custom DDRs..";
                    ThisClient.ProcessProgress = 95;
                    break;
                case "Complete":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Complete!";
                    ThisClient.ProcessProgress = 100;
                    break;
                    
                default:
                    break;
            }
        }
        private void RegisterClient(int thisIndex)
        {
            GetWait();
            string ThisFilePath = System.IO.Directory.GetCurrentDirectory();
            Device ThisClient = Devices[thisIndex];
            //Update UI
            FireProgress(thisIndex, "CreatingCert...");
            X509Certificate2 newCert = FauxDeployCMAgent.CreateSelfSignedCertificate(ThisClient.Name);
            string myPath = ExportCert(newCert, ThisClient.Name, ThisFilePath);

            //Update UI
            FireProgress(thisIndex, "CertCreated!");
            System.Threading.Thread.Sleep(1500);
            FireProgress(thisIndex, "Starting Inventory...");
            
            SmsClientId clientId = FauxDeployCMAgent.RegisterClient(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password);

            //Update UI
            FireProgress(thisIndex, "SendingDiscovery");
            FauxDeployCMAgent.SendDiscovery(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password, clientId);

            FireProgress(thisIndex, "RequestingPolicy");
            FauxDeployCMAgent.GetPolicy(CMServer, ThisClient.Name, DomainName, SiteCode, ExportPath, myPath, Password, clientId);

            FireProgress(thisIndex, "SendingCustom");
            FauxDeployCMAgent.SendCustomDiscovery(CMServer, ThisClient.Name, SiteCode, ThisFilePath, CustomClientRecords);

            FireProgress(thisIndex, "Complete");
        }

        private async void CreateClientsButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceExpander.IsExpanded = true;

            string BaseName = NewClientName.Text;
            int CountOfMachines = (Int32.TryParse(NumberOfClients.Text, out int unUsed)) ? Int32.Parse(NumberOfClients.Text) : 1;
            int BeginningWith = (!StartingNumber.Text.Length.Equals(0)) ? Int32.Parse(StartingNumber.Text) : 0;
            List<String> DeviceList = new List<string>();

            var progress = new Progress<int>(_ => IdCounter++) as IProgress<int>;

            for (int i = BeginningWith; i < CountOfMachines; i++)
            {
                string ThisClient = BaseName + i;
                DeviceList.Add(ThisClient);

            }

            var myTask = Task.Run(() =>
            {                
                Parallel.ForEach(DeviceList, new ParallelOptions { MaxDegreeOfParallelism = maxThreads}, device =>
                {
                    Device ThisDevice = new Device() { Name = device, Status = "CertCreated", ImageSource = "Images\\step01.png", ProcessProgress = 10 };
                    Devices.Add(ThisDevice);                    
                    int thisIndex = devices.IndexOf(ThisDevice);
                    RegisterClient(thisIndex);

                    progress.Report(0);
             
                });             
            });

            await myTask;            
            
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

        private void MaximumThreads_TextChanged(object sender, TextChangedEventArgs e)
        {
            maxThreads = int.Parse(MaximumThreads.Text);
        }
    }
}
