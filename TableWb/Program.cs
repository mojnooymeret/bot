using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HtmlAgilityPack;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TableWb
{
   internal class Program
   {
      private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
      private const string SpreadsheetId = "1z-7GcT4XrQrnoZa7rGGmQ-DOt5xDHbGwHnMyeDPlqmU";
      private const string GoogleCredentialsFileName = "google-credentials.json";
      private static string pathBrowser = string.Empty;
      static string[] tables = { "Leftovers!A:Z", "Finder!A:Z", "Category!A:Z" };

      static async Task Main(string[] args)
      {
         var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
         pathBrowser = Path.GetDirectoryName(location);
         var serviceValues = GetSheetsService().Spreadsheets.Values;
         //await MoveAsync(serviceValues);
         //await ReadAsync(serviceValues, "Leftovers!A:B");
         await ReadAsync(serviceValues, "Finder!A:B");
         //await MoveAsync(serviceValues);
         //await ReadAsync(serviceValues, "Category!A:A");

         Console.ReadKey();
      }

      static string[] ua = {"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_1) AppleWebKit/602.2.14 (KHTML, like Gecko) Version/10.0.1 Safari/602.2.14",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.98 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.98 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0" };

      static bool move = false;
      private static async void Parse()
      {
         try {
            Collect();
            for (int j = 0; j < items.Count; j++) {
               if (DateTime.Now.Hour == 0 && move == false) {
                  move = true;
                  var service = GetSheetsService().Spreadsheets.Values;
                  await MoveAsync(service);
                  return;
               }
               else if (DateTime.Now.Hour != 0)
                  move = false;
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions());
               try {
                  driver.Manage().Window.Size = new Size(1440, 900);
                  driver.Url = "https://www.wildberries.ru/catalog/" + items[j].article + "/detail.aspx?targetUrl=SP";
                  await Task.Delay(12000);
                  var sizes = driver.FindElements(By.ClassName("sizes-list"));
                  if (sizes.Count > 0) {
                     string response = string.Empty;
                     var jSize = driver.FindElements(By.ClassName("j-size"));
                     var sizesName = driver.FindElements(By.ClassName("sizes-list__size-ru"));
                     if (jSize.Count > 0) {
                        for (int i = 0; i < jSize.Count; i++) {
                           try {
                              sizesName = driver.FindElements(By.ClassName("sizes-list__size-ru"));
                              response += sizesName[i].Text + " (";
                              jSize = driver.FindElements(By.ClassName("j-size"));
                              string enabledSize = jSize[i].GetAttribute("class");
                              if (enabledSize.Contains("disabled")) {
                                 response += "0 шт.)\n";
                                 await Task.Delay(500);
                                 continue;
                              }
                              jSize[i].Click();
                              var addCartSize = driver.FindElements(By.ClassName("btn-main"));
                              for (int k = 0; k < addCartSize.Count; k++) {
                                 try {
                                    addCartSize[k].Click();
                                    break;
                                 } catch { }
                              }
                              await Task.Delay(2500);
                              driver.FindElement(By.CssSelector("#basketContent > div.navbar-pc__item.j-item-basket > a")).Click();
                              int countSize = 1;
                              while (true) {
                                 try {
                                    await Task.Delay(50);
                                    var plus = driver.FindElement(By.CssSelector("#basketForm > div.basket-form__content.j-basket-form__content > div:nth-child(5) > div.basket-section__basket-list.basket-list > div > div > div > div > div > div > div.list-item__count.count > div > div > button.count__plus.plus"));
                                    string enable = plus.GetAttribute("class");
                                    if (enable.Contains("disable") || countSize == 255)
                                       break;
                                    plus.Click();
                                    countSize++;
                                 } catch { }
                              }
                              if (countSize == 255)
                                 response += countSize + "+ шт.)\n";
                              else
                                 response += countSize + " шт.)\n";
                              driver.Manage().Window.Size = new Size(640, 900);
                              await Task.Delay(100);
                              driver.FindElement(By.CssSelector("#basketForm > div.basket-form__content.j-basket-form__content > div:nth-child(5) > div.basket-section__basket-list.basket-list > div > div > div > div > div > div > div.list-item__good > div > div.list-item__price > div.list-item__btn.btn > button.btn__del.j-basket-item-del")).Click();
                              driver.Manage().Window.Size = new Size(1440, 900);
                              await Task.Delay(5000);
                              driver.Url = "https://www.wildberries.ru/catalog/" + items[j].article + "/detail.aspx?targetUrl=SP";
                              await Task.Delay(3000);
                           } catch { }
                        }
                        var service = GetSheetsService().Spreadsheets.Values;
                        await WriteAsync(service, items[j].range, new List<IList<object>> { new List<object> { response.Trim('\n') } });
                     }
                  }
                  else {
                     var addCart = driver.FindElements(By.ClassName("btn-main"));
                     for (int i = 0; i < addCart.Count; i++) {
                        try {
                           addCart[i].Click();
                           break;
                        } catch { }
                     }
                     await Task.Delay(500);
                     driver.FindElement(By.CssSelector("#basketContent > div.navbar-pc__item.j-item-basket > a")).Click();
                     int count = 1;
                     while (true) {
                        try {
                           await Task.Delay(100);
                           var plus = driver.FindElement(By.CssSelector("#basketForm > div.basket-form__content.j-basket-form__content > div:nth-child(5) > div.basket-section__basket-list.basket-list > div > div > div > div > div > div > div.list-item__count.count > div > div > button.count__plus.plus"));
                           string enable = plus.GetAttribute("class");
                           if (enable.Contains("disable") || count == 255) {
                              var service = GetSheetsService().Spreadsheets.Values;
                              if (count == 255)
                                 await WriteAsync(service, items[j].range, new List<IList<object>> { new List<object> { count + "+ шт." } });
                              else
                                 await WriteAsync(service, items[j].range, new List<IList<object>> { new List<object> { count + " шт." } });
                              break;
                           }
                           plus.Click();
                           count++;
                        } catch { }
                     }
                  }
                  driver.Quit();
               } catch {
                  driver.Quit();
                  if (DateTime.Now.Hour == 0 && move == false) {
                     move = true;
                     var service = GetSheetsService().Spreadsheets.Values;
                     await MoveAsync(service);
                     return;
                  }
                  else if (DateTime.Now.Hour != 0)
                     move = false;
               }
            }
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            await ReadAsync(serviceValues, "Leftovers!A:Z");
            return;
         } catch {
            if (DateTime.Now.Hour == 0 && move == false) {
               move = true;
               var serviceValues = GetSheetsService().Spreadsheets.Values;
               await MoveAsync(serviceValues);
               return;
            }
            else if (DateTime.Now.Hour != 0) {
               move = false;
               var serviceValues = GetSheetsService().Spreadsheets.Values;
               await ReadAsync(serviceValues, "Leftovers!A:Z");
               return;
            }
         }
      }

      public static void Collect()
      {
         try {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            try {
               foreach (Process proc in Process.GetProcessesByName("msedge")) {
                  proc.Kill();
               }
            } catch { }
            try {
               foreach (Process proc in Process.GetProcessesByName("msedgewebview2")) {
                  proc.Kill();
               }
            } catch { }
         } catch { }
      }

      public static async void FindCard()
      {
         try {
            Collect();
            var valuesResource = GetSheetsService().Spreadsheets.Values;
            for (int k = 0; k < finds.Count; k++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions());
               try {
                  int page = 1;
                  while (true) {
                     try {
                        if (page > 15) {
                           await WriteAsync(valuesResource, finds[k].range, new List<IList<object>> { new List<object> { "1500+" } });
                           break;
                        }
                        driver.Url = "https://search.wb.ru/exactmatch/ru/male/v4/search?curr=rub&dest=-1257786&query=" + finds[k].request + "&page=" + page + "&regions=80,64,38,4,115,83,33,68,70,69,30,86,75,40,1,66,48,110,22,31,71,114,111&resultset=catalog&spp=0";
                        await Task.Delay(5000);
                        var source = driver.PageSource;
                        HtmlDocument document = new HtmlDocument();
                        document.LoadHtml(source);
                        string html = document.DocumentNode.InnerText;
                        Root root = JsonConvert.DeserializeObject<Root>(html);
                        var asd = root.data.products.Find(x => x.id.ToString() == finds[k].article);
                        if (asd != null) {
                           int position = 0;
                           for (int i = 0; i < root.data.products.Count; i++) {
                              position++;
                              if (root.data.products[i].id.ToString() == finds[k].article)
                                 break;
                           }
                           position = position + (page - 1) * 100;
                           await WriteAsync(valuesResource, finds[k].range, new List<IList<object>> { new List<object> { position.ToString() } });
                           break;
                        }
                     } catch { }
                     page++;
                  }
                  driver.Quit();
                  #region old_SeleniumClicks
                  //driver.Manage().Window.Size = new Size(1440, 900);
                  //driver.Url = "https://www.wildberries.ru/catalog/0/search.aspx?search=" + finds[k].request;
                  //await Task.Delay(12000);
                  //int position = 0;
                  //bool find = false;
                  //while (true) {
                  //   IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                  //   int last = 0, next = 710;
                  //   for (int i = 0; i < 15; i++) {
                  //      js.ExecuteScript("window.scrollTo(" + last + ", " + next + ");");
                  //      last += 710;
                  //      next += 710;
                  //      await Task.Delay(100);
                  //   }
                  //   var items = driver.FindElements(By.ClassName("j-card-item"));
                  //   for (int i = 0; i < items.Count; i++) {
                  //      try {
                  //         string getArticle = items[i].GetAttribute("data-nm-id");
                  //         if (getArticle == finds[k].article) {
                  //            find = true;
                  //            break;
                  //         }
                  //         position++;
                  //      } catch { }
                  //   }
                  //   if (find == true) {
                  //      driver.Quit();
                  //      break;
                  //   }
                  //   else if (position >= 2000) {
                  //      break;
                  //   }
                  //   else
                  //      driver.FindElement(By.ClassName("j-next-page")).Click();
                  //}
                  //await WriteAsync(valuesResource, finds[k].range, new List<IList<object>> { new List<object> { position } });
                  #endregion
               } catch { driver.Quit(); }
            }
            await ReadAsync(valuesResource, "Category!A:A");
         } catch {
            var valuesResource = GetSheetsService().Spreadsheets.Values;
            await ReadAsync(valuesResource, "Category!A:A");
         }
      }

      public static async void FindCategory()
      {
         try {
            var valuesResource = GetSheetsService().Spreadsheets.Values;
            Collect();
            for (int k = 0; k < categories.Count; k++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions());
               try {
                  int page = 1;
                  while (true) {
                     try {
                        if (page >= 15) {
                           await WriteAsync(valuesResource, categories[k].range, new List<IList<object>> { new List<object> { "1500+" } });
                           break;
                        }
                        driver.Url = categories[k].source.Replace("&page=1", "&page=" + page);
                        await Task.Delay(5100);
                        var source = driver.PageSource;
                        HtmlDocument document = new HtmlDocument();
                        document.LoadHtml(source);
                        string html = document.DocumentNode.InnerText;
                        Root root = JsonConvert.DeserializeObject<Root>(html);
                        var asd = root.data.products.Find(x => x.id.ToString() == categories[k].article);
                        if (asd != null) {
                           int position = 0;
                           for (int i = 0; i < root.data.products.Count; i++) {
                              position++;
                              if (root.data.products[i].id.ToString() == categories[k].article)
                                 break;
                           }
                           position = position + (page - 1) * 100;
                           await WriteAsync(valuesResource, categories[k].range, new List<IList<object>> { new List<object> { position } });
                           break;
                        }
                     } catch { }
                     page++;
                  }
                  driver.Quit();
                  #region old_SeleniumClicks
                  //driver.Manage().Window.Size = new Size(1440, 900);
                  //driver.Url = categories[k].source;
                  //await Task.Delay(12000);
                  //int position = 0;
                  //bool find = false;
                  //while (true) {
                  //   IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                  //   int last = 0, next = 710;
                  //   for (int i = 0; i < 15; i++) {
                  //      js.ExecuteScript("window.scrollTo(" + last + ", " + next + ");");
                  //      last += 710;
                  //      next += 710;
                  //      await Task.Delay(100);
                  //   }
                  //   var items = driver.FindElements(By.ClassName("j-card-item"));
                  //   for (int i = 0; i < items.Count; i++) {
                  //      try {
                  //         string getArticle = items[i].GetAttribute("data-nm-id");
                  //         if (getArticle == categories[k].article) {
                  //            find = true;
                  //            break;
                  //         }
                  //         position++;
                  //      } catch { }
                  //   }
                  //   if (find == true) {
                  //      driver.Quit();
                  //      break;
                  //   }
                  //   else if (position >= 2000) {
                  //      break;
                  //   }
                  //   else {
                  //      var btn = driver.FindElement(By.ClassName("j-next-page"));
                  //      var action = new Actions(driver);
                  //      action.MoveToElement(btn);
                  //      action.Perform();
                  //      try {
                  //         driver.FindElement(By.ClassName("j-next-page")).Click();
                  //      } catch { driver.FindElement(By.ClassName("j-next-page")).Click(); }
                  //   }
                  //}
                  //var service = GetSheetsService().Spreadsheets.Values;
                  //await WriteAsync(service, categories[k].range, new List<IList<object>> { new List<object> { position } });
                  #endregion
               } catch { }
            }
            await ReadAsync(valuesResource, "Leftovers!A:B");
         } catch {
            var valuesResource = GetSheetsService().Spreadsheets.Values;
            await ReadAsync(valuesResource, "Leftovers!A:B");
         }
      }

      public static EdgeOptions GetOptions()
      {
         try {
            Random rnd = new Random();
            EdgeOptions options = new EdgeOptions();
            options.AddArgument("--user-agent=" + ua[rnd.Next(0, ua.Length)]);
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArguments(new List<string>() { "--headless", "--no-sandbox", "--disable-dev-shm-usage" });
            return options;
         } catch { return null; }
      }


      public static SheetsService GetSheetsService()
      {
         try {
            using (var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read)) {
               var serviceInitializer = new BaseClientService.Initializer {
                  HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
               };
               return new SheetsService(serviceInitializer);
            }
         } catch { return null; }
      }

      static List<Item> items = new List<Item>();
      static List<Find> finds = new List<Find>();
      static List<Category> categories = new List<Category>();
      public static async Task ReadAsync(SpreadsheetsResource.ValuesResource valuesResource, string table)
      {
         try {
            var response = await valuesResource.Get(SpreadsheetId, table).ExecuteAsync();
            var values = response.Values;
            if (values == null || !values.Any()) {
               Console.WriteLine("No data found.");
               return;
            }

            if (table.Split('!')[0] == "Leftovers") {
               items.Clear();
               for (int i = 1; i < values.Count; i++) {
                  var res = string.Join("|", values[i].Select(r => r.ToString()));
                  items.Add(new Item(res.Split('|')[0], "B" + Convert.ToInt32(i + 1)));
               }
               Parse();
            }
            else if (table.Split('!')[0] == "Finder") {
               finds.Clear();
               for (int i = 1; i < values.Count; i++) {
                  var res = string.Join("|", values[i].Select(r => r.ToString()));
                  finds.Add(new Find(res.Split('|')[0], res.Split('|')[1], "Finder!C" + Convert.ToInt32(i + 1)));
               }
               FindCard();
            }
            else if (table.Split('!')[0] == "Category") {
               categories.Clear();
               string article = string.Empty;
               for (int i = 0; i < values.Count; i++) {
                  var res = string.Join("|", values[i].Select(r => r.ToString()));
                  if (res.Contains("Артикул:"))
                     article = res.Split(' ')[1].Replace(" ", string.Empty);
                  else
                     categories.Add(new Category(article, res, "Category!B" + Convert.ToInt32(i + 1)));
               }
               FindCategory();
            }
         } catch { }
      }

      private static async Task WriteAsync(SpreadsheetsResource.ValuesResource valuesResource, string writeRange, List<IList<object>> item)
      {
         try {
            var valueRange = new ValueRange { Values = item };
            var update = valuesResource.Update(valueRange, SpreadsheetId, writeRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();
         } catch { }
      }

      private static async Task MoveAsync(SpreadsheetsResource.ValuesResource valuesResource)
      {
         try {
            for (int i = 0; i < tables.Length; i++) {
               try {
                  var response = await valuesResource.Get(SpreadsheetId, tables[i]).ExecuteAsync();
                  var values = response.Values;
                  if (values == null || !values.Any())
                     continue;
                  if (tables[i].Contains("Leftovers") || tables[i].Contains("Category"))
                     MoveRange(valuesResource, values, 1, tables[i].Split('!')[0]);
                  else if (tables[i].Contains("Finder"))
                     MoveRange(valuesResource, values, 2, tables[i].Split('!')[0]);
               } catch { }
            }
            await ReadAsync(valuesResource, "Finder!A:B");
         } catch { }
      }

      private static async void MoveRange(SpreadsheetsResource.ValuesResource valuesResource, IList<IList<object>> values, int start, string table)
      {
         try {
            for (int i = 0; i < values.Count; i++) {
               List<object> obj = new List<object>();
               if (i == 0) obj.Add(DateTime.Now.Date.ToString().Split(' ')[0]);
               for (int j = start; j < values[i].Count; j++) {
                  if (i == 0) {
                     obj.Add(values[i][j]);
                  }
                  else if (j == 1 && table == "Leftovers" || j == 1 && table == "Category" || j == 2 && table == "Finder") {
                     obj.Add("");
                     obj.Add(values[i][j]);
                  }
                  else
                     obj.Add(values[i][j]);
               }
               if (table == "Leftovers" || table == "Category")
                  await WriteAsync(valuesResource, table + "!B" + Convert.ToInt32(i + 1) + ":Z" + Convert.ToInt32(i + 1), new List<IList<object>> { obj });
               else if (table == "Finder")
                  await WriteAsync(valuesResource, table + "!C" + Convert.ToInt32(i + 1) + ":Z" + Convert.ToInt32(i + 1), new List<IList<object>> { obj });
            }
         } catch { }
      }

      class Item
      {
         public string article { get; set; }
         public string range { get; set; }
         public Item(string article, string range)
         {
            this.article = article;
            this.range = range;
         }
      }

      class Find
      {
         public string article { get; set; }
         public string request { get; set; }
         public string range { get; set; }
         public Find(string article, string request, string range)
         {
            this.article = article;
            this.request = request;
            this.range = range;
         }
      }

      class Category
      {
         public string article { get; set; }
         public string source { get; set; }
         public string range { get; set; }
         public Category(string article, string source, string range)
         {
            this.article = article;
            this.source = source;
            this.range = range;
         }
      }

      public class Data
      {
         public List<Product> products { get; set; }
      }

      public class Product
      {
         public int id { get; set; }
      }

      public class Root
      {
         public Data data { get; set; }
      }
   }
}
