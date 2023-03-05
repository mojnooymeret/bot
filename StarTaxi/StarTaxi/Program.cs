using Newtonsoft.Json;
using StarTaxi.DataBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace StarTaxi
{
   internal class Program
   {
      private static string token { get; set; } = "5867072259:AAHbJvCwzNZmuHskFykl8BSHaLrBcaEAAM4";
      private static TelegramBotClient client;
      static void Main()
      {
         Connect.LoadWork(works);
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnUpdate += UpdateData;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }

      readonly static InlineKeyboardMarkup cancelReg = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelReg") } });
      public static List<User> users = new List<User>();
      public static List<LastMessage> lastMessages = new List<LastMessage>();
      public static List<Driver> drivers = new List<Driver>();
      public static List<Answer> answers = new List<Answer>();
      public static List<Order> orders = new List<Order>();
      public static List<Work> works = new List<Work>();

      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            Connect.LoadOrder(orders);
            var checkOrder = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status != "success" && x.status != "cancel" && x.end_latitude != 0 && x.end_logitude != 0);
            if (checkOrder == null) {
               checkOrder = orders.Find(x => x.id_driver == message.Chat.Id.ToString() && x.status != "success" && x.status != "cancel");
               if (checkOrder == null) {
                  try {
                     await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                  } catch { }
                  Connect.LoadLastMessage(lastMessages);
                  var msg = lastMessages.Find(x => x.id == message.Chat.Id.ToString());
                  if (msg != null) {
                     Connect.LoadUser(users);
                     if (msg.bot_message != "none") {
                        if (msg.bot_message == "WaitName")
                           ChangeName(message, "reg");
                        else if (msg.bot_message == "ChangeName")
                           ChangeName(message, "change");
                        else if (msg.bot_message == "ChangePhone")
                           ChangePhone(message, "change");
                        else if (msg.bot_message == "WaitPhone")
                           ChangePhone(message, "reg");
                        else if (msg.bot_message == "WaitFIO") {
                           if (message.Text.Contains(" ")) {
                              if (message.Text.Split(' ').Length == 3) {
                                 string[] words = message.Text.Split(' ');
                                 string fio = string.Empty;
                                 for (int i = 0; i < words.Length; i++)
                                    fio += words[i][..1].ToUpper() + words[i][1..] + " ";
                                 fio = fio.Trim(' ');
                                 Connect.LoadDriver(drivers);
                                 if (drivers.Find(x => x.id == message.Chat.Id.ToString()) == null)
                                    Connect.Query("insert into `Driver` (id, fio) values ('" + message.Chat.Id + "', '" + fio + "');");
                                 else Connect.Query("update `Driver` fio = '" + fio + "' where id = '" + message.Chat.Id + "';");
                                 ChangeMessage(message, msg, "WaitDPhone");
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВведите рабочий номер телефона", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                              }
                              else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Введите своё ФИО (Пример: Иванов Иван Иванович)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Введите своё ФИО (Пример: Иванов Иван Иванович)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitDPhone") {
                           if (message.Text.Length == 11) {
                              if (message.Text[0] == '8') {
                                 string phone = message.Text[0] + " (" + message.Text[1] + message.Text[2] + message.Text[3] + ") " + message.Text[4] + message.Text[5] + message.Text[6] + "-" + message.Text[7] + message.Text[8] + "-" + message.Text[9] + message.Text[10];
                                 Connect.Query("update `Driver` set phone = '" + phone + "' where id = '" + message.Chat.Id + "';");
                                 ChangeMessage(message, msg, "WaitExp");
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВведите водительский стаж в годах (цифрой)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                              }
                              else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНомер телефона должен начинаться на 8 и состоять из 11 цифр, введите номер еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНомер телефона должен начинаться на 8 и состоять из 11 цифр, введите номер еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitExp") {
                           try {
                              Convert.ToInt32(message.Text);
                              Connect.Query("update `Driver` set experiense = '" + message.Text + "' where id = '" + message.Chat.Id + "';");
                              ChangeMessage(message, msg, "WaitAuto");
                              await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВведите марку и модель автомобиля (Например: Hyundai Solaris)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                           } catch { await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат, введите водительский стаж в года (цифрой) еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg); }
                        }
                        else if (msg.bot_message == "WaitAuto") {
                           string auto = string.Empty;
                           if (message.Text.Contains(" ")) {
                              string[] words = message.Text.Split(' ');
                              for (int i = 0; i < words.Length; i++) {
                                 if (words.Length > 1)
                                    auto += words[i][..1].ToUpper() + words[i][1..] + " ";
                              }
                              auto = auto.Trim(' ');
                           }
                           else auto += message.Text[..1].ToUpper() + message.Text[1..];
                           Connect.Query("update `Driver` set auto = '" + auto + "' where id = '" + message.Chat.Id + "';");
                           ChangeMessage(message, msg, "WaitPlate");
                           await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВведите номерной знак автомобиля без пробелов (Например: А777АА159)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitPlate") {
                           if (!message.Text.Contains(" ")) {
                              string plate = message.Text.ToUpper();
                              if (plate[0] >= 'А' && plate[0] <= 'Я' && plate[1] >= '0' && plate[1] <= '9' && plate[2] >= '0' && plate[2] <= '9' && plate[3] >= '0' && plate[3] <= '9' && plate[4] >= 'А' && plate[4] <= 'Я' && plate[5] >= 'А' && plate[5] <= 'Я' && plate[6] >= '0' && plate[6] <= '9' && plate[7] >= '0' && plate[7] <= '9') {
                                 if (plate.Length >= 9) {
                                    if (plate[0]! >= '0' && plate[0]! <= '9') {
                                       await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Введите номерной знак автомобиля без пробелов (Например: А777АА159)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                                       return;
                                    }
                                 }
                                 Connect.Query("update `Driver` set plate_number = '" + plate + "' where id = '" + message.Chat.Id + "';");
                                 ChangeMessage(message, msg, "WaitFace");
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nОтправьте фотографию водительского удостоверения", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                              }
                              else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Введите номерной знак автомобиля без пробелов (Например: А777АА159)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Введите номерной знак автомобиля без пробелов (Например: А777АА159)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitFace") {
                           if (message.Photo != null) {
                              string photo = message.Photo[^1].FileId;
                              Connect.Query("update `Driver` set photo_face = '" + photo + "', status = 'wait' where id = '" + message.Chat.Id + "';");
                              ChangeMessage(message, msg, "WaitCarPhoto");
                              await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nОтправьте фотографию рабочего автомобиля", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Отправьте фотографию водительского удостоверения", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitCarPhoto") {
                           if (message.Photo != null) {
                              string photo = message.Photo[^1].FileId;
                              Connect.Query("update `Driver` set photo_auto = '" + photo + "', status = 'wait' where id = '" + message.Chat.Id + "';");
                              ChangeMessage(message, msg, "none");
                              InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Отправить заявку", "SendRequestReg") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelReg") } });
                              Connect.LoadDriver(drivers);
                              var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
                              if (driver != null) {
                                 string[] medias = { driver.photo_face, driver.photo_auto };
                                 string request = "ФИО: " + driver.fio + "\nТелефон: " + driver.phone + "\nВодительский стаж: " + driver.experiense + "\nАвтомобиль: " + driver.auto + "\nНомерной знак: " + driver.plate_number;
                                 Telegram.Bot.Types.IAlbumInputMedia[] mediaGroup = GetMedia(medias, request);
                                 await client.SendMediaGroupAsync(message.Chat.Id, mediaGroup);
                                 await client.SendTextMessageAsync(message.Chat.Id, "Выберите действие", replyMarkup: keyboard);
                              }
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nНеверный формат. Отправьте фотографию рабочего автомобиля", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        }
                        else if (msg.bot_message == "WaitMessageSupport") {
                           InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Отправить сообщение", "SendMessageSupport") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                           await client.SendTextMessageAsync(message.Chat.Id, "*Обращение в техническую поддержку*\n\nТекст сообщения:\n" + message.Text, Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                        }
                        else if (msg.bot_message == "WaitOnePoint") {
                           InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                           try {
                              try {
                                 await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                              } catch { }
                              if (message.Location != null) {
                                 Connect.Query("insert into `Order` (id_client, start_latitude, start_longitude, end_latitude, end_longitude, status) values ('" + message.Chat.Id + "', '" + message.Location.Latitude.ToString().Replace(",", ".") + "', '" + message.Location.Longitude.ToString().Replace(",", ".") + "', 0, 0, 'wait');");
                                 ChangeMessage(message, msg, "WaitTwoPoint");
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОтправьте конечную точку маршрута геолокацией (куда увезти клиента)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                              }
                              else await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОтправьте стартовую точку маршрута геолокацией (куда подъехать машине)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                           } catch { await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОтправьте стартовую точку маршрута геолокацией (куда подъехать машине)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard); }
                        }
                        else if (msg.bot_message == "WaitTwoPoint") {
                           try {
                              InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                              InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚕 Стандарт", "StandartTaxi") }, new[] { InlineKeyboardButton.WithCallbackData("💺 Детский", "ChildTaxi") }, new[] { InlineKeyboardButton.WithCallbackData("🐶 Перевозка животных", "AnimalTaxi") }, new[] { InlineKeyboardButton.WithCallbackData("🚚 Грузовой", "CargoTaxi") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                              try {
                                 await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                              } catch { }
                              if (message.Location != null) {
                                 Connect.Query("update `Order` set end_latitude = '" + message.Location.Latitude.ToString().Replace(",", ".") + "', end_longitude = '" + message.Location.Longitude.ToString().Replace(",", ".") + "' where id_client = '" + message.Chat.Id + "' and status = 'wait';");
                                 ChangeMessage(message, msg, "SelectTarif");
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nВыберите тариф:\nСтандарт - обычное такси\nДетский - автомобиль с детским креслом\nГрузовой - автомобиль для перевозки", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                              }
                              else await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОтправьте конечную точку маршрутка геолокацией (куда подъехать машине)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                           } catch { }
                        }
                        else if (msg.bot_message == "SelectTarif") {
                           try {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nВыберите тариф для продолжения", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           } catch { }
                        }
                        return;
                     }
                  }
                  if (message.Text == "/start") {
                     Connect.LoadUser(users);
                     var user = users.Find(x => x.id == message.Chat.Id.ToString());
                     if (user == null) {
                        await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация*\n\nДля начала работы с ботом введите ваше Имя", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        ChangeMessage(message, msg, "WaitName");
                     }
                     else await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация*\n\nВы уже зарегистрированы", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else if (message.Text == "/info") {
                     Connect.LoadDriver(drivers);
                     if (drivers.Find(x => x.id == message.Chat.Id.ToString()) != null) {
                        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("📄 Как заказать такси", "WhatOne") }, new[] { InlineKeyboardButton.WithCallbackData("📄 Как изменить данные профиля", "WhatTwo") }, new[] { InlineKeyboardButton.WithCallbackData("📄 Как выполнять заказы таксистом", "WhatFour") }, new[] { InlineKeyboardButton.WithCallbackData("📄 Как изменить данные таксиста", "WhatFive") } });
                        await client.SendTextMessageAsync(message.Chat.Id, "📚 Инструкция работы с ботом", replyMarkup: keyboard);
                     }
                     else {
                        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("📄 Как заказать такси", "WhatOne") }, new[] { InlineKeyboardButton.WithCallbackData("📄 Как изменить данные профиля", "WhatTwo") }, new[] { InlineKeyboardButton.WithCallbackData("📄 Как начать работать таксистом", "WhatThree") } });
                        await client.SendTextMessageAsync(message.Chat.Id, "📚 Инструкция работы с ботом", replyMarkup: keyboard);
                     }
                  }
                  else if (message.Text == "/profile") {
                     if (works.Find(x => x.id_driver == message.Chat.Id.ToString()) == null)
                        GetProfile(message);
                     else await client.SendTextMessageAsync(message.Chat.Id, "*Профиль*\n\nНа смене можно просмотреть только личный кабинет водителя - /drivermenu", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else if (message.Text == "/driver") {
                     Connect.LoadDriver(drivers);
                     var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
                     if (driver != null)
                        await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВы уже являетесь водителем", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     else {
                        await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация водителя*\n\nВведите своё ФИО", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelReg);
                        ChangeMessage(message, msg, "WaitFIO");
                     }
                  }
                  else if (message.Text == "/call") {
                     await client.SendTextMessageAsync(message.Chat.Id, "*Вызов диспетчера*\n\nДиспетчер доступен по номеру телефона 8 (800) 800-80-80", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else if (message.Text == "/support") {
                     InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("👨‍💻 Написать в поддержку", "WriteSupport") } });
                     await client.SendTextMessageAsync(message.Chat.Id, "*Обращение в техническую поддержку*\n\nЕсли у вас возникла какая либо проблема, вы можете обратиться в тех. поддержку. Излагайте свои мысли кратко и понятно", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                  }
                  else if (message.Text == "/drivermenu") {
                     Connect.LoadDriver(drivers);
                     var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
                     if (driver == null || driver.status != "sleep" && driver.status != "work")
                        await client.SendTextMessageAsync(message.Chat.Id, "*Личный кабинет*\n\nВы не являетесь водителем, чтобы стать им отправьте команду /driver", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     else {
                        string rating = GetRating(driver);
                        InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚕 Начать смену", "SelectTarifDriver") }, new[] { InlineKeyboardButton.WithCallbackData("⚙️ Редактировать профиль", "EditDriver") } });
                        if (driver.status == "work")
                           keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚗 Закончить смену", "EndWork") } });
                        await client.SendTextMessageAsync(message.Chat.Id, "*Личный кабинет*\n\nФИО: " + driver.fio + "\nТелефон: " + driver.phone + "\nАвтомобиль: " + driver.auto + "\nРейтинг: " + rating + "\nСоврешено поездок: " + driver.ride_count, Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                     }
                  }
                  else if (message.Text == "/taxi") {
                     Connect.LoadOrder(orders);
                     if (orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status == "wait") == null) {
                        if (works.Count > 0) {
                           var active = drivers.Find(x => x.id == message.Chat.Id.ToString());
                           if (active == null || active.status != "work") {
                              InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                              await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОтправьте стартовую точку маршрута геолокацией (куда подъехать машине)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                              ChangeMessage(message, msg, "WaitOnePoint");
                           }
                           else await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nВызов такси на смене невозможен, закончите смену, после чего попробуйте еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                        else await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению нет ни одного водителя на линии", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     }
                     else await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nВы уже вызвали такси, ожидайте ответа от водителей, либо отмените заказ нажатием на кнопку \"Отменить\"", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
               }
               else {
                  await client.SendTextMessageAsync(checkOrder.id_client, "🗣 " + message.Text);
               }
            }
            else {
               await client.SendTextMessageAsync(checkOrder.id_driver, "🗣 " + message.Text);
            }
         } catch { }
      }

      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message;
            var data = ev.CallbackQuery.Data;
            Connect.LoadLastMessage(lastMessages);
            var msg = lastMessages.Find(x => x.id == message.Chat.Id.ToString());
            InlineKeyboardMarkup cancelKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
            if (data == "ChangeName") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               if (msg == null)
                  Connect.Query("insert into 'LastMessage' (id, bot_message) values ('" + message.Chat.Id + "', 'ChangeName');");
               else Connect.Query("update 'LastMessage' set bot_message = 'ChangeName' where id = '" + message.Chat.Id + "';");
               await client.SendTextMessageAsync(message.Chat.Id, "*Редактирование профиля*\n\nВведите новое имя", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelKey);
            }
            else if (data == "ChangePhone") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               if (msg == null)
                  Connect.Query("insert into 'LastMessage' (id, bot_message) values ('" + message.Chat.Id + "', 'ChangePhone');");
               else Connect.Query("update 'LastMessage' set bot_message = 'ChangePhone' where id = '" + message.Chat.Id + "';");
               await client.SendTextMessageAsync(message.Chat.Id, "*Редактирование профиля*\n\nВведите новый номер телефона", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelKey);
            }
            else if (data.Contains("Cancel")) {
               if (msg != null) Connect.Query("update `LastMessage` set bot_message = 'none' where id = '" + message.Chat.Id + "';");
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               if (data == "CancelReg") {
                  Connect.LoadDriver(drivers);
                  var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
                  if (driver != null) {
                     if (driver.status == "" || driver.status == null) {
                        Connect.Query("delete from `Driver` where id = '" + message.Chat.Id + "';");
                        await client.SendTextMessageAsync(message.Chat.Id, "⛔️ Регистрация отменена");
                     }
                  }
               }
               else if (data == "CancelTaxi") {
                  Connect.LoadOrder(orders);
                  try {
                     await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                  } catch { }
                  var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status != "success" && x.status != "cancel");
                  if (order != null) {
                     Connect.Query("delete from `Order` where id_client = '" + order.id_client + "' and status = '" + order.status + "';");
                     if (order.id_driver != "" && order.id_driver != null) {
                        var driver = works.Find(x => x.id_driver == order.id_driver);
                        if (driver != null) {
                           ChangeStatusDriver(driver, "free");
                           try {
                              await client.DeleteMessageAsync(driver.id_driver, Convert.ToInt32(driver.messages.Split(' ')[0]));
                              await client.DeleteMessageAsync(driver.id_driver, Convert.ToInt32(driver.messages.Split(' ')[1]));
                              await client.DeleteMessageAsync(driver.id_driver, Convert.ToInt32(driver.messages.Split(' ')[2]));
                              await client.DeleteMessageAsync(driver.id_driver, Convert.ToInt32(driver.messages.Split(' ')[3]));
                              await client.DeleteMessageAsync(message.Chat.Id, Convert.ToInt32(driver.messages.Split(' ')[4]));
                           } catch { }
                           await client.SendTextMessageAsync(driver.id_driver, "*Заказ*\n\nЗаказ был отменен пользователем", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                     }
                  }
                  await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nВы отменили заказ", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               }
               else
                  await client.SendTextMessageAsync(message.Chat.Id, "⛔️ Отменено");
            }
            else if (data == "StandartTaxi") {
               try {
                  try {
                     await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                  } catch { }
                  Connect.LoadOrder(orders);
                  var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status == "wait");
                  Connect.Query("update `Order` set tarif = 'standart' where id = " + order.id + ";");
                  CallTaxi(message, order, "standart");
               } catch { }
            }
            else if (data == "ChildTaxi") {
               try {
                  try {
                     await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                  } catch { }
                  Connect.LoadOrder(orders);
                  var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status == "wait");
                  Connect.Query("update `Order` set tarif = 'child' where id = " + order.id + ";");
                  CallTaxi(message, order, "child");
               } catch { }
            }
            else if (data == "AnimalTaxi") {
               try {
                  try {
                     await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                  } catch { }
                  Connect.LoadOrder(orders);
                  var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status == "wait");
                  Connect.Query("update `Order` set tarif = 'animal' where id = " + order.id + ";");
                  CallTaxi(message, order, "animal");
               } catch { }
            }
            else if (data == "CargoTaxi") {
               try {
                  try {
                     await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                  } catch { }
                  Connect.LoadOrder(orders);
                  var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.status == "wait");
                  Connect.Query("update `Order` set tarif = 'cargo' where id = " + order.id + ";");
                  CallTaxi(message, order, "cargo");
               } catch { }
            }
            else if (data == "SendRequestReg") {
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
               var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
               if (driver != null) {
                  InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Принять", "AcceptDriver") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "NoDriver") } });
                  string request = "Регистрация нового водителя\n\nТелеграм: @" + message.Chat.Username + "\nФИО: " + driver.fio + "\nТелефон: " + driver.phone + "\nВодительский стаж: " + driver.experiense + "\nАвтомобиль: " + driver.auto + "\nНомерной знак: " + driver.plate_number + "\n\n" + message.Chat.Id;
                  string[] medias = { driver.photo_face, driver.photo_auto };
                  Telegram.Bot.Types.IAlbumInputMedia[] mediaGroup = GetMedia(medias, request);
                  await client.SendMediaGroupAsync(-1001528750159, mediaGroup);
                  await client.SendTextMessageAsync(-1001528750159, "Выберите действие\n\n" + message.Chat.Id, replyMarkup: keyboard);
                  await client.SendTextMessageAsync(message.Chat.Id, "*Рассмотрение регистрации водителя*\n\nВаша заявка на регистрацию отправлена на проверку", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               }
            }
            else if (data == "AcceptDriver") {
               string[] lines = message.Text.Split('\n');
               Connect.Query("update `Driver` set status = 'sleep', rating = '0', ride_count = '0' where id = '" + lines[^1] + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "✅ Одобрено\n\n" + lines[^1], replyMarkup: null);
               await client.SendTextMessageAsync(lines[^1], "*Рассмотрение регистрации водителя*\n\n✅ Ваша заявка на регистрацию одобрена", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "NoDriver") {
               string[] lines = message.Text.Split('\n');
               Connect.Query("delete from `Driver` where id = '" + lines[^1] + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "⛔️ Отклонено\n\n" + lines[^1], replyMarkup: null);
               await client.SendTextMessageAsync(lines[^1], "*Рассмотрение регистрации водителя*\n\n⛔️ Ваша заявка на регистрацию отклонена", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "SelectTarifDriver") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               Connect.LoadDriver(drivers);
               string[] list = GetCategory(message.Chat.Id.ToString());
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData(list[0], "SelectCategoryStandart") }, new[] { InlineKeyboardButton.WithCallbackData(list[1], "SelectCategoryChild") }, new[] { InlineKeyboardButton.WithCallbackData(list[2], "SelectCategoryAnimal") }, new[] { InlineKeyboardButton.WithCallbackData(list[3], "SelectCategoryCargo") }, new[] { InlineKeyboardButton.WithCallbackData("Начать смену", "StartWork") }, new[] { InlineKeyboardButton.WithCallbackData("Отменить", "Cancel") } });
               await client.SendTextMessageAsync(message.Chat.Id, "*Начало смены*\n\nУкажите тарифы по которым хотите работать:", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
            }
            else if (data.Contains("SelectCategory")) {
               Connect.LoadDriver(drivers);
               int id = -1;
               if (data.Contains("Standart")) id = 0;
               else if (data.Contains("Child")) id = 1;
               else if (data.Contains("Animal")) id = 2;
               else if (data.Contains("Cargo")) id = 3;
               string[] list = GetCategory(message.Chat.Id.ToString());
               string newCategory = string.Empty;
               for (int i = 0; i < list.Length; i++) {
                  if (i == id && list[id].Contains("✅")) continue;
                  else if (i == id && list[id].Contains("⛔️")) {
                     if (i == 0) newCategory += "standart|";
                     else if (i == 1) newCategory += "child|";
                     else if (i == 2) newCategory += "animal|";
                     else if (i == 3) newCategory += "cargo|";
                  }
                  else if (list[i].Contains("✅")) {
                     if (i == 0) newCategory += "standart|";
                     else if (i == 1) newCategory += "child|";
                     else if (i == 2) newCategory += "animal|";
                     else if (i == 3) newCategory += "cargo|";
                  }
               }
               newCategory = newCategory.Trim('|');
               Connect.Query("update `Driver` set category = '" + newCategory + "' where id = " + message.Chat.Id + ";");
               Connect.LoadDriver(drivers);
               list = GetCategory(message.Chat.Id.ToString());
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData(list[0], "SelectCategoryStandart") }, new[] { InlineKeyboardButton.WithCallbackData(list[1], "SelectCategoryChild") }, new[] { InlineKeyboardButton.WithCallbackData(list[2], "SelectCategoryAnimal") }, new[] { InlineKeyboardButton.WithCallbackData(list[3], "SelectCategoryCargo") }, new[] { InlineKeyboardButton.WithCallbackData("Начать смену", "StartWork") }, new[] { InlineKeyboardButton.WithCallbackData("Отменить", "Cancel") } });
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: keyboard);
               } catch { }
            }
            else if (data == "StartWork") {
               Connect.LoadDriver(drivers);
               var driver = drivers.Find(x => x.id == message.Chat.Id.ToString());
               if (driver != null) {
                  if (driver.status == "sleep") {
                     try {
                        await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                     } catch { }
                     Connect.Query("update `Driver` set status = 'work' where id = '" + message.Chat.Id + "';");
                     Connect.LoadDriver(drivers);
                     WorkList("add", null, driver);
                     await client.SendTextMessageAsync(message.Chat.Id, "*Смена*\n\nВы вышли на смену, ожидайте новые заказы (включите уведомления на телефоне чтобы не пропустить заказ)\nЧтобы закончить смену выполните команду /drivermenu и нажмите на кнопку \"Закончить смену\"\n\nКоличество машин на линии: " + drivers.FindAll(x => x.status == "work").Count + " 🚕", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
               }
            }
            else if (data == "EndWork") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               Connect.Query("update `Driver` set status = 'sleep' where id = '" + message.Chat.Id + "';");
               var worker = works.Find(x => x.id_driver == message.Chat.Id.ToString());
               if (worker != null) {
                  WorkList("delete", worker, null);
                  await client.SendTextMessageAsync(message.Chat.Id, "*Смена*\n\nВы закончили смену", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               }
            }
            else if (data == "EditDriver") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("👨‍💻 Написать в поддержку", "WriteSupport") } });
               await client.SendTextMessageAsync(message.Chat.Id, "Для редактирования данных профиля обратитесь в техническую поддержку", replyMarkup: keyboard);
            }
            else if (data == "WriteSupport") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               Connect.LoadAnswer(answers);
               var answer = answers.Find(x => x.id == message.Chat.Id.ToString());
               if (answer == null) {
                  InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                  await client.SendTextMessageAsync(message.Chat.Id, "*Обращение в техническую поддержку*\n\nВведите текст сообщения\nПри необходимости сменить личные данные необходимо подтвердить их подлинность", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
                  ChangeMessage(message, msg, "WaitMessageSupport");
               }
               else
                  await client.SendTextMessageAsync(message.Chat.Id, "*Обращение в техническую поддержку*\n\nВы уже отправили вопрос в тех. поддержку, ожидайте ответа", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "SendMessageSupport") {
               ChangeMessage(message, msg, "none");
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               string request = "Телеграм: @" + message.Chat.Username + "\n\n" + message.Text.Replace("Обращение в техническую поддержку\n\nТекст сообщения:\n", string.Empty) + "\n\n" + message.Chat.Id + "";
               Connect.Query("insert into `Answer` (id, text, status) values ('" + message.Chat.Id + "', '" + request + "', 'wait');");
               Connect.LoadAnswer(answers);
               var answer = answers.FindAll(x => x.id == message.Chat.Id.ToString());
               await client.SendTextMessageAsync(-1001845851895, "№" + answer[^1].id_message + "\n" + request);
            }
            else if (data == "WhatOne") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               await client.SendTextMessageAsync(message.Chat.Id, "*Как вызвать такси?*\n\nДля вызова такси введите команду /taxi из меню или с клавиатуры, после чего нажмите на кнопку с иконкой \"Скрепка\" слева от поля ввода сообщения, в открывшемся окне внизу выберите пункт \"Геопозиция\" и отправьте точку на карте, куда следует подъехать таксисту. После успешной отправки стартовой точки, проделайте аналогичные действия с указанием конечной точки на карте(куда следует отвезти клиента). После отправки стартовой и конечной точки появится сообщение с ценой заказа и кнопками заказа и отмены. Для поиска автомобиля нажмите на кнопку заказать.", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "WhatTwo") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               await client.SendTextMessageAsync(message.Chat.Id, "*Как изменить данные профиля?*\n\nДля изменения данных профиля выберите в меню или введите с клавиатуры команду /profile, после чего нажмите на кнопку \"Изменить имя\" или \"Изменить телефон\" и введите новые данные", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "WhatThree") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               await client.SendTextMessageAsync(message.Chat.Id, "*Как начать работать таксистом?*\n\nДля регистрации водительского профиля введите команду /driver и введите требуемые ботом данные. Вводимые данные должны быть достоверными. После заполнения всех необходимых данных нажмите на кнопку \"Отправить заявку\" и ожидайте её одобрения или отказа от администрации", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "WhatFour") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               await client.SendTextMessageAsync(message.Chat.Id, "*Как выполнять заказы таксистом?*\n\nДля получения новых заказов требуется выйти на смену, это можно сделать нажатием на кнопку \"Начать смену\" в профиле таксиста кнопкой меню или вводом команды /drivermenu, выход со смены осуществляется аналогичным образом. При получения заказа вы сможете посмотреть стартовую и конечную точку посредством нажатия на геопозицию и принять, либо отказаться от заказа. После принятия заказа маршрут до стартовой точки можно открыть нажатием на геопозицию, которую отправил бот и выбрать удобное вам приложения с картой, маршрут построится автоматически. При прибытии на стартовую точку требуется нажать на кнопку \"Машина подана\", клиенту отобразится сообщени о том, что вы его ожидание в указанной им точке. После того, как клиент сядет в автомобиль нажмите на кнопку \"Начать поездку\", отобразится сумма, которую вы получите после выполнения заказа. Маршрут до конечной точки можно посмотреть так же как и стартовую, обычным нажатием на геопозицию, которую отправил бот и открытием маршрута в удобном вам приложении с картой. По достижении конечной точки и получения оплаты от клиента нажмите кнопку \"Завершить заказ\", после нажатия на эту кнопку заказ считается завершенным, а пользователь сможет поставить вам оценку от 1 до 5, которая будет учитываться в водительском профиле (/drivermenu)", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "WhatFive") {
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               await client.SendTextMessageAsync(message.Chat.Id, "*Как изменить данные таксиста?*\n\nДля изменения данных в профиле таксиста требуется отбратиться в техническую поддержку нажатием в меню или вводом с клавиатуры /support. Опишите какие данные нужно изменить и на что, после чего отправьте сообщение кнопкой", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else if (data == "OrderTaxi") {
               Connect.LoadWork(works);
               Connect.LoadOrder(orders);
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               var order = orders.Find(x => x.id_client == message.Chat.Id.ToString() && x.id_driver == "" && x.status == "wait");
               string category = order.tarif;
               string id = order.id.ToString();
               if (works.Count > 0) {
                  Connect.LoadUser(users);
                  Connect.LoadDriver(drivers);
                  var user = users.Find(x => x.id == message.Chat.Id.ToString());
                  if (order != null && user != null) {
                     InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                     InlineKeyboardMarkup driverButton = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Принять", "AcceptOrderDriver|" + order.id) }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отказаться", "cancelOrderDriver|" + order.id) } });
                     var find = await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОжидайте, ищем для вас машину", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                     string canceled = string.Empty;
                     RouteData route = GetWayTime(order.start_longitude.ToString().Replace(",", "."), order.start_latitude.ToString().Replace(",", "."), order.end_logitude.ToString().Replace(",", "."), order.end_latitude.ToString().Replace(",", "."));
                     int i = 0, time = 0, change = 0;
                     while (true) {
                        try {
                           if (time >= 180) {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению, не удалось найти машину для вас", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              Connect.Query("delete from `Order` where id = " + order.id + ";");
                              try {
                                 await client.DeleteMessageAsync(message.Chat.Id, find.MessageId);
                              } catch { }
                              return;
                           }
                           Work worker = works[i];
                           List<Work> freeWorkers = new List<Work>();
                           bool checkFree = false;
                           if (canceled != "") {
                              for (int j = 0; j < works.Count; j++) {
                                 for (int k = 0; k < canceled.Split(' ').Length - 1; k++) {
                                    if (works[j].id_driver == canceled.Split(' ')[k]) {
                                       checkFree = false;
                                       break;
                                    }
                                    checkFree = true;
                                 }
                                 if (checkFree == true) freeWorkers.Add(works[j]);
                              }
                           }
                           else
                              freeWorkers = works;
                           freeWorkers = freeWorkers.FindAll(x => x.category.Contains(category));
                           if (freeWorkers.Count > 0) {
                              if (!canceled.Contains(worker.id_driver) && worker.status == "free" && worker.category.Contains(order.tarif)) {
                                 ChangeStatusDriver(worker, "wait");
                                 var search_1 = await client.SendTextMessageAsync(worker.id_driver, "*Новый заказ*\n\nДлина маршрута: " + route.distance + " км.\nВремя пути: " + route.time + " мин.\n\nИмя: " + user.username + "\nНомер телефона: " + user.phone, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 var search_2 = await client.SendLocationAsync(worker.id_driver, (float)order.start_latitude, (float)order.start_longitude);
                                 var search_3 = await client.SendLocationAsync(worker.id_driver, (float)order.end_latitude, (float)order.end_logitude);
                                 var search_4 = await client.SendTextMessageAsync(worker.id_driver, "🚖 Выберите действие", replyMarkup: driverButton);
                                 Connect.Query("update `Work` set messages = '" + search_1.MessageId + " " + search_2.MessageId + " " + search_3.MessageId + " " + search_4.MessageId + " ' where id = " + worker.id + ";");
                                 for (int j = 0; j < 25; j++) {
                                    try {
                                       if (change == 0) {
                                          await client.EditMessageTextAsync(message.Chat.Id, find.MessageId, "*Вызов такси* 〽️\n\nОжидайте, ищем для вас машину", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                                          change = 1;
                                       }
                                       else {
                                          await client.EditMessageTextAsync(message.Chat.Id, find.MessageId, "*Вызов такси*\n\nОжидайте, ищем для вас машину", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                                          change = 0;
                                       }
                                    } catch { }
                                    Connect.LoadOrder(orders);
                                    order = orders.Find(x => x.id.ToString() == id);
                                    if (orders.Find(x => x.id.ToString() == id && x.id_driver == worker.id_driver) != null) { // accept order
                                       string messages = search_1.MessageId + " " + search_2.MessageId + " " + search_3.MessageId + " " + search_4.MessageId + " ";
                                       Connect.Query("update `Order` set date = '" + DateTime.Now + "', price = '" + route.price + "' where id = " + order.id + ";");
                                       var driverInfo = drivers.Find(x => x.id == worker.id_driver);
                                       try {
                                          await client.DeleteMessageAsync(message.Chat.Id, find.MessageId);
                                       } catch { }
                                       Connect.LoadDriver(drivers);
                                       var drive = drivers.Find(x => x.id == worker.id_driver);
                                       string rating = string.Empty;
                                       if (drive != null) {
                                          rating = GetRating(drive);
                                          var userMessage = await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОжидайте водителя на стартовой точке\nИмя: " + driverInfo.fio.Split(' ')[1] + "\nАвтомобиль: " + driverInfo.auto + " (" + driverInfo.plate_number + ")\nРейтинг водителя: " + rating + "\nВремя пути: " + route.time + " минут\nДистанция: " + route.distance + " км.", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                                          messages += userMessage.MessageId;
                                          await client.SendTextMessageAsync(message.Chat.Id, "🗣 Вы находитесь в общем чате с водителем, он увидит все что вы напишите");
                                       }
                                       else {
                                          var userMessage = await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nОжидайте водителя на стартовой точке\nИмя: " + driverInfo.fio.Split(' ')[1] + "\nАвтомобиль: " + driverInfo.auto + " (" + driverInfo.plate_number + ")\nВремя пути: " + route.time + " минут\nДистанция: " + route.distance + " км.", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                                          messages += userMessage.MessageId;
                                       }
                                       Connect.Query("update `Work` set messages = '" + messages + "' where id = " + worker.id + ";");
                                       Connect.LoadWork(works);
                                       worker = works.Find(x => x.id == worker.id);
                                       WorkList("next", worker, null);
                                       ChangeStatusDriver(worker, "work");
                                       if (order != null) {
                                          try {
                                             InlineKeyboardMarkup driverKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚕 Машина подана", "WaitClientDriver") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить заказ", "BreakOrder") } });
                                             await client.EditMessageTextAsync(order.id_driver, Convert.ToInt32(worker.messages.Split(' ')[3]), "🚖 Направляйтесь к клиенту по первой геопозиции", replyMarkup: driverKey);
                                             await client.SendTextMessageAsync(order.id_driver, "🗣 Вы находитесь в общем чате с клиентом, он увидит все что вы напишите");
                                          } catch { }
                                       }
                                       return;
                                    }
                                    else if (orders.Find(x => x.id.ToString() == id && x.id_driver == "cancel") != null) { // cancel
                                       try {
                                          await client.DeleteMessageAsync(worker.id_driver, search_1.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_2.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_3.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_4.MessageId);
                                       } catch { }
                                       ChangeStatusDriver(worker, "free");
                                       Connect.Query("update `Order` set id_driver = '' where id = " + id + ";");
                                       i++;
                                       if (i >= works.Count) i = 0;
                                       canceled = worker.id_driver + " ";
                                       break;
                                    }
                                    else if (orders.Find(x => x.id.ToString() == id) == null) {  // отмена заказа пользователем
                                       ChangeStatusDriver(worker, "free");
                                       try {
                                          await client.DeleteMessageAsync(worker.id_driver, search_1.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_2.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_3.MessageId);
                                          await client.DeleteMessageAsync(worker.id_driver, search_4.MessageId);
                                       } catch { }
                                       return;
                                    }
                                    await Task.Delay(1000);
                                    time += 1;
                                 }
                                 Connect.LoadOrder(orders);
                                 if (orders.Find(x => x.id.ToString() == id) != null) {// ignore
                                    try {
                                       await client.DeleteMessageAsync(worker.id_driver, search_1.MessageId);
                                       await client.DeleteMessageAsync(worker.id_driver, search_2.MessageId);
                                       await client.DeleteMessageAsync(worker.id_driver, search_3.MessageId);
                                       await client.DeleteMessageAsync(worker.id_driver, search_4.MessageId);
                                    } catch { }
                                    WorkList("next", worker, null);
                                    ChangeStatusDriver(worker, "free");
                                    canceled = worker.id_driver + " ";
                                 }
                              }
                              else {
                                 await Task.Delay(1500);
                                 time += 1;
                              }
                           }
                           else {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению, не удалось найти машину для вас", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              Connect.Query("delete from `Order` where id = " + id + ";");
                              return;
                           }
                        } catch {
                           await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению, не удалось найти машину для вас", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           Connect.Query("delete from `Order` where id = " + id + ";");
                        }
                     }
                  }
                  else {
                     await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению, нет ни одного водителя на линии", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     Connect.Query("delete from `Order` where id = " + id + ";");
                  }
               }
               else {
                  await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nК сожалению, нет ни одного водителя на линии", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  Connect.Query("delete from `Order` where id = " + id + ";");
               }
            }
            else if (data.Contains("AcceptOrderDriver|")) {
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
               string id = data.Split('|')[1];
               Connect.Query("update `Order` set id_driver = '" + message.Chat.Id + "', status = 'work' where id = " + id + ";");
            }
            else if (data.Contains("cancelOrderDriver|")) {
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
               string id = data.Split('|')[1];
               Connect.LoadWork(works);
               var driver = works.Find(x => x.id_driver == message.Chat.Id.ToString());
               try {
                  if (driver != null) {
                     await client.DeleteMessageAsync(message.Chat.Id, Convert.ToInt32(driver.messages.Split(' ')[0]));
                     await client.DeleteMessageAsync(message.Chat.Id, Convert.ToInt32(driver.messages.Split(' ')[1]));
                     await client.DeleteMessageAsync(message.Chat.Id, Convert.ToInt32(driver.messages.Split(' ')[2]));
                     await client.DeleteMessageAsync(message.Chat.Id, Convert.ToInt32(driver.messages.Split(' ')[3]));
                  }
               } catch { }
               Connect.Query("update `Order` set id_driver = 'cancel' where id = " + id + "; update `Work` set status = 'free', messages = '' where id = " + driver.id + ";");
            }
            else if (data == "WaitClientDriver") {
               Connect.LoadOrder(orders);
               var order = orders.Find(x => x.id_driver == message.Chat.Id.ToString() && x.status == "work");
               if (order != null) {
                  var driver = works.Find(x => x.id_driver == message.Chat.Id.ToString());
                  if (driver != null) {
                     InlineKeyboardMarkup driverButton = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚕 Начать поездку", "StartDrive") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить заказ", "BreakOrder") } });
                     InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
                     await client.EditMessageTextAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[3]), "🚖 Ожидайте клиента", replyMarkup: driverButton);
                     var driverInfo = drivers.Find(x => x.id == order.id_driver);
                     if (driverInfo != null) {
                        string rating = GetRating(driverInfo);
                        RouteData route = GetWayTime(order.start_longitude.ToString().Replace(",", "."), order.start_latitude.ToString().Replace(",", "."), order.end_logitude.ToString().Replace(",", "."), order.end_latitude.ToString().Replace(",", "."));
                        await client.EditMessageTextAsync(order.id_client, Convert.ToInt32(driver.messages.Split(' ')[4]), "*Вызов такси*\n\nИмя: " + driverInfo.fio.Split(' ')[1] + "\nАвтомобиль: " + driverInfo.auto + " (" + driverInfo.plate_number + ")\nРейтинг водителя: " + rating + "\nВремя пути: " + route.time + " минут\nДистанция: " + route.distance + " км.\n\n🚖 *Водитель ожидает вас в месте назначения*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                     }
                  }
               }
            }
            else if (data.Contains("BreakOrder")) {
               Connect.LoadOrder(orders);
               var order = orders.Find(x => x.id_driver == message.Chat.Id.ToString() && x.status != "success" && x.status != "cancel");
               if (order != null) {
                  if (data == "BreakOrder")
                     Connect.Query("update `Order` set status = 'cancel' where id = " + order.id + ";");
                  var driver = works.Find(x => x.id_driver == order.id_driver);
                  if (driver != null) {
                     Connect.Query("update `Work` set messages = '' where id = " + driver.id + ";");
                     try {
                        await client.DeleteMessageAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[0]));
                        await client.DeleteMessageAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[1]));
                        await client.DeleteMessageAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[2]));
                        await client.DeleteMessageAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[3]));
                        if (data != "BreakOrderSuccess")
                           await client.EditMessageReplyMarkupAsync(order.id_client, Convert.ToInt32(driver.messages.Split(' ')[4]), replyMarkup: null);
                     } catch { }
                     ChangeStatusDriver(driver, "free");
                     if (data == "BreakOrderSuccess") {
                        Connect.LoadDriver(drivers);
                        var drive = drivers.Find(x => x.id == order.id_driver);
                        if (drive != null) {
                           RouteData route = GetWayTime(order.start_longitude.ToString().Replace(",", "."), order.start_latitude.ToString().Replace(",", "."), order.end_logitude.ToString().Replace(",", "."), order.end_latitude.ToString().Replace(",", "."));
                           try {
                              string rating = GetRating(drive);
                              await client.EditMessageTextAsync(order.id_client, Convert.ToInt32(driver.messages.Split(' ')[4]), "*Вызов такси*\n\nИмя: " + drive.fio.Split(' ')[1] + "\nАвтомобиль: " + drive.auto + " (" + drive.plate_number + ")\nРейтинг водителя: " + rating + "\nВремя пути: " + route.time + " минут\nДистанция: " + route.distance + " км.\n\n🚖 *Поездка окончена*", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           } catch { }
                           int count = Convert.ToInt32(drive.ride_count) + 1;
                           Connect.Query("update `Order` set status = 'success' where id = " + order.id + "; update `Driver` set ride_count = '" + count + "' where id = '" + order.id_driver + "';");
                           InlineKeyboardMarkup gradekey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", "Grade|1|" + order.id), InlineKeyboardButton.WithCallbackData("2", "Grade|2|" + order.id), InlineKeyboardButton.WithCallbackData("3", "Grade|3|" + order.id), InlineKeyboardButton.WithCallbackData("4", "Grade|4|" + order.id), InlineKeyboardButton.WithCallbackData("5", "Grade|5|" + order.id) } });
                           await client.SendTextMessageAsync(order.id_driver, "*Заказ*\n\nЗаказ успешно выполнен\nПрибыль с заказа: " + order.price + " ₽", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           await client.SendTextMessageAsync(order.id_client, "*Заказ*\n\nЗаказ успешно завершен\nОцените поездку от 1 до 5 кнопкой ниже", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: gradekey);
                           //int profit = Convert.ToInt32(order.price) - Convert.ToInt32(order.price) / 100 * 25;
                           double companyProfit = Convert.ToDouble(order.price) / 100 * 25;
                           await client.SendTextMessageAsync(-1001860678113, "Водитель: " + drivers.Find(x => x.id == order.id_driver).fio + " (`" + order.id_driver + "`)\nКлиент: " + users.Find(x => x.id == order.id_client).username + " (`" + order.id_client + "`)\nВремя пути: " + route.time + " мин.\nДистанция: " + route.distance + "км.\nСумма: " + order.price + " ₽\n\n`" + order.date + "`", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                     }
                     else {
                        await client.SendTextMessageAsync(order.id_driver, "*Заказ*\n\nЗаказ отменен", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        await client.SendTextMessageAsync(order.id_client, "*Вызов такси*\n\nЗаказ отменен", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     }
                  }
               }
            }
            else if (data == "StartDrive") {
               Connect.LoadOrder(orders);
               var order = orders.Find(x => x.id_driver == message.Chat.Id.ToString() && x.status == "work");
               if (order != null) {
                  var driver = works.Find(x => x.id_driver == message.Chat.Id.ToString());
                  if (driver != null) {
                     InlineKeyboardMarkup driverButton = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🚕 Завершить поездку", "BreakOrderSuccess") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить заказ", "BreakOrder") } });
                     await client.EditMessageTextAsync(order.id_driver, Convert.ToInt32(driver.messages.Split(' ')[3]), "🚖 Направляйтесь к пункту назначения по второй геопозиции\n\nПрибыль за выполненный заказ: " + order.price + " ₽", replyMarkup: driverButton);
                     var driverInfo = drivers.Find(x => x.id == order.id_driver);
                     if (driverInfo != null) {
                        string rating = GetRating(driverInfo);
                        RouteData route = GetWayTime(order.start_longitude.ToString().Replace(",", "."), order.start_latitude.ToString().Replace(",", "."), order.end_logitude.ToString().Replace(",", "."), order.end_latitude.ToString().Replace(",", "."));
                        await client.EditMessageTextAsync(order.id_client, Convert.ToInt32(driver.messages.Split(' ')[4]), "*Вызов такси*\n\nИмя: " + driverInfo.fio.Split(' ')[1] + "\nАвтомобиль: " + driverInfo.auto + " (" + driverInfo.plate_number + ")\nРейтинг водителя: " + rating + "\nВремя пути: " + route.time + " минут\nДистанция: " + route.distance + " км.\n\n🚖 *Счастливого пути!*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: null);
                     }
                  }
               }
            }
            else if (data.Contains("Grade")) {
               int grade = Convert.ToInt32(data.Split('|')[1]);
               string id = data.Split('|')[2];
               Connect.Query("update `Order` set grade = '" + grade + "' where id = " + id + ";");
               Connect.LoadOrder(orders);
               var driver = orders.Find(x => x.id.ToString() == id);
               if (driver != null) {
                  var globalGrade = orders.FindAll(x => x.id_driver == driver.id_driver && x.grade != "" && x.grade != null);
                  double rating = 0;
                  for (int i = 0; i < globalGrade.Count; i++)
                     rating += Convert.ToDouble(globalGrade[i].grade);
                  rating /= globalGrade.Count;
                  Connect.Query("update `Driver` set rating = '" + Math.Round(rating, 2) + "' where id = '" + driver.id_driver + "';");
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "⭐️ Спасибо за оценку!");
                  await client.SendTextMessageAsync(driver.id_driver, "⭐️ Ваш рейтинг изменился");
               }
            }
         } catch { }
      }

      private static async void UpdateData(object sender, UpdateEventArgs e)
      {
         try {
            var update = e.Update;
            if (update.ChannelPost != null) {
               if (update.ChannelPost.Chat.Id == -1001845851895) { // support
                  if (update.ChannelPost.ReplyToMessage != null) {
                     string number = update.ChannelPost.ReplyToMessage.Text.Split('\n')[0];
                     string chatId = update.ChannelPost.ReplyToMessage.Text.Split('\n')[^1];
                     Connect.Query("delete from `Answer` where id_message = '" + number.Replace("№", string.Empty) + "';");
                     await client.SendTextMessageAsync(chatId, "*Ответ от технической поддержки*\n\n" + update.ChannelPost.Text, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     try {
                        await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.MessageId);
                        await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.ReplyToMessage.MessageId);
                     } catch { }
                  }
                  else {
                     try {
                        await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.MessageId);
                     } catch { }
                  }
               }
               else if (update.ChannelPost.Chat.Id == -1001758234342) { // console
                  try {
                     await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.MessageId);
                  } catch { }
                  if (update.ChannelPost.Text.Contains(" ")) {
                     Connect.LoadDriver(drivers);
                     Connect.LoadUser(users);
                     string value = string.Empty;
                     if (update.ChannelPost.Text.Split(' ').Length >= 3) {
                        string id = update.ChannelPost.Text.Split(' ')[1];
                        var user = drivers.Find(x => x.id == id);
                        if (user != null) {
                           for (int i = 2; i < update.ChannelPost.Text.Split(' ').Length; i++)
                              value += update.ChannelPost.Text.Split(' ')[i] + " ";
                           value = value.Trim(' ');
                        }
                        if (update.ChannelPost.Text.Split(' ')[0] == "/changefio") {
                           if (user != null) {
                              Connect.Query("update `Driver` set fio = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/changephone") {
                           if (user != null) {
                              Connect.Query("update `Driver` set phone = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/changeexp") {
                           if (user != null) {
                              Connect.Query("update `Driver` set experiense = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/changeauto") {
                           if (user != null) {
                              Connect.Query("update `Driver` set auto = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/changeplate") {
                           if (user != null) {
                              Connect.Query("update `Driver` set plate_number = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/changestatus") {
                           if (user != null) {
                              Connect.Query("update `Driver` set status = '" + value + "' where id = '" + id + "';");
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                        else if (update.ChannelPost.Text.Split(' ')[0] == "/sendmessage") {
                           if (user != null) {
                              await client.SendTextMessageAsync(id, "*Сообщение от администрации*\n\n" + value, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           }
                        }
                     }
                     else if (update.ChannelPost.Text.Split(' ').Length == 2) {
                        if (update.ChannelPost.Text.Contains("/getuser")) {
                           var user = users.Find(x => x.id == update.ChannelPost.Text.Split(' ')[1]);
                           if (user != null) {
                              string request = "Телеграм: @" + user.telegram + "\nИмя: " + user.username + "\nТелефон: " + user.phone;
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text + "\n\n" + request);
                           }
                        }
                        else if (update.ChannelPost.Text.Contains("/getdriver")) {
                           var driver = drivers.Find(x => x.id == update.ChannelPost.Text.Split(' ')[1]);
                           var user = users.Find(x => x.id == update.ChannelPost.Text.Split(' ')[1]);
                           if (driver != null && user != null) {
                              string request = "Телеграм: @" + user.telegram + "\nФИО: " + driver.fio + "\nТелефон: " + driver.phone + "\nВодительский стаж: " + driver.experiense + "\nАвтомобиль: " + driver.auto + "\nГос. номер: " + driver.plate_number + "\nРейтинг: " + driver.rating + "\nКоличество поездок: " + driver.ride_count + "\nСтатус: " + driver.status;
                              await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text + "\n\n" + request);
                           }
                        }
                        else if (update.ChannelPost.Text.Contains("/sendmessageall")) {
                           Connect.LoadUser(users);
                           await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text);
                           for (int i = 0; i < users.Count; i++) {
                              await client.SendTextMessageAsync(users[i].id, "*Сообщение от администрации*\n\n" + value, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              await Task.Delay(1000);
                           }
                        }
                     }
                  }
                  else if (update.ChannelPost.Text == "/line") {
                     Connect.LoadDriver(drivers);
                     var driver = drivers.FindAll(x => x.status != "wait" && x.status != "sleep");
                     if (driver != null && driver.Count > 0) {
                        string request = "Водители на линии (" + driver.Count + "):\n";
                        for (int i = 0; i < driver.Count; i++)
                           request += driver[i].fio + " (" + driver[i].id + ")\n";
                        await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text + "\n\n" + request);
                     }
                     else await client.SendTextMessageAsync(update.ChannelPost.Chat.Id, "✅ " + update.ChannelPost.Text + "\n\nНа данный момент нет водителей на линии");
                  }
                  else if (update.ChannelPost.Text.Contains("/getdb")) {
                     string path = Path.GetFullPath("taxistar.db");
                     File.Copy(path, Path.GetFullPath("taxi.db"), true);
                     using (var stream = File.OpenRead(Path.GetFullPath("taxi.db"))) {
                        InputOnlineFile file = new InputOnlineFile(stream);
                        await client.SendDocumentAsync(update.ChannelPost.Chat.Id, file);
                     }
                     File.Delete(Path.GetFullPath("taxi.db"));
                  }
               }
               else if (update.ChannelPost.Chat.Id == -1001860678113) { // orders
                  try {
                     await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.MessageId);
                  } catch { }
               }
               else if (update.ChannelPost.Chat.Id == -1001528750159) { // drivers
                  try {
                     await client.DeleteMessageAsync(update.ChannelPost.Chat.Id, update.ChannelPost.MessageId);
                  } catch { }
               }
            }
         } catch { }
      }

#pragma warning disable IDE1006
      public class Map
      {
         public List<Route> routes { get; set; }
      }

      public class Route
      {
         public double duration { get; set; }
         public double distance { get; set; }
      }

      public class RouteData
      {
         public string distance { get; set; }
         public string time { get; set; }
         public int price { get; set; }
         public RouteData(string distance, string time, int price)
         {
            this.distance = distance;
            this.time = time;
            this.price = price;
         }
      }
#pragma warning restore IDE1006

      public static RouteData GetWayTime(string start_longitude, string start_latitude, string end_longitude, string end_latitude)
      {
         try {
            using WebClient wc = new WebClient();
            Map map = JsonConvert.DeserializeObject<Map>(wc.DownloadString("https://router.project-osrm.org/route/v1/car/" + start_longitude + "," + start_latitude + ";" + end_longitude + "," + end_latitude));
            int price = Convert.ToInt32(Math.Round(map.routes[0].distance / 1000, 1) * 25);
            if (price < 59) price = 59;
            RouteData route = new RouteData(Math.Round(map.routes[0].distance / 1000, 1).ToString(), Math.Round(map.routes[0].duration / 60 + (map.routes[0].duration / 60 / 100) * 25, 0).ToString(), price);
            return route;
         } catch { return null; }
      }

      public static void ChangeStatusDriver(Work driver, string status)
      {
         Connect.Query("update `Work` set status = '" + status + "' where id = " + driver.id + ";");
      }

      public static void WorkList(string operation, Work worker, Driver driver)
      {
         try {
            if (operation == "delete")
               Connect.Query("delete from `Work` where id = " + worker.id + ";");
            else if (operation == "add")
               Connect.Query("insert into `Work` (id_driver, status, messages, category) values ('" + driver.id + "', 'free', '', '" + driver.category + "');");
            else if (operation == "next") {
               Connect.Query("insert into `Work` (id_driver, status, messages, category) values ('" + worker.id_driver + "', '" + worker.status + "', '" + worker.messages + "', '" + worker.category + "');");
               WorkList("delete", worker, null);
            }
            Connect.LoadWork(works);
         } catch { }
      }

      public static void ChangeMessage(Telegram.Bot.Types.Message message, LastMessage msg, string value)
      {
         if (msg == null) Connect.Query("insert into 'LastMessage' (id, bot_message) values ('" + message.Chat.Id + "', '" + value + "');");
         else Connect.Query("update 'LastMessage' set bot_message = '" + value + "' where id = '" + message.Chat.Id + "';");
      }

      public async static void ChangeName(Telegram.Bot.Types.Message message, string type)
      {
         try {
            if (users.Find(x => x.id == message.Chat.Id.ToString()) == null)
               Connect.Query("insert into `User` (id, username, telegram) values ('" + message.Chat.Id + "', '" + message.Text + "', '" + message.From.Username + "');");
            else Connect.Query("update `User` set username = '" + message.Text + "', telegram = '" + message.From.Username + "' where id = '" + message.Chat.Id + "';");
            if (type == "reg") {
               await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация*\n\nВведите свой телефонный номер", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               Connect.Query("update `LastMessage` set bot_message = 'WaitPhone' where id = '" + message.Chat.Id + "';");
            }
            else if (type == "change") {
               await client.SendTextMessageAsync(message.Chat.Id, "*Редактирование профиля*\n\nВы успешно изменили имя", Telegram.Bot.Types.Enums.ParseMode.Markdown);
               Connect.Query("update `LastMessage` set bot_message = 'none' where id = '" + message.Chat.Id + "';");
               GetProfile(message);
            }
         } catch { }
      }

      public async static void ChangePhone(Telegram.Bot.Types.Message message, string type)
      {
         try {
            string ok = string.Empty, no = string.Empty; ;
            if (type == "reg") {
               ok = "*Регистрация*\n\nВы успешно зарегистрировались!\nИнструкция работы с ботом - /info";
               no = "*Регистрация*";
            }
            else if (type == "change") {
               ok = "*Редактирование профиля*\n\nВы успешно изменили номер телефона";
               no = "*Редактирование профиля*";
            }
            if (message.Text.Length == 11) {
               if (message.Text[0] == '8') {
                  string phone = message.Text[0] + " (" + message.Text[1] + message.Text[2] + message.Text[3] + ") " + message.Text[4] + message.Text[5] + message.Text[6] + "-" + message.Text[7] + message.Text[8] + "-" + message.Text[9] + message.Text[10];
                  Connect.Query("update `User` set phone = '" + phone + "' where id = '" + message.Chat.Id + "';");
                  Connect.Query("update `LastMessage` set bot_message = 'none' where id = '" + message.Chat.Id + "';");
                  await client.SendTextMessageAsync(message.Chat.Id, ok, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  if (type == "change") GetProfile(message);
               }
               else await client.SendTextMessageAsync(message.Chat.Id, no + "\n\nНомер телефона должен начинаться на 8 и состоять из 11 цифр, введите номер еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            else await client.SendTextMessageAsync(message.Chat.Id, no + "\n\nНомер телефона должен начинаться на 8 и состоять из 11 цифр, введите номер еще раз", Telegram.Bot.Types.Enums.ParseMode.Markdown);
         } catch { }
      }

      public async static void CallTaxi(Telegram.Bot.Types.Message message, Order order, string type)
      {
         InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Заказать такси", "OrderTaxi") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CancelTaxi") } });
         try {
            await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
         } catch { }
         Connect.Query("update `LastMessage` set bot_message = 'none' where id = " + message.Chat.Id + ";");
         if (order != null) {
            RouteData route = GetWayTime(order.start_longitude.ToString().Replace(",", "."), order.start_latitude.ToString().Replace(",", "."), order.end_logitude.ToString().Replace(",", "."), order.end_latitude.ToString().Replace(",", "."));
            if (type == "standart") type = "Стандарт";
            else if (type == "child") type = "Детский";
            else if (type == "cargo") type = "Грузовой";
            else if (type == "animal") type = "Перевозка животных";
            await client.SendTextMessageAsync(message.Chat.Id, "*Вызов такси*\n\nЦена: " + route.price + " ₽\nТариф: " + type + "\nНажмите требуемую кнопку", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
         }
      }

      public async static void GetProfile(Telegram.Bot.Types.Message message)
      {
         try {
            Connect.LoadUser(users);
            var user = users.Find(x => x.id == message.Chat.Id.ToString());
            if (user != null) {
               var keyborad = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⚙️ Изменить имя", "ChangeName") }, new[] { InlineKeyboardButton.WithCallbackData("⚙️ Изменить телефон", "ChangePhone") } });
               await client.SendTextMessageAsync(message.Chat.Id, "*Профиль*\n\nИмя: " + user.username + "\nНомер телефона: " + user.phone, Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyborad);
            }
         } catch { }
      }

      public static string[] GetCategory(string id)
      {
         try {
            string category = drivers.Find(x => x.id == id).category;
            string[] list = { "Стандарт", "Детский", "Перевозка животных", "Грузовой" };
            if (category.Contains("standart")) list[0] = "✅ Стандарт";
            else list[0] = "⛔️ Стандарт";
            if (category.Contains("child")) list[1] = "✅ Детский";
            else list[1] = "⛔️ Детский";
            if (category.Contains("animal")) list[2] = "✅ Перевозка животных";
            else list[2] = "⛔️ Перевозка животных";
            if (category.Contains("cargo")) list[3] = "✅ Грузовой";
            else list[3] = "⛔️ Грузовой";
            return list;
         } catch { return null; }
      }

      public static string GetRating(Driver drive)
      {
         try {
            if (drive.rating[0] == '0') return "0";
            else if (drive.rating[0] == '1') return "⭐️ (" + drive.rating + ")";
            else if (drive.rating[0] == '2') return "⭐️⭐️ (" + drive.rating + ")";
            else if (drive.rating[0] == '3') return "⭐️⭐️⭐️ (" + drive.rating + ")";
            else if (drive.rating[0] == '4') return "⭐️⭐️⭐️⭐️ (" + drive.rating + ")";
            else if (drive.rating[0] == '5') return "⭐️⭐️⭐️⭐️⭐️ (" + drive.rating + ")";
            return null;
         } catch { return null; }
      }

      private static Telegram.Bot.Types.IAlbumInputMedia[] GetMedia(string[] stringArray, string text)
      {
         var media = new Telegram.Bot.Types.IAlbumInputMedia[stringArray.Length];
         for (int i = 0; i < stringArray.Length; i++) {
            var mediaItem = new Telegram.Bot.Types.IAlbumInputMedia[1];
            if (i == 0) {
               mediaItem[0] = new Telegram.Bot.Types.InputMediaPhoto(stringArray[i]) { Caption = text };
            }
            else {
               mediaItem[0] = new Telegram.Bot.Types.InputMediaPhoto(stringArray[i]);
            }
            media[i] = mediaItem[0];
         }
         return media;
      }
   }
}
