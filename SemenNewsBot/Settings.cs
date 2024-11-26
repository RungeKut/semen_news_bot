using System;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SemenNewsBot
{
    public class Settings
    {
        private static Settings instance;
        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();

                return instance;
            }
        }
        private readonly string? _tokenToAccess;
        private string path = "Settings.json";
        public string TokenToAccess { get { return _tokenToAccess ?? string.Empty; } }
        public Settings() 
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd();
                JsonSerializer.Deserialize<Settings>(json);
                Console.WriteLine("Read TokenToAccess: " + _tokenToAccess);
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
