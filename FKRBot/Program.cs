using FKRBot.Enums;
using FKRBot.Services;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace FKRBot
{
    public class Program
    {
        private static TelegramBotClient _botClient;
        private static EzhkhService _ezhkhService;
        private static readonly string _token = "8097191478:AAFzMcIQFSXye3Eky6mD81DfGMQkaeLi9Xs"; // Укажите токен бота
        private static readonly Regex FioRegex = new(@"^[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}$");

        public static void Main()
        {
            _botClient = new TelegramBotClient(_token);

            UserStorage.UpdateState();

            // Запуск поллинга обновлений
            var cts = new CancellationTokenSource();
            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cancellationToken: cts.Token);

            Console.WriteLine("Bot is running...");
            Console.ReadLine();
            cts.Cancel();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Type == MessageType.Text && !string.IsNullOrEmpty(update.Message.Text))
            {
                var message = update.Message;

                switch (message.Text)
                {
                    case "/start":
                        {
                            if (!UserStorage.InspectorStore.TryGetValue(message.Chat.Id, out var value))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                    [
                                        ["Объекты капитального ремонта"]
                                    ])
                                {
                                    ResizeKeyboard = true
                                };

                                await _botClient.SendMessage(message.Chat.Id, $"Здравствуйте, {value.FIO}", replyMarkup: replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Зарегистрироваться":
                        {
                            if (!UserStorage.InspectorStore.TryGetValue(message.Chat.Id, out var value))
                            {
                                await _botClient.SendMessage(message.Chat.Id, "Введите Ваше ФИО, каждое слово с большой буквы");

                                var newUser = new Entities.User
                                {
                                    TelegramID = message.Chat.Id.ToString(),
                                    UserActivityStateType = Enums.UserActivityStateType.FIO
                                };

                                UserStorage.InspectorStore.Add(message.Chat.Id, newUser);
                            }
                            else
                            {
                                await _botClient.SendMessage(message.Chat.Id, "Вы уже зарегистрированы");

                                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                    [
                                        ["Объекты капитального ремонта"]
                                    ])
                                {
                                    ResizeKeyboard = true
                                };

                                await _botClient.SendMessage(message.Chat.Id, $"Здравствуйте, {value.FIO}", replyMarkup: replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Объекты капитального ремонта":
                        {
                            if (!UserStorage.InspectorStore.TryGetValue(message.Chat.Id, out var value))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                value.UserActivityStateType = UserActivityStateType.MO;

                                var municipalities = await _ezhkhService.GetMunicipalities(value.TelegramID);

                                if (municipalities == null)
                                {
                                    await _botClient.SendMessage(message.Chat.Id, "Не удалось загрузить муниципальные образования");
                                    break;
                                }

                                if (!municipalities.Any())
                                {
                                    await _botClient.SendMessage(message.Chat.Id, "У вас нет ни одного контракта");
                                }

                                var rowsCount = municipalities.Length % 2 == 0 ? municipalities.Length / 2 : municipalities.Length / 2 + 1;

                                var keyboardButtons = new InlineKeyboardButton[rowsCount][];

                                if (municipalities.Length % 2 == 0)
                                {
                                    for (var i = 0; i < rowsCount; i++)
                                    {
                                        keyboardButtons[i] = 
                                        [
                                            InlineKeyboardButton.WithCallbackData(municipalities[2 * i].Municipality + " " + municipalities[2 * i].Count, $"Мун%{municipalities[2 * i].Id}"),
                                            InlineKeyboardButton.WithCallbackData(municipalities[2 * i + 1].Municipality + " " + municipalities[2 * i + 1].Count, $"Мун%{municipalities[2 * i + 1].Id}")
                                        ];
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i < rowsCount - 1; i++)
                                    {
                                        keyboardButtons[i] = 
                                        [
                                            InlineKeyboardButton.WithCallbackData(municipalities[2 * i].Municipality + " " + municipalities[2 * i].Count, $"Мун%{municipalities[2 * i].Id}"),
                                            InlineKeyboardButton.WithCallbackData(municipalities[2 * i + 1].Municipality + " " + municipalities[2 * i + 1].Count, $"Мун%{municipalities[2 * i + 1].Id}")
                                        ];
                                    }

                                    keyboardButtons[rowsCount - 1] = [InlineKeyboardButton.WithCallbackData(municipalities.Last().Municipality + " " + municipalities.Last().Count, $"Мун%{municipalities.Last().Id}")];
                                }

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(keyboardButtons);

                                await _botClient.SendMessage(message.Chat.Id, "Выберите муниципальное образование", replyMarkup: replyKeyboardMarkup);
                            }

                            break;
                        }
                    default:
                        {
                            if (!UserStorage.InspectorStore.TryGetValue(message.Chat.Id, out var value))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                switch (value.UserActivityStateType)
                                {
                                    case UserActivityStateType.FIO:
                                        {
                                            var fio = message.Text.Trim();

                                            if (FioRegex.IsMatch(fio))
                                            {
                                                value.FIO = fio;
                                                value.UserActivityStateType = UserActivityStateType.NotSet;

                                                var registrSuccess = await _ezhkhService.RegisterTGInspector(value);

                                                if (registrSuccess)
                                                {
                                                    await _botClient.SendMessage(message.Chat.Id, "Вы успешно зарегистрировались");
                                                }
                                                else
                                                {
                                                    UserStorage.InspectorStore.Remove(message.Chat.Id);

                                                    await _botClient.SendMessage(message.Chat.Id, "При регистрации произошла ошибка, попробуйте позже");
                                                }
                                            }
                                            else
                                            {
                                                await _botClient.SendMessage(message.Chat.Id, "Введенное ФИО не соответствует формату");
                                            }

                                            break;
                                        }
                                    case UserActivityStateType.NotSet:
                                        {
                                            var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                                            [
                                                ["Объекты капитального ремонта"]
                                            ])
                                            {
                                                ResizeKeyboard = true
                                            };

                                            await _botClient.SendMessage(message.Chat.Id, $"Вы не выбрали команду", replyMarkup: replyKeyboardMarkup);

                                            break;
                                        }
                                }
                            }

                            break;
                        }
                }
            }
            else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: { Data: not null, Message: not null } })
            {
                if (!UserStorage.InspectorStore.TryGetValue(update.CallbackQuery.Message.Chat.Id, out var value))
                {
                    await SendRegistrationInfo(update.CallbackQuery.Message.Chat.Id);
                }
                else
                {
                    switch (update.CallbackQuery.Data.Split("%")[0])
                    {
                        default:
                            {
                                switch (value.UserActivityStateType)
                                {
                                    case UserActivityStateType.MO:
                                        {
                                            var moId = update.CallbackQuery.Data.Split("%")[1];



                                            break;
                                        }
                                }

                                break;
                            }
                    }
                }
            }
        }

        private static async Task SendRegistrationInfo(long userId)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(
            [
                ["Зарегистрироваться"]
            ])
            {
                ResizeKeyboard = true
            };

            await _botClient.SendMessage(userId, "Вам необходимо зарегистрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram", replyMarkup: replyKeyboardMarkup);
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
