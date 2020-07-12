
namespace CMFaux
{
    public class AppSettings
    {
        public string cmServerName { get; set; }
        public string cmServerCode { get; set; }
        public string baseNamePtrn { get; set; }


        public void Save(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(MySettings));
                xmls.Serialize(sw, this);
            }
        }
        public MySettings Read(string filename)
        {
            using (StreamReader sw = new StreamReader(filename))
            {
                XmlSerializer xmls = new XmlSerializer(typeof(MySettings));
                return xmls.Deserialize(sw) as MySettings;
            }
        }
    }
    //And here is how to use it. It's possible to load default values or override them with the user's settings by just checking if user settings exist:

    public class MyApplicationLogic
    {
        public const string UserSettingsFilename = "settings.xml";
        public string _DefaultSettingspath =
            Assembly.GetEntryAssembly().Location +
            "\\Settings\\" + UserSettingsFilename;

        public string _UserSettingsPath =
            Assembly.GetEntryAssembly().Location +
            "\\Settings\\UserSettings\\" +
            UserSettingsFilename;

        public MyApplicationLogic()
        {
            // if default settings exist
            if (File.Exists(_UserSettingsPath))
                this.Settings = Settings.Read(_UserSettingsPath);
            else
                this.Settings = Settings.Read(_DefaultSettingspath);
        }
        public MySettings Settings { get; private set; }

        public void SaveUserSettings()
        {
            Settings.Save(_UserSettingsPath);
        }
    }
}