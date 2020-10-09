using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientFaux.Models
{
    public partial class Models
    {
        public class Device : INotifyPropertyChanged
        {
            private string s;
            private string i;
            private int p;
            public string Name { get; set; }

            public string Status
            {
                get { return s; }
                set
                {
                    s = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("Status");
                }
            }

            public string ImageSource
            {
                get { return i; }
                set
                {
                    i = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("ImageSource");
                }
            }

            public int ProcessProgress
            {
                get { return p; }
                set
                {
                    p = value;
                    // Call OnPropertyChanged whenever the property is updated
                    OnPropertyChanged("ProcessProgress");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}