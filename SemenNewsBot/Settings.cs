using System.Text.Json;

namespace SemenNewsBot
{
    public class Settings
    {
        private static Settings? instance;
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();
                return instance;
            }
        }

        private static readonly string _settingsPath = "Settings.json";

        private string _tokenToAccess;
        public string TokenToAccess { get { return _tokenToAccess ?? string.Empty; } set { _tokenToAccess = value; } }
        public Settings() 
        {
            _tokenToAccess = string.Empty;
        }

        public static void Init()
        {
            using (StreamReader reader = new StreamReader(_settingsPath))
            {
                string json = reader.ReadToEnd();
                Settings.instance = JsonSerializer.Deserialize<Settings>(json);
                Console.WriteLine("Read TokenToAccess: " + Settings.Instance.TokenToAccess);
            }

            // полная перезапись файла 
            //using (StreamWriter writer = new StreamWriter(path, false))
            //{
            //    writer.WriteLine(JsonSerializer.Serialize(this, typeof(Settings)));
            //}

            // добавление в файл
            //using (StreamWriter writer = new StreamWriter(path, true))
            //{
            //    writer.WriteLine("Addition");
            //    writer.Write("4,5");
            //}
        }
    }
}
