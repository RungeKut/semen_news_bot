using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;

namespace SemenNewsBot
{
    public class SemenovNoblRu
    {
        private static SemenovNoblRu? instance;
        public static SemenovNoblRu Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SemenovNoblRu();
                    Init();
                }
                return instance;
            }
        }
        public class Content
        {
            public string? Title { get; set; }
            public Uri? Link { get; set; }
            public string? Text { get; set; }
            public override bool Equals([NotNullWhen(true)] object? comparand)
            {
                if (comparand is null)
                {
                    return false;
                }

                if (ReferenceEquals(this, comparand))
                {
                    return true;
                }

                Content? obj = comparand as Content;

                if (obj is null)
                    return false;
                else
                    return (Title == obj.Title) && (Link == obj.Link) && (Text == obj.Text);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
        private bool EnableSave { get; set; }
        private List<Content> SiteContent { get; set; }
        private SemenovNoblRu()
        {
            SiteContent = new List<Content>();
            EnableSave = true;
        }
        public async void SemenovNoblRuExecuter(ITelegramBotClient botClient)
        {
            var baseAddress = new Uri("https://semenov.nobl.ru/");
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                cookieContainer.Add(baseAddress, new Cookie("beget", "begetok") { Expires = DateTime.UtcNow.AddDays(1), Path = "/" });
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    var stream = await client.GetStreamAsync("https://semenov.nobl.ru/presscenter/news/rss/");
                    using (var xmlReader = XmlReader.Create(stream))
                    {
                        var feed = SyndicationFeed.Load(xmlReader);
                        foreach (var item in feed.Items)
                        {
                            Content tempContent = new Content()
                            {
                                Title = item.Title.Text,
                                Link = item.Links.First().Uri,
                                Text = Regex.Replace(item.Summary.Text, @"\s+", " ").Trim()
                            };
                            if (SemenovNoblRu.Instance.SiteContent.Contains(tempContent))
                            {
                                SemenovNoblRu.Instance.EnableSave = false;
                            }
                            else
                            {
                                SemenovNoblRu.Instance.SiteContent.Add(tempContent);
                                botClient.SendMessage(Settings.Instance.SemenovChatId, tempContent.Title + "\n" + tempContent.Link + "\n\n" + tempContent.Text, messageThreadId: Settings.Instance.SemenovThemeId);
                                Console.WriteLine("NEW semenov.nobl.ru: " + tempContent.Title);
                                SemenovNoblRu.Instance.EnableSave = true;
                            }
                        }
                        while (SemenovNoblRu.Instance.SiteContent.Count > 100)
                        {
                            SemenovNoblRu.Instance.SiteContent.RemoveRange(0, 1);
                        }
                        Save();
                    }
                }
            }
        }

        public static void Init()
        {
            try
            {
                using (StreamReader reader = new StreamReader("semenov.nobl.ru.json"))
                {
                    string json = reader.ReadToEnd();
                    SemenovNoblRu.Instance.SiteContent = JsonSerializer.Deserialize<List<Content>>(json);
                    Console.WriteLine("Read semenov.nobl.ru.json ОК");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Save();
            }
        }
        public static void Save()
        {
            if (SemenovNoblRu.Instance.EnableSave)
            {
                using (StreamWriter writer = new StreamWriter("semenov.nobl.ru.json", false))
                {
                    writer.WriteLine(JsonSerializer.Serialize(SemenovNoblRu.Instance.SiteContent, typeof(List<Content>)));
                    Console.WriteLine("Save semenov.nobl.ru.json ОК");
                }
                SemenovNoblRu.Instance.EnableSave = false;
            }
        }
    }
}
