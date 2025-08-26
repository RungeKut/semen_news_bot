using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using SemenNewsBot;
using System.Text.Json;
using System.Net;
using System.Xml;
using System.ServiceModel.Syndication;

internal static class Program
{
    // Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
    private static ITelegramBotClient? _botClient;

    // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
    private static ReceiverOptions? _receiverOptions;

    [STAThread]
    static async Task Main()
    {
        Settings.Init();
        _botClient = new TelegramBotClient(Settings.Instance.TokenToAccess); // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather
        _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
        {
            AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
            {
                UpdateType.Message, // Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
            }
        };

        using var cts = new CancellationTokenSource();

        // UpdateHander - обработчик приходящих Update`ов
        // ErrorHandler - обработчик ошибок, связанных с Bot API
        // Запускаем приём обновлений
        _botClient.StartReceiving(
            updateHandler: UpdateHandler,
            errorHandler: ErrorHandler,
            receiverOptions: _receiverOptions,
            cancellationToken: cts.Token
        ); // Запускаем бота

        User me = await _botClient.GetMe(); // Создаем переменную, в которую помещаем информацию о нашем боте.
        Console.WriteLine($"{me.FirstName} запущен!");

        //await Task.Delay(-1); // Устанавливаем бесконечную задержку, чтобы наш бот работал постоянно

        // =============== Основной цикл — проверка RSS каждые 10 минут ===============
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await SemenovNoblRu.Instance.SemenovNoblRuExecuter(_botClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в RSS-обработчике: {ex.Message}");
            }

            // Асинхронная задержка — не блокирует поток
            await Task.Delay(TimeSpan.FromMinutes(10), cts.Token);
        }
        Console.WriteLine($"{me.FirstName} остановлен!");
    }

    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
        try
        {
            // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        if (update.Message?.From?.Username == "GroupAnonymousBot")
                        {
                            if (update.Message.Text == "Это новостная тема Семенова!")
                            {
                                Settings.Instance.SemenovChatId = update.Message.Chat.Id;
                                Settings.Instance.SemenovThemeId = update.Message.MessageThreadId;
                                Settings.Save();

                                await botClient.DeleteMessage(
                                    chatId: update.Message.Chat.Id,
                                    messageId: update.Message.Id,
                                    cancellationToken: cancellationToken
                                );

                                await botClient.SendMessage(
                                    chatId: update.Message.Chat.Id,
                                    text: "Id новостной темы Семенова сохранено!",
                                    messageThreadId: update.Message.MessageThreadId,
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в UpdateHandler: {ex.Message}");
        }
    }

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
}
