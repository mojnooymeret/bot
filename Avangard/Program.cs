using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace Avangard
{
   internal class Program
   {
      readonly static InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } }); // клавиатура с кнопкой "Отмена"
      static long channel = -1001825737455; // id канала, куда отправляются заявки (получить id канала можно переслав сообщение из него в бота @myidbot)
      private static string token { get; set; } = "5973484194:AAFHcMFVCUc1DVI0zEo-XktqCGOD47HU5jo"; // токен бота (получается в @BotFather)
      private static TelegramBotClient client; // клиент бота, через который выполняются различные действия с ним
      static void Main()
      {
         client = new TelegramBotClient(token); // присваиваем клиенту токен нашего бота
         client.StartReceiving(); // запускаем бота
         client.OnMessage += ClientMessage; // устанавливаем обработчик входящих сообщений
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => { // устанавливаем обработчкик нажатий на кнопки
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine(); // бесконечно читаем строку чтобы бот не выключался
      }

      // ниже идут коллекции (листы), в которых хранятся подгружаемые данные из базы данных, название класса соответсвует названию таблице в БД
      static List<User> users = new List<User>();
      static List<Category> categories = new List<Category>();
      static List<Request> requests = new List<Request>();
      static List<Service> services = new List<Service>();
      static List<SubCategory> subCategories = new List<SubCategory>();
      static List<SubService> subServices = new List<SubService>();

      // обработчик входящих сообщений
      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message; // присваем в переменную message входящие данные для удобства (чтобы не прописывать e.)
            if (message.Text == "/start") { // если сообщение = /start
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null); // удаляем клавиатуру у предыдущего сообщения (message.Chat.Id - id пользователя (чата),
                                                                                                                       // message.MessageId - id сообщения в чате, replyMarkup - клавиатура, устанавливаем клавиатуре null
               } catch { }
               Connect.LoadUser(users); // подгружаем базу данных с пользователями в соответствующую коллекцию
               var user = users.Find(x => x.id == message.Chat.Id.ToString()); // ищем пользователя в полученных данных (если id = id пользователя, написавшего сообщение)
               if (user == null) // если пользователь не найден
                  Connect.Query("insert into `User` (id, message) values ('" + message.Chat.Id + "', 'none');"); // записываем его в БД, устанавливаем параметр message = 'none' (none - не ожидается никаких сообщений)
               InlineKeyboardMarkup keyboard = GetCategory(); // получаем клавиатуру со всеми категориями
               await client.SendTextMessageAsync(message.Chat.Id, "Здравствуйте! \n\nВас приветствует помощник компании Авангард! Какие услуги\n\nВас интересуют?", replyMarkup: keyboard); // отправляем сообщение с клавиатурой
            }
            else if (message.Text == "/menu") {
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
               } catch { }
               InlineKeyboardMarkup keyboard = GetCategory();
               await client.SendTextMessageAsync(message.Chat.Id, "Главная", replyMarkup: keyboard);
            }
            else {
               Connect.LoadUser(users);
               var user = users.Find(x => x.id == message.Chat.Id.ToString());
               if (user != null) {
                  if (user.message == "waitfio") { // если ожидаемое сообщение от пользователя (столбец message в БД) waitfio (ввод ФИО)
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                     } catch { }
                     Connect.LoadRequest(requests);
                     var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку в базе по соответствию id пользователя и дате "none", если дата "none", значит заявка формируется, если дата к примеру "01.01.2023 14:00:00", значит заявка сформирована и отправлена в канал
                     string fio = message.Text; // присваиваем отправленный текст в переменную
                     if (fio.Split(' ').Length == 3) { // если в введенном пробел разделяет текст на 3 элемент (а именно Фамилия Имя Отчество)
                        if (request != null && request.fio != "" && request.fio != null) // если заявка была найдена и записана в переменную request и фамилия в заявке уже заполнена (!= "" && != null)
                           EditData(request, message, "ФИО", "fio"); // отправляем текст на изменение в уже сформированной заявке
                        else { // если ФИО в заявке еще нет, тогда заполняем ее
                           Connect.Query("update `Request` set id_user = '" + message.Chat.Id + "', fio = '" + fio + "' where id_user = '" + message.Chat.Id + "' and date = 'none'; update `User` set message = 'waitphone' where id = '" + message.Chat.Id + "';"); // обновляем саму заявку в базе (таблица Request, поле fio), а также устанавливаем пользователю следующее ожидание сообщения телефона "waitphone" 
                           await client.SendTextMessageAsync(message.Chat.Id, "Введите свой номер телефона для связи", replyMarkup: cancel);
                        }
                     }
                     else // иначе уведомляем пользователя о том, что ФИО введено неверно и требуется ввести его еще раз в правильном виде
                        await client.SendTextMessageAsync(message.Chat.Id, "*Неверный формат ФИО*\n\nВведите своё ФИО (Пример: Иванов Иван Иванович", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                  }
                  else if (user.message == "waitphone") {
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                     } catch { }
                     string phone = message.Text.Replace(" ", string.Empty).Replace("+", string.Empty); // удаляем из присланного сообщения пробелы и +
                     if (phone.Length == 11 && phone[0] == '8' || phone[0] == '7') { // если введенный номер телефона содержит в себе 11 цифр, а также начинается с цифры 8 или 7
                        Connect.LoadRequest(requests);
                        var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем зявку пользователя
                        string number = request.phone; // записываем номер телефона из БД в переменную
                        if (request != null && request.phone != "" && request.phone != null) { // если заявка была найдена и записана в переменную request и номер телефона в заявке уже заполнен (!= "" && != null)
                           EditData(request, message, "Номер телефона", "phone"); // отправяем текст на изменение в уже сформированной заявке
                           return; // останавливаем выполнение функции
                        }
                        // если номера телефона в заявке еще нет, тогда заполняем его
                        Connect.Query("update `Request` set phone = '" + phone + "' where id_user = '" + message.Chat.Id + "' and date = 'none';"); // записываем номер телефона в БД
                        Connect.LoadRequest(requests);
                        request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку
                        if (request != null) { // если заявка найдена
                           if (request.id_service != "0") { // если услуга из категории
                              Connect.LoadService(services);
                              var service = services.Find(x => x.id.ToString() == request.id_service); // ищем выбранную услугу пользователем
                              if (service != null) { // если услуга найдена
                                 if (service.type.Contains("default")) { // если тип заполнения услуги "default" (требуется ввод адреса)
                                    await client.SendTextMessageAsync(message.Chat.Id, "Введите адрес, по которому требуется работа (Пример: ул. Мира, д. 4, п. 1, кв. 111)", replyMarkup: cancel);
                                    Connect.Query("update `User` set message = 'waitaddress' where id = '" + message.Chat.Id + "';");
                                 }
                                 else if (service.type.Contains("nonaddress")) { // иначе, если тип заполнения услуги "nonaddress" (не требуется ввод адреса)
                                    if (service.type.Contains("square")) { // если тип заполнения услуги включает в себя "square" (ввод метража)
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите метраж квартиры/участка в квадратах", replyMarkup: cancel);
                                       Connect.Query("update `User` set message = 'waitsquare' where id = '" + message.Chat.Id + "';"); // изменяем ожидание сообщения метража от пользователя
                                    }
                                    else { // иначе требуем ввод комментария
                                       InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipComment") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите комментарий к заявке при необходимости", replyMarkup: keyboard);
                                       Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';");
                                    }
                                 }
                              }
                           }
                           else { // если услуга из подкатегории
                              Connect.LoadSubService(subServices);
                              var subService = subServices.Find(x => x.id.ToString() == request.id_subservice); // ищем услугу
                              if (subService != null) { // если услуга найдена
                                 if (subService.type.Contains("default")) { // 103
                                    await client.SendTextMessageAsync(message.Chat.Id, "Введите адрес, по которому требуется работа (Пример: ул. Мира, д. 4, п. 1, кв. 111)", replyMarkup: cancel);
                                    Connect.Query("update `User` set message = 'waitaddress' where id = '" + message.Chat.Id + "';");
                                 }
                                 else if (subService.type.Contains("nonaddress")) { // 107
                                    if (subService.type.Contains("square")) {
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите метраж квартиры/участка в квадратах", replyMarkup: cancel);
                                       Connect.Query("update `User` set message = 'waitsquare' where id = '" + message.Chat.Id + "';");
                                    }
                                    else { // 112
                                       InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipComment") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите комментарий к заявке при необходимости", replyMarkup: keyboard);
                                       Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';");
                                    }
                                 }
                              }
                           }
                        }

                     }
                     else // иначе уведомляем пользователя и неверном формате номера телефона и требуем повторный ввод
                        await client.SendTextMessageAsync(message.Chat.Id, "*Неверный формат телефона*\n\nВведите свой номер телефона для связи (Например: 89995553322)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                  }
                  else if (user.message == "waitaddress") {
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                     } catch { }
                     if (message.Text.Split(',').Length == 4) { // если символ "," делит сообдщение на 4 элемента (улица, дом, подъезд, квартира)
                        if (message.Text.Split(',')[0].Contains("ул.") && message.Text.Split(',')[1].Contains("д.") && message.Text.Split(',')[2].Contains("п. ") && message.Text.Split(',')[3].Contains("кв. ")) {
                           Connect.LoadRequest(requests);
                           var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // достаем текущий адрес заявки
                           string address = request.address; // присваем текущий адрес в переменную
                           Connect.Query("update `Request` set address = '" + message.Text + "' where id_user = '" + message.Chat.Id + "' and date = 'none';"); // записываем новый адрес в перменную
                           Connect.LoadRequest(requests);
                           request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку
                           if (request != null) {
                              if (address != null && address != "") // если адрес уже заполнен, значит пользователь редактирует его
                                 EditData(request, message, "Адрес", "address"); // отправляем на редактирование адрес
                              else { // если адрес не заполнен, значит пользователь формирует новую заявку
                                 Connect.LoadService(services);
                                 var service = services.Find(x => x.id.ToString() == request.id_service); // ищем заявку
                                 if (service != null) { // если услуга найдена в категории
                                    if (service.type.Contains("square")) { // 108
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите метраж квартиры/участка в квадратах", replyMarkup: cancel);
                                       Connect.Query("update `User` set message = 'waitsquare' where id = '" + message.Chat.Id + "';"); // 110
                                    }
                                    else { // 112
                                       InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipComment") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                                       await client.SendTextMessageAsync(message.Chat.Id, "Введите комментарий к заявке при необходимости", replyMarkup: keyboard);
                                       Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';"); // 110
                                    }
                                 }
                                 else { // ищем услугу в подкатегории
                                    Connect.LoadSubService(subServices);
                                    var subService = subServices.Find(x => x.id.ToString() == request.id_subservice);
                                    if (subService != null) {
                                       if (subService.type.Contains("square")) { // 108
                                          await client.SendTextMessageAsync(message.Chat.Id, "Введите метраж квартиры/участка в квадратах", replyMarkup: cancel);
                                          Connect.Query("update `User` set message = 'waitsquare' where id = '" + message.Chat.Id + "';"); // 110
                                       }
                                       else { // 112
                                          InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipComment") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                                          await client.SendTextMessageAsync(message.Chat.Id, "Введите комментарий к заявке при необходимости", replyMarkup: keyboard);
                                          Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';"); // 110
                                       }
                                    }
                                 }
                              }
                           }
                        } // иначе уведомляем пользователя и неверном формате адреса и требуем повторный ввод
                        else await client.SendTextMessageAsync(message.Chat.Id, "*Неверный формат*\n\nВведите адрес, по которому требуется работа (Пример: ул. Мира, д. 4, п. 1, кв. 111)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                     } // иначе уведомляем пользователя и неверном формате адреса и требуем повторный ввод
                     else await client.SendTextMessageAsync(message.Chat.Id, "*Неверный формат*\n\nВведите адрес, по которому требуется работа (Пример: ул. Мира, д. 4, п. 1, кв. 111)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                  }
                  else if (user.message == "waitsquare") {
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                     } catch { }
                     try {
                        var square = Convert.ToDouble(message.Text); // если отправленное сообщение конвертируется в double (число), значит оно является числом
                        Connect.LoadRequest(requests);
                        var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку
                        if (request.square != null && request.square != "") // если метраж в базе уже есть, значит пользователь его редактирует на новый
                           EditData(request, message, "Метраж", "square");
                        else {
                           Connect.Query("update `Request` set square = '" + square + "' where id_user = '" + message.Chat.Id + "' and date = 'none';"); // вводим метраж в базу
                           InlineKeyboardMarkup key = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Пропустить", "SkipComment") }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });
                           await client.SendTextMessageAsync(message.Chat.Id, "Введите комментарий к заявке при необходимости", replyMarkup: key);
                           Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';"); // ожидаем комментарий
                        }
                     } catch { // если отправленное сообщение не было конвертировано в double (число), значит пользователь ввел некорректный метраж
                        await client.SendTextMessageAsync(message.Chat.Id, "Неверный формат. Введите метраж квартиры/участка в квадратах (Например: 52)", replyMarkup: cancel);
                     }
                  }
                  else if (user.message == "waitcomment") {
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1, replyMarkup: null);
                     } catch { }
                     Connect.LoadRequest(requests);
                     var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку
                     if (request.comment != null && request.comment != "") // если комментарий уже заполнен, значит пользователь редактирует его на новый
                        EditData(request, message, "Комментарий", "comment");
                     else { // иначе заполняем в базу входящее сообщение
                        Connect.Query("update `Request` set comment = '" + message.Text + "' where id_user = '" + message.Chat.Id + "' and date = 'none';");
                        SkipComment(message); // вызываем функцию отправки предпросмотра и редактирования заявки пользователю
                     }
                  }
               }
            }
         } catch { }
      }

      // функция для редактирования данных в заявке (request - заявка из БД, message - сообщение пользователя, name - переменная для вывода пользователю (например: "Комментарий" успешно изменен), row - изменяемый столбец в таблице БД)
      private static async void EditData(Request request, Telegram.Bot.Types.Message message, string name, string row)
      {
         try {
            Connect.Query("update `Request` set " + row + " = '" + message.Text + "' where id = " + request.id + "; update `User` set message = 'none' where id = '" + message.Chat.Id + "';"); // отправляем запрос на изменение в базу и перестаем ждать от пользователя сообщение (message = 'none')
            await client.SendTextMessageAsync(message.Chat.Id, "✅ " + name + " успешно изменен"); // уведомляем пользователя об успешном редактировании
            Connect.LoadRequest(requests); // подгружаем обновленную заявку для вывода пользователю
            request = requests.Find(x => x.id == request.id);
            await Task.Delay(200); // задержка 200 мс чтобы бот не флудил
            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Отправить заявку", "RequestSend_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Редактировать данные", "EditData_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Отмена", "Cancel") } }); // клавиатура для сообщения с заявкой
            await client.SendTextMessageAsync(message.Chat.Id, GetRequest(request), replyMarkup: keyboard); // отправляем обновленную заявку пользователю на просмотр и редактирование
         } catch { }
      }

      // обработчик нажатий на кнопки под сообщениями
      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message; // данные сообщения
            var data = ev.CallbackQuery.Data; // callback кнопки (идентификатор, по которому выполняются определенные функции)
            if (data.Contains("Category_")) { // если идентификатор содержит "Category_" (получение подкатегорий/услуг выбранной категории в меню (Главная)
               Connect.LoadCategory(categories);
               var category = categories.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем выбранную категорию (id категории передается после "_" (например Category_1, 1 - id категории в БД)
               Connect.LoadSubCategory(subCategories);
               var subs = subCategories.FindAll(x => x.id_category.ToString() == data.Split('_')[1]); // ищем подкатегории выбранной категории
               InlineKeyboardMarkup keyboard; // объявляем пустую клавиатуру
               if (subs.Count == 0) { // если подкатегорий в категории нет, то клавиатуру с услугами этой категории
                  Connect.LoadService(services);
                  var service = services.FindAll(x => x.id_category == category.id); // ищем услуги в категории
                  keyboard = GetService(service, false); // получаем клавиатуру с услугами
               }
               else // если подкатегории в категории есть
                  keyboard = GetSubCategory(subs); // получаем клавиатуру с категориями
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, category.name, replyMarkup: keyboard); // выводим результат сообщением пользователю
            }
            else if (data.Contains("CategorySub_")) { // получение услуг в подкатегории
               Connect.LoadSubCategory(subCategories);
               Connect.LoadSubService(subServices);
               var category = subCategories.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем подкатегорию для получения её названия
               var service = subServices.FindAll(x => x.id_subcategory.ToString() == data.Split('_')[1]); // ищем услугу в этой подкатегории
               InlineKeyboardMarkup keyboard = GetSubService(service); // получаем клавиатуру с услугами в подкатегории
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, category.name, replyMarkup: keyboard); // выводим результат в сообщении
            }
            else if (data.Contains("ServiceSub_")) { // получение информации о услуге в подкатегории
               Connect.LoadSubService(subServices);
               var service = subServices.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем услугу в подкатегориях
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithUrl("Подробнее", service.url) }, new[] { InlineKeyboardButton.WithCallbackData("Оставить заявку", "RequestSub_" + service.id + "_" + service.type) }, new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "CategorySub_" + service.id_subcategory) }, new[] { InlineKeyboardButton.WithCallbackData("⬅️ На главную", "MainCategory") } });
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, service.name + "\n\n" + service.desc, replyMarkup: keyboard);
            }
            else if (data.Contains("Service_")) { // получение информации о услуге в категории
               Connect.LoadService(services);
               var service = services.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем услугу в категории
               Connect.LoadSubCategory(subCategories);
               var sub = subCategories.Find(x => x.id_category.ToString() == data.Split('_')[1]); // ищем есть ли подкатегория у услуги
               InlineKeyboardMarkup keyboard;
               if (sub == null) // если подкатегории нет, то выводим клавиатуру с кнопкой "Назад" назначенную на категорию
                  keyboard = GetBackService(service, false);
               else// если подкатегория есть, то выводим клавиатуру с кнопкой "Назад" назначенную на подкатегорию
                  keyboard = GetBackService(service, true);
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, service.name + "\n\n" + service.desc, replyMarkup: keyboard);
            }
            else if (data.Contains("ServiceMain_")) { // получение услуг категории
               Connect.LoadService(services);
               var service = services.FindAll(x => x.id_category.ToString() == data.Split('_')[1]); // ищем услугИ
               InlineKeyboardMarkup keyboard = GetService(service, true); // получаем клавиатуру полученных услуг
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Услуги", replyMarkup: keyboard);
            }
            else if (data == "MainCategory") { // получение главной клавиатуры с категориями
               InlineKeyboardMarkup keyboard = GetCategory(); // получаем клавиатуру категорий
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Главная", replyMarkup: keyboard);
            }
            else if (data.Contains("Request_")) { // оформление заявки (услуга в категории)
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
               Connect.LoadService(services); // подгружаем услуги из БД
               var service = services.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем услугу
               if (service != null) { // если услуга найдена в БД, то ожидаем от пользователя ввода ФИО (message = 'waitfio'), а также создаем пустую заявку в базе (id_service - id услуги в категории, если услуга находится в подкатегории, то id_service = 0, и наоборот с id_subservice), date = 'none' для того, чтобы программа понимала, что эта заявка еще не была отправлена в канал и находится на стадии формирования, если date = (например) '14.02.2023 14:00:00', значит заявка сформирована и отправлена в канал
                  Connect.Query("update `User` set message = 'waitfio' where id = '" + message.Chat.Id + "'; insert into `Request` (id_user, id_service, id_subservice, date) values ('" + message.Chat.Id + "', " + service.id + ", 0, 'none');");
                  await client.SendTextMessageAsync(message.Chat.Id, "Введите своё ФИО", replyMarkup: cancel);
               }
            }
            else if (data.Contains("RequestSub_")) { // оформление заявки (услуга в ПОДкатегории)
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
               Connect.LoadSubService(subServices); // 311
               var service = subServices.Find(x => x.id.ToString() == data.Split('_')[1]); // 312
               if (service != null) { // 313
                  Connect.Query("update `User` set message = 'waitfio' where id = '" + message.Chat.Id + "'; insert into `Request` (id_user, id_service, id_subservice, date) values ('" + message.Chat.Id + "', 0, " + service.id + ", 'none');");
                  await client.SendTextMessageAsync(message.Chat.Id, "Введите своё ФИО", replyMarkup: cancel);
               }
            }
            else if (data == "Cancel") { // Отмена заполнения заявки
               Connect.Query("delete from `Request` where id_user = '" + message.Chat.Id + "' and date = 'none'; update `User` set message = 'none' where id = '" + message.Chat.Id + "';"); // удаление заявки из БД
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
               } catch { }
            }
            else if (data == "SkipComment") { // пропустить заполнение комментария
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               SkipComment(message); // вызываем функцию вывода заявки на экран
            }
            else if (data.Contains("RequestSend_")) { // отправить заявку в канал (капча)
               try {
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId);
               } catch { }
               Random rnd = new Random();
               string number = rnd.Next(1, 9).ToString(); // формируем рандомную цифру от 1 до 9
               // формируем клавиатуру с цифрами от 1 до 9, где в callback "SendCaptcha_1(сгенерированный номер)_1(номер кнопки)_idзаявки"
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("1", "SendCaptcha_" + number + "_1_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("2", "SendCaptcha_" + number + "_2_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("3", "SendCaptcha_" + number + "_3_" + data.Split('_')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("4", "SendCaptcha_" + number + "_4_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("5", "SendCaptcha_" + number + "_5_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("6", "SendCaptcha_" + number + "_6_" + data.Split('_')[1]) }, new[] { InlineKeyboardButton.WithCallbackData("7", "SendCaptcha_" + number + "_7_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("8", "SendCaptcha_" + number + "_8_" + data.Split('_')[1]), InlineKeyboardButton.WithCallbackData("9", "SendCaptcha_" + number + "_9_" + data.Split('_')[1]) } });
               await client.SendTextMessageAsync(message.Chat.Id, "Выберите цифру " + number, replyMarkup: keyboard);
            }
            else if (data.Contains("SendCaptcha_")) { // выбор цифры в капче
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
               if (data.Split('_')[2] == data.Split('_')[1]) { // если выбранная цифра совпадает с фактической (подробнее строка 345)
                  Connect.Query("update `Request` set date = '" + DateTime.Now + "' where id = " + data.Split('_')[3] + ";"); // то устанавливаем заявке дату текущую дату в время DateTime.Now
                  Connect.LoadRequest(requests);
                  var request = requests.Find(x => x.id.ToString() == data.Split('_')[3]); // получаем актуальную заявку с датой
                  if (request != null) { // если заявка найдена
                     string msg = GetRequest(request); // получаем текст заявки
                     InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Закрыть заявку", "RequestComplete") } });
                     await client.SendTextMessageAsync(channel, msg, replyMarkup: keyboard); // отправляем заявку в канал (channel - id канала (строка 13))
                     try {
                        await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
                     } catch { }
                     await client.SendTextMessageAsync(message.Chat.Id, "✅ Ваша заявка успешно отправлена, после назначения специалиста, он свяжется с вами по указанному номеру телефона");
                  }
               }
               else { // если выбрана неверная цифра, то удаляем заявку и уведомляем пользователя
                  Connect.Query("delete from `Request` where id = " + data.Split('_')[3] + ";");
                  await client.SendTextMessageAsync(message.Chat.Id, "⛔️ Выбрана неверная цифра, заявка отменена");
               }
            }
            else if (data.Contains("RequestComplete")) { // кнопка в канале "Закрыть заявку" - удаляем сообщение из канала, запись в БД остается
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               } catch { }
            }
            else if (data.Contains("EditData_")) { // кнопка редактирования заявки
               var request = requests.Find(x => x.id.ToString() == data.Split('_')[1]); // ищем заявку
               var keyboard = new List<InlineKeyboardButton[]>(); // создаем пустую клавиатуру
               if (request != null) { // если заявка найдена
                  keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("ФИО", "EditFio_" + request.id) }); // добавляем кнопку ФИО
                  keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Телефон", "EditPhone_" + request.id) }); // добавляем кнопку Телефон
                  if (request.address != "") // если в заявке заполнен адрес
                     keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Адрес", "EditAddress_" + request.id) }); // добавляем кнопку Адрес
                  if (request.square != "") // если в заявке заполнен метраж
                     keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Метраж", "EditSquare_" + request.id) }); // добавляем кнопку Метраж
                  if (request.comment != "") // если в заявке заполнен коммент
                     keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Комментарий", "EditComment_" + request.id) }); // добавляем кнопку Комментарий
                  keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "RequestBack_" + request.id) }); // добавляем кнопку Назад
               }
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Выберите данные для редактирования", replyMarkup: keyboard.ToArray());
            }
            else if (data.Contains("RequestBack_")) { // возврат в заявку из редактирования
               Connect.LoadRequest(requests);
               var request = requests.Find(x => x.id.ToString() == data.Split('_')[1]);
               if (request != null) {
                  InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Отправить заявку", "RequestSend_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Редактировать данные", "EditData_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Отмена", "Cancel") } });
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, GetRequest(request), replyMarkup: keyboard);
               }
            }
            else if (data.Contains("EditFio_")) { // изменение ФИО (ожидаем от пользователя ввод нового ФИО)
               Connect.Query("update `User` set message = 'waitfio' where id = '" + message.Chat.Id + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите новое ФИО");
            }
            else if (data.Contains("EditPhone_")) { // аналогия 401 строка
               Connect.Query("update `User` set message = 'waitphone' where id = '" + message.Chat.Id + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите новый номер телефона для связи");
            }
            else if (data.Contains("EditAddress_")) { // аналогия 401 строка
               Connect.Query("update `User` set message = 'waitaddress' where id = '" + message.Chat.Id + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите новый адрес, по которому требуется работа");
            }
            else if (data.Contains("EditComment_")) { // аналогия 401 строка
               Connect.Query("update `User` set message = 'waitcomment' where id = '" + message.Chat.Id + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите новый комментарий к заявке");
            }
            else if (data.Contains("EditSquare_")) { // аналогия 401 строка
               Connect.Query("update `User` set message = 'waitsquare' where id = '" + message.Chat.Id + "';");
               await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Введите новый метраж квартиры/участка в квадратах");
            }
         } catch { }
      }

      // функция получения текста заявки
      private static string GetRequest(Request request)
      {
         try {
            string title = string.Empty; // перменная названия услуги
            if (request.id_service != "0") { // если услуга в категории
               Connect.LoadService(services);
               title = services.Find(x => x.id.ToString() == request.id_service).name; // ищем услугу по id
            }
            else { // если услугу в подкатегории
               Connect.LoadSubService(subServices);
               title = subServices.Find(x => x.id.ToString() == request.id_subservice).name; // ищем услугу по id
            }
            string msg = "Заявка №" + request.id + "\n\n" + title + "\nФИО: " + request.fio + "\nТелефон: " + request.phone + "\n"; // формирование текста заявки
            if (request.address != "") // если адрес заполнен
               msg += "Адрес: " + request.address + "\n";
            if (request.square != "") // если метраж заполнен
               msg += "Метраж квартиры/участка: " + request.square + " м2\n";
            if (request.id_service != "0") { // если услуга в категории
               Connect.LoadService(services);
               var service = services.Find(x => x.id.ToString() == request.id_service);
               if (service != null) {
                  if (service.price.Contains("sub") && request.square != "") // если в услуге формула умножения метража на цену
                     msg += "Цена: " + Convert.ToInt32(request.square) * Convert.ToInt32(service.price.Split('=')[1]); // перемножаем
                  else
                     msg += service.price; // добавляем цену из базы
               }
            }
            else { // если услуга в подкатегории
               Connect.LoadSubService(subServices);
               var service = subServices.Find(x => x.id.ToString() == request.id_subservice);
               if (service != null)
                  if (service.price.Contains("sub") && request.square != "") // если в услуге формула умножения метража на цену
                     msg += "Цена: " + Convert.ToInt32(request.square) * Convert.ToInt32(service.price.Split('=')[1]) + "\n"; // перемножаем
                  else
                     msg += "Цена: " + service.price + "\n"; // добавляем цену из базы
            }
            if (request.comment != "")
               msg += "Комментарий: " + request.comment + "\n";
            return msg;
         } catch { return null; }
      }

      // вывод текста заявки на экран пользователю
      private static async void SkipComment(Telegram.Bot.Types.Message message)
      {
         try {
            Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "';");
            Connect.LoadRequest(requests);
            var request = requests.Find(x => x.id_user == message.Chat.Id.ToString() && x.date == "none"); // ищем заявку
            if (request != null) {
               InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("Отправить заявку", "RequestSend_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Редактировать данные", "EditData_" + request.id) }, new[] { InlineKeyboardButton.WithCallbackData("Отмена", "Cancel") } });
               await client.SendTextMessageAsync(message.Chat.Id, GetRequest(request), replyMarkup: keyboard);
            }
         } catch { }
      }

      // Получение клавиатуры с главными категориями
      private static InlineKeyboardButton[][] GetCategory()
      {
         try {
            Connect.LoadCategory(categories);
            var keyboard = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < categories.Count; i++)
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(categories[i].name, "Category_" + categories[i].id) }); // включаем в кнопку название (categories[i].name) и идентификатор (Category_(id категории))
            return keyboard.ToArray();
         } catch { return null; }
      }

      // Получение клавиатуры с подкатегориями определенной категории
      private static InlineKeyboardButton[][] GetSubCategory(List<SubCategory> data)
      {
         try {
            var keyboard = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < data.Count; i++)
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(data[i].name, "CategorySub_" + data[i].id) });
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Услуги ➡️", "ServiceMain_" + data[0].id_category) });
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ На главную", "MainCategory") });

            Connect.LoadService(services);
            return keyboard.ToArray();
         } catch { return null; }
      }

      // Получение клавиатуры с услугами категории
      private static InlineKeyboardButton[][] GetService(List<Service> data, bool back)
      {
         try {
            var keyboard = new List<InlineKeyboardButton[]>(); // пустая клавиатура
            for (var i = 0; i < data.Count; i++)
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(data[i].name, "Service_" + data[i].id) }); // аналогично строке 488
            if (back == true)
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "Category_" + data[0].id_category) }); // кнопка назад для возврата в категорию
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ На главную", "MainCategory") });
            Connect.LoadService(services);
            return keyboard.ToArray();
         } catch { return null; }
      }

      // Получение клавиатуры с услугами в подкатегории
      private static InlineKeyboardButton[][] GetSubService(List<SubService> data)
      {
         try {
            var keyboard = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < data.Count; i++)
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData(data[i].name, "ServiceSub_" + data[i].id) }); // аналогично строке 488
            Connect.LoadSubCategory(subCategories);
            var sub = subCategories.Find(x => x.id == data[0].id_subcategory);
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "Category_" + sub.id_category) }); // кнопка назад для возврата в категорию
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ На главную", "MainCategory") });
            Connect.LoadService(services);
            return keyboard.ToArray();
         } catch { return null; }
      }

      // Получение клавиатуры для перехода из услуг категории в её услуги
      private static InlineKeyboardButton[][] GetBackService(Service data, bool back)
      {
         try {
            var keyboard = new List<InlineKeyboardButton[]>();
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("Оставить заявку", "Request_" + data.id + "_" + data.type) });
            keyboard.Add(new[] { InlineKeyboardButton.WithUrl("Подробнее", data.url) });
            if (back == true) // если есть подкатегория
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "ServiceMain_" + data.id_category) });
            else // если нет подкатегории
               keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "Category_" + data.id_category) });
            keyboard.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ На главную", "MainCategory") });
            Connect.LoadService(services);
            return keyboard.ToArray();
         } catch { return null; }
      }
   }
}
