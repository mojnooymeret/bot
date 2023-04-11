using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.InputFiles;
using TL;

namespace Chanel
{
   internal class Program
   {
      private static string token { get; set; } = "5764441358:AAGeAkDB4DKv8g5qIGyDVh0sUPoYE7FzRvE";
      private static TelegramBotClient client;
      static async Task Main(string[] args)
      {
         client = new TelegramBotClient(token);
         await AuthAsync();
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         Console.ReadLine();
      }
      public static long channelId = -1001695523633;
      public static List<User> users = new List<User>();
      public static List<Chanel> chanels = new List<Chanel>();
      public static List<Post> posts = new List<Post>();

      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            if (message.Text == "/start") {
               Connect.LoadUser(users);
               var user = users.Find(x => x.id == message.Chat.Id.ToString());
               if (user == null)
                  Connect.Query("insert into `User` (id, message) values ('" + message.Chat.Id + "', 'none');");
            }
            else if (message.Text == "/channels") {
               await GetChanels(message, true, false);
            }
            else if (message.Text.Contains("/addchannel")) {
               if (message.Text.Contains(" ") && message.Text.Split(' ').Length == 4) {
                  string[] numbers = message.Text.Split(' ');
                  string request = string.Empty;
                  var ch = allches.Find(x => x.localId.ToString() == numbers[1]);
                  if (ch != null)
                     request += "insert into `Chanel` (id, topic, title) values ('" + ch.id + "', '" + numbers[2] + "|" + numbers[3] + "', '" + ch.title + "');\n";
                  Connect.Query(request);
                  await client.SendTextMessageAsync(message.Chat.Id, "✅ Каналы добалены");
               }
               else
                  await client.SendTextMessageAsync(message.Chat.Id, "⛔️ Неверный запрос (Пример: /addchannel [id канала от] [id темы от] [id темы в])");
            }
            else if (message.Text.Contains("/delchannel")) {
               Connect.LoadChannel(chanels);
               if (message.Text.Contains(" ")) {
                  string[] numbers = message.Text.Split(' ');
                  string request = string.Empty;
                  for (int i = 1; i < numbers.Length; i++) {
                     try {
                        if (chanels[Convert.ToInt32(numbers[i]) - 1] != null)
                           request += "delete from `Chanel` where id = " + chanels[Convert.ToInt32(numbers[i]) - 1].id + ";";
                     } catch { }
                  }
                  Connect.Query(request);
                  await client.SendTextMessageAsync(message.Chat.Id, "✅ Каналы удалены");
               }
            }
            else if (message.Text.Contains("/off") && message.Chat.Id == 885185553)
               Process.GetCurrentProcess().Kill();
            else if (message.Text == "/getdb") {
               string path = Path.GetFullPath("chanel.db");
               File.Copy(path, Path.GetFullPath("ch.db"), true);
               using (var stream = File.OpenRead(Path.GetFullPath("ch.db"))) {
                  InputOnlineFile file = new InputOnlineFile(stream);
                  await client.SendDocumentAsync(885185553, file);
               }
               File.Delete(Path.GetFullPath("ch.db"));
            }
         } catch { }
      }

      public static Messages_Chats chats = new Messages_Chats();
      public static async Task GetChanels(Telegram.Bot.Types.Message message, bool write, bool load)
      {
         try {
            Connect.LoadChannel(chanels);

            if (load == true)
               chats = await wclient.Messages_GetAllChats();
            int number = 1;
            allches.Clear();
            string response = "Список доступных каналов для парсинга:\n";
            foreach (var element in chats.chats) {
               try {
                  if (element.Value.ToString().Contains("Channel") || element.Value.ToString().Contains("Chat") || element.Value.ToString().Contains("Group")) {
                     ChatBase chat = element.Value;
                     allches.Add(new Allch(number, chat.ID.ToString(), null, chat.Title));
                     response += number + ". " + chat.Title + "\n";
                     number++;
                  }
               } catch { }
            }
            number = 1;
            if (chanels.Count > 0) {
               response += "\nСписок каналов находящихся в парсинге:\n";
               for (int i = 0; i < chanels.Count; i++) {
                  response += number + ". " + chanels[i].title + " (" + chanels[i].topic + ")\n";
                  number++;
               }
            }
            response += "\nДля добавления/удаления канала в/из парсинг(-а) введите /addchannel [id канала от] [id темы от] [id темы в] (номера можно вводить через пробел, например /addchannel 12 32 43)";
            if (write == true)
               await client.SendTextMessageAsync(message.Chat.Id, response);
         } catch { }
      }

      public static async Task GetMessage()
      {
         try {
            Connect.LoadChannel(chanels);
            List<Chanel> notdub = new List<Chanel>();
            for (int i = 0; i < chanels.Count; i++)
               if (notdub.Find(x => x.id == chanels[i].id) == null)
                  notdub.Add(new Chanel(chanels[i].id, chanels[i].topic, chanels[i].title));
            var chats = await wclient.Messages_GetAllChats();
            for (int i = 0; i < notdub.Count; i++) {
               try {
                  InputChannelBase channel = null;
                  try {
                     Chat tchat = (Chat)chats.chats[notdub[i].id];
                     channel = tchat.migrated_to;
                  } catch {
                     try {
                        Channel tchannel = (Channel)chats.chats[notdub[i].id];
                        channel = tchannel;
                     } catch { }
                  }
                  Messages_ForumTopics get = null;
                  if (channel != null)
                     get = await wclient.Channels_GetForumTopics(channel);
                  if (get != null) {
                     if (get.messages.Length < 1) return;
                     foreach (var msgBase in get.messages) {
                        try {
                           if (msgBase is Message msg) {
                              Message message = (Message)msgBase;
                              Connect.LoadPost(posts);
                              var chan = chanels.Find(x => x.id.ToString() == message.peer_id.ID.ToString() && x.topic.Split('|')[0] == message.reply_to.reply_to_msg_id.ToString());
                              if (chan != null) {
                                 var post = posts.Find(x => x.id.Split('|')[0] == chan.id.ToString() && x.id.Split('|')[1] == message.ID.ToString());
                                 if (post == null) {
                                    if (message.media == null || message.media.ToString() == "TL.MessageMediaWebPage") {
                                       using (var http = new HttpClient()) {
                                          string url = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={channelId}&message_thread_id={chan.topic.Split('|')[1]}&text={message.message}";
                                          await http.PostAsync(url, null);
                                          await Task.Delay(2500);
                                       }
                                       Connect.Query("insert into `Post` (id) values ('" + chan.id + "|" + message.ID + "');");
                                    }
                                    else {
                                       string filename = string.Empty;
                                       try {
                                          MessageMediaPhoto media = (MessageMediaPhoto)message.media;
                                          if (media.photo != null) {
                                             Photo photo = (Photo)media.photo;
                                             filename = $"{photo.id}.jpg";
                                             using var fileStream = File.Create(filename);
                                             var type = await wclient.DownloadFileAsync(photo, fileStream);
                                             fileStream.Close();
                                          }
                                          if (File.Exists(Path.GetFullPath(filename))) {
                                             byte[] bData = File.ReadAllBytes(Path.GetFullPath(filename));
                                             string url = "https://api.telegram.org/bot" + token + "/sendPhoto?chat_id=" + channelId + "&message_thread_id=" + chan.topic.Split('|')[1] + "&caption=" + message.message;
                                             using (var http = new HttpClient()) {
                                                using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture))) {
                                                   content.Add(new StreamContent(new MemoryStream(bData)), "photo", filename);
                                                   using (
                                                      var send = await http.PostAsync(url, content)) {
                                                      await Task.Delay(2500);
                                                   }
                                                }
                                             }
                                             Connect.Query("insert into `Post` (id) values ('" + chan.id + "|" + message.ID + "');");
                                             File.Delete(Path.GetFullPath(filename));
                                          }
                                       } catch {
                                          try {
                                             MessageMediaDocument document = (MessageMediaDocument)message.media;
                                             if (document.document != null) {
                                                Document doc = (Document)document.document;
                                                filename = doc.Filename;
                                                filename ??= $"{doc.id}.{doc.mime_type[(doc.mime_type.IndexOf('/') + 1)..]}";
                                                using var fileStream = File.Create(filename);
                                                await wclient.DownloadFileAsync(doc, fileStream);
                                                fileStream.Close();
                                             }
                                             if (File.Exists(Path.GetFullPath(filename))) {
                                                Document doc = (Document)document.document;
                                                byte[] bData = File.ReadAllBytes(Path.GetFullPath(filename));
                                                string url = string.Empty, type = string.Empty;
                                                if (doc.mime_type.Contains("ogg")) {
                                                   url = "https://api.telegram.org/bot" + token + "/sendVoice?chat_id=" + channelId + "&message_thread_id=" + chan.topic.Split('|')[1] + "&caption=" + message.message;
                                                   type = "voice";
                                                }
                                                else if (doc.mime_type.Contains("mpeg") || doc.mime_type.Contains("mp3")) {
                                                   url = "https://api.telegram.org/bot" + token + "/sendAudio?chat_id=" + channelId + "&message_thread_id=" + chan.topic.Split('|')[1] + "&caption=" + message.message;
                                                   type = "audio";
                                                }
                                                else {
                                                   url = "https://api.telegram.org/bot" + token + "/sendVideo?chat_id=" + channelId + "&message_thread_id=" + chan.topic.Split('|')[1] + "&caption=" + message.message;
                                                   type = "video";
                                                }
                                                using (var http = new HttpClient()) {
                                                   using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(System.Globalization.CultureInfo.InvariantCulture))) {
                                                      content.Add(new StreamContent(new MemoryStream(bData)), type, filename);
                                                      using (
                                                         var send = await http.PostAsync(url, content)) {
                                                         await Task.Delay(2500);
                                                      }
                                                   }
                                                }
                                                Connect.Query("insert into `Post` (id) values ('" + chan.id + "|" + message.ID + "');");
                                                File.Delete(Path.GetFullPath(filename));
                                             }
                                          } catch { }
                                       }
                                    }
                                 }
                              }
                           }
                        } catch { }
                     }
                  }
               } catch { }
            }
         } catch { }
      }

      public static WTelegram.Client wclient;
      public static async Task AuthAsync()
      {
         try {
            wclient = new WTelegram.Client(15154180, "01525307f48c1d6e59a7e796963b18d6");
            await wclient.ConnectAsync();
            await wclient.Login("+79996668870");
            if (wclient.User == null) {
               Console.WriteLine("Введите код из SMS: ");
               string code = Console.ReadLine();
               Console.WriteLine(await wclient.Login(code));
               Console.WriteLine(wclient.User);
            }
            new Thread(() => {
               Parse();
            }).Start();
         } catch { }
      }

      public static List<Allch> allches = new List<Allch>();
      public class Allch
      {
         public int localId { get; set; }
         public string id { get; set; }
         public string hash { get; set; }
         public string title { get; set; }
         public Allch(int localId, string id, string hash, string title)
         {
            this.localId = localId;
            this.id = id;
            this.hash = hash;
            this.title = title;
         }
      }

      public async static void Parse()
      {
         try {
            while (true) {
               Connect.LoadChannel(chanels);
               await GetChanels(null, false, true);
               if (chanels.Count > 0) {
                  await GetMessage();
                  await Task.Delay(5000);
               }
               else await Task.Delay(52000);
            }
         } catch {
            new Thread(() => {
               Parse();
            }).Start();
         }
      }
   }
}
