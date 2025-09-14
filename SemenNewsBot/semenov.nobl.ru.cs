using System.Net;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Text.RegularExpressions;
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
                    instance.Init();
                }
                return instance;
            }
        }
        public class Content
        {
            public string? Title { get; set; }
            public Uri? Link { get; set; }
            public string? Text { get; set; }
            public override bool Equals(object? obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj is not Content other) return false;

                // Нормализуем URI для сравнения (удаляем завершающий слеш)
                var thisLink = Title + NormalizeUri(Link) + Text;
                var otherLink = other.Title + NormalizeUri(other.Link) + other.Text;
                return thisLink == otherLink;
            }

            private static string? NormalizeUri(Uri? uri)
            {
                return uri?.ToString().TrimEnd('/') ?? string.Empty;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(
                    Title,
                    NormalizeUri(Link),
                    Text
                );
            }
        }
        private bool EnableSave { get; set; }
        private List<Content> SiteContent { get; set; }

        // Единый HttpClient для всего приложения
        private static readonly HttpClient SharedClient;

        static SemenovNoblRu()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            // Устанавливаем куки begetok для обхода анти-DDoS
            var baseAddress = new Uri("https://semenov.nobl.ru/");
            handler.CookieContainer.Add(baseAddress, new Cookie("beget", "begetok")
            {
                Expires = DateTime.UtcNow.AddDays(1),
                Path = "/",
                Domain = "semenov.nobl.ru"
            });

            SharedClient = new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(90)
            };
        }

        private SemenovNoblRu()
        {
            SiteContent = new List<Content>();
            EnableSave = true;
        }
        public async Task SemenovNoblRuExecuter(ITelegramBotClient botClient)
        {
            try
            {
                var stream = await SharedClient.GetStreamAsync("presscenter/news/rss/");
                using var xmlReader = XmlReader.Create(stream);
                var feed = SyndicationFeed.Load(xmlReader);

                bool newContentFound = false;

                foreach (var item in feed.Items)
                {
                    Content tempContent;
                    try
                    {
                        tempContent = new Content
                        {
                            Title = item.Title?.Text?.Trim(),
                            Link = item.Links.FirstOrDefault()?.Uri,
                            Text = Regex.Replace(item.Summary?.Text ?? "", @"\s+", " ").Trim()
                        };
                    }
                    catch
                    {
                        tempContent = new Content
                        {
                            Title = item.Title?.Text?.Trim(),
                            Link = item.Links.FirstOrDefault()?.Uri,
                            Text = item.Summary?.Text?.Trim()
                        };
                    }

                    if (tempContent.Title == null || tempContent.Link == null)
                        continue;

                    if (SiteContent.Contains(tempContent))
                    {
                        continue; // Уже есть
                    }

                    SiteContent.Add(tempContent);
                    newContentFound = true;

                    try
                    {
                        await botClient.SendMessage(
                            chatId: Settings.Instance.SemenovChatId,
                            text: $"{tempContent.Title}\n{tempContent.Link}\n\n{tempContent.Text}",
                            messageThreadId: Settings.Instance.SemenovThemeId
                        );
                        Console.WriteLine("NEW semenov.nobl.ru: " + tempContent.Title);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
                    }
                }

                if (newContentFound)
                {
                    // Ограничиваем список последними 100 записями
                    if (SiteContent.Count > 100)
                    {
                        SiteContent.RemoveRange(0, SiteContent.Count - 100);
                    }
                    Save();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Ошибка HTTP-запроса к RSS: {httpEx.Message}");
            }
            catch (XmlException xmlEx)
            {
                Console.WriteLine($"Ошибка парсинга XML: {xmlEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неизвестная ошибка при обработке RSS: {ex.Message}");
            }
        }

        public void Init()
        {
            try
            {
                if (File.Exists("semenov.nobl.ru.json"))
                {
                    string json = File.ReadAllText("semenov.nobl.ru.json");
                    var contentList = JsonSerializer.Deserialize<List<Content>>(json);
                    if (contentList != null)
                    {
                        SiteContent = contentList;
                    }
                    Console.WriteLine("Read semenov.nobl.ru.json ОК");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
                Save(); // Создаём пустой файл
            }
        }

        public void Save()
        {
            if (!EnableSave) return;

            try
            {
                string json = JsonSerializer.Serialize(SiteContent, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("semenov.nobl.ru.json", json);
                Console.WriteLine("Save semenov.nobl.ru.json ОК");
                EnableSave = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
    }
}
