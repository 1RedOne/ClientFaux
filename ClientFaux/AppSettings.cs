
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls.Primitives;

namespace CMFaux
{
    public class AppSettings
    {
        public string cmServerName { get; set; }
        public string cmServerCode { get; set; }
        public string baseNamePtrn { get; set; }

        public void Save(string filename)
        {
            string json = JsonConvert.SerializeObject(this);

            //write string to file
            System.IO.File.WriteAllText(filename, json);
        }

        public AppSettings Read(string filename)
        {
            var items = new AppSettings();
            using (StreamReader r = new StreamReader(filename))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<AppSettings>(json);
            }
            return items;

        }
    }
} 