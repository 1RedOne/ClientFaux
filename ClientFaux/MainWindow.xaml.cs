﻿using System;
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
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;

namespace CMFaux
{

    // to do : fix discovery 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {        
        public SecureString Password { get; private set; }
        public string BaseName { get; private set; }
        public string CmServer { get; private set; }
        public string SiteCode { get; private set; }
        public string DomainName { get; private set; }
        public string ExportPath { get; private set; }
        public string CalculatedClientsCount { get; set; }
        public bool InventoryIsChecked { get; set; }
        public int MaxThreads { get; private set; }
        private int _idCounter;
        public int IdCounter
        {
            get => _idCounter;
            set
            {
                if (value == _idCounter) return;
                _idCounter = value;
                OnPropertyChanged("IdCounter");
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
        
        internal ObservableCollection<Device> Devices { get => devices; set => devices = value; }    
        private ObservableCollection<Device> devices = new ObservableCollection<Device>();
        
        internal ObservableCollection<CustomClientRecord> CustomClientRecords { get => customClientRecords; set => customClientRecords = value; }
        private ObservableCollection<CustomClientRecord> customClientRecords = new ObservableCollection<CustomClientRecord> {
            new CustomClientRecord(){ RecordName="ClientKind", RecordValue="FakeClient" }
        };

        private static object _syncLock = new object();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            FilePath.Text = System.IO.Directory.GetCurrentDirectory();
            PasswordBox.Password = "Pa$$w0rd!";
            MaximumThreads.Text = "4";
            StartingNumber.Text = "1";
            EndingNumber.Text = "21";
            DomainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            Password = PasswordBox.Password.ToSecureString();
            BaseName = NewClientName.Text;
            CmServer = CMServerName.Text;
            SiteCode = CMSiteCode.Text;
            ExportPath = FilePath.Text;
            InventoryIsChecked = InventoryCheck.IsChecked.Value;
            MaxThreads = int.Parse(MaximumThreads.Text);
            dgDevices.ItemsSource = Devices;
            dgInventory.ItemsSource = customClientRecords;
            BindingOperations.EnableCollectionSynchronization(Devices, _syncLock);
            BindingOperations.EnableCollectionSynchronization(CustomClientRecords, _syncLock);
            Counter.SetBinding(ContentProperty, new Binding("IdCounter"));
            DataContext = this;
            DeviceCounter = 0;
            string logPath = String.Concat(Directory.GetCurrentDirectory() + "CMLog.log");
            System.IO.FileStream myTraceLog = new System.IO.FileStream(logPath, System.IO.FileMode.OpenOrCreate);

            // Creates the new trace listener.

            System.Diagnostics.TextWriterTraceListener myListener = new System.Diagnostics.TextWriterTraceListener(myTraceLog);

            Trace.Listeners.Add(myListener);
        }
        private void FireProgress(int thisIndex, string statusMessage, int percentageComplete)
        {
            Device ThisClient = Devices[thisIndex];
            ThisClient.ProcessProgress = percentageComplete;
            switch
                (statusMessage)
            {
                case "CreatingCert...":
                    ThisClient.Status = "Creating Cert...";
                    break;
                case "CertCreated":
                    ThisClient.ImageSource = "Images\\step02.png";
                    ThisClient.Status = "Registering...";
                    break;
                case "Registering Client...":
                    ThisClient.ImageSource = "Images\\step02.png";
                    ThisClient.Status = "Starting Inventory...";
                    break;
                case "Starting Inventory...":
                    ThisClient.ImageSource = "Images\\step02.png";
                    ThisClient.Status = "Starting Inventory...";
                    break;
                case "SendingDiscovery":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Sending Discovery...";
                    break;
                case "RequestingPolicy":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Requesting Policy...";
                    break;
                case "SendingCustom":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Sending Custom DDRs..";
                    break;
                case "Complete":
                    ThisClient.ImageSource = "Images\\step03.png";
                    ThisClient.Status = "Complete!";
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
            FireProgress(thisIndex, "CreatingCert...", 15);
            X509Certificate2 newCert = FauxDeployCMAgent.CreateSelfSignedCertificate(ThisClient.Name);
            string myPath = ExportCert(newCert, ThisClient.Name, ThisFilePath);

            //Update UI
            FireProgress(thisIndex, "CertCreated!", 25);
            System.Threading.Thread.Sleep(1500);
            FireProgress(thisIndex, "Registering Client...", 30);
            
            SmsClientId clientId = FauxDeployCMAgent.RegisterClient(CmServer, ThisClient.Name, DomainName, myPath, Password);

            FireProgress(thisIndex, "Starting Inventory...", 50);
            FauxDeployCMAgent.SendDiscovery(CmServer, ThisClient.Name, DomainName, SiteCode, myPath, Password, clientId, InventoryIsChecked);

            FireProgress(thisIndex, "RequestingPolicy", 75);
            //FauxDeployCMAgent.GetPolicy(CMServer, SiteCode, myPath, Password, clientId);

            FireProgress(thisIndex, "SendingCustom", 85);
            FauxDeployCMAgent.SendCustomDiscovery(CmServer, ThisClient.Name, SiteCode, ThisFilePath, CustomClientRecords);

            FireProgress(thisIndex, "Complete", 100);
        }

        private async void CreateClientsButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceExpander.IsExpanded = true;

            string BaseName = NewClientName.Text;
            int CountOfMachines = (Int32.TryParse(NumberOfClients.Text, out int unUsed)) ? Int32.Parse(NumberOfClients.Text) : 1;
            
            
            int BeginningWith = (!StartingNumber.Text.Length.Equals(0)) ? Int32.Parse(StartingNumber.Text) : 0;
            List<String> DeviceList = new List<string>();
            int current = 0;
            object lockCurrent = new object();
            var progress = new Progress<int>(_ => IdCounter++) as IProgress<int>;

            for (int i = BeginningWith; i < CountOfMachines; i++)
            {
                string ThisClient = BaseName + i;
                DeviceList.Add(ThisClient);

            }

            var myTask = Task.Run(() =>
            {
                //Parallel.ForEach(DeviceList, new ParallelOptions { MaxDegreeOfParallelism = MaxThreads}, device =>
                //{
                //    Device ThisDevice = new Device() { Name = device, Status = "Starting...", ImageSource = "Images\\step01.png", ProcessProgress = 10 };
                //    Devices.Add(ThisDevice);                    
                //    int thisIndex = devices.IndexOf(ThisDevice);
                //    RegisterClient(thisIndex);

                //    progress.Report(0);

                //});             
                Parallel.For(0, DeviceList.Count,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxThreads },
                    (ii, loopState) =>
                    {
                       
                        // So the way Parallel.For works is that it chunks the task list up with each thread getting a chunk to work on...
                        // e.g. [1-1,000], [1,001- 2,000], [2,001-3,000] etc...
                        // We have prioritized our job queue such that more important tasks come first. So we don't want the task list to be
                        // broken up, we want the task list to be run in roughly the same order we started with. So we ignore tha past in 
                        // loop variable and just increment our own counter.
                        int thisCurrent = 0;
                        lock (lockCurrent)
                        {
                            thisCurrent = current;
                            current++;
                        }
                        string device = DeviceList[thisCurrent];
                        Device ThisDevice = new Device() { Name = device, Status = "Starting...", ImageSource = "Images\\step01.png", ProcessProgress = 10 };
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
            CmServer= CMServerName.Text;
            if (CmServer.Length.Equals(0))
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

        private void Inventory_Click (object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 3;
        }

        private void SCCMSettings_Click(object sender, RoutedEventArgs e)
        {
            TabControl.SelectedIndex = 4;
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
            CalculatedClientsCount = (Math.Abs(
                (EndingNo - StartingNo))).ToString();
            NumberOfClients.Text = CalculatedClientsCount;

            Console.WriteLine("Generating " + CalculatedClientsCount + " new clients");
        }

        private void PWChanged(object sender, RoutedEventArgs args)
        {
            Password = PasswordBox.Password.ToSecureString();
        }

        private void GetWait()
        {
            Random random = new Random();
            int w = random.Next(3, 7);
            System.Threading.Thread.Sleep(100 * w);
        }

        private void MaximumThreads_TextChanged(object sender, TextChangedEventArgs e)
        {
            MaxThreads = int.Parse(MaximumThreads.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            CustomClientRecord newRecord = new CustomClientRecord() { RecordName = NewDDRProp.Text, RecordValue = NewDDRValue.Text };
            CustomClientRecords.Add(newRecord);
            NewDDRValue.Text = null;
            NewDDRProp.Text = null;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            if (row != null)
            {
                row.DetailsVisibility = row.IsSelected ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            }
        }

        private void InventoryCheck_Click(object sender, RoutedEventArgs e)
        {
            InventoryIsChecked = InventoryCheck.IsChecked.Value;
            PerformInventoryLabel.Content = (InventoryIsChecked) ? "Perform In-depth Discovery (Slower): ✔" : "Perform In-depth Discovery (Slower): ❌";
        }
    }
}
