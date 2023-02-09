using DocumentFormat.OpenXml.Packaging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.InputFiles;

namespace Brak
{
   internal class Program
   {
      private static string token { get; set; } = "6123367281:AAFrPTXtsRggDpUdP4j-7rzR40YL0FfUGEU";
      private static TelegramBotClient client;
      static void Main(string[] args)
      {
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnUpdate += UpdateData;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }
      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            if (message.Text == "/change") {
               SearchAndReplace("Батуев");
               if (File.Exists(Path.GetFullPath("brak.docx"))) {
                  using (var stream = File.OpenRead(Path.GetFullPath("brak.docx"))) {
                     InputOnlineFile pdf = new InputOnlineFile(stream);
                     pdf.FileName = Path.GetFullPath("brak.docx").Split('\\').Last();
                     var send = await client.SendDocumentAsync(message.Chat.Id, pdf);
                  }
               }
            }
         } catch { }
      }
      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message;
            var data = ev.CallbackQuery.Data;
         } catch { }
      }

      private static async void UpdateData(object sender, UpdateEventArgs e)
      {
         try {
            var update = e.Update;
         } catch { }
      }

      public static void SearchAndReplace(string text)
      {
         using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(Path.GetFullPath("brak.docx"), true)) {
            string docText = null;
            using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream())) {
               docText = sr.ReadToEnd();
            }

            Regex regexText = new Regex("surnameman");
            docText = regexText.Replace(docText, text);

            using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create))) {
               sw.Write(docText);
            }
         }
      }
   }
}
