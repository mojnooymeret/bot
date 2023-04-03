using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace Avangard
{
   public class Connect
   {
      public static SQLiteDataReader Query(string str) // функция взаимодействия с базой, где str - sql запрос
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|avangard.db"); // путь к базе (указан корень программы, папка netcoreapp3.1 файл avangard.db)
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection); // создаем подключение
         try {
            SQLiteConnection.Open(); // открываем подключение
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader(); // выполняем запрос
            return reader; // закрываем чтение
         } catch { return null; }
      }

      public static void LoadUser(List<User> data) // получение данных из таблицы User БД
      {
         try {
            data.Clear(); // чистим входящую коллекцию (List из Program.cs)
            SQLiteDataReader query = Query("select * from `User`;"); // выполняем запрос на получение данных
            if (query != null) { // если запрос дал результат
               while (query.Read()) { // пока данные запроса читаются
                  data.Add(new User( // добавляем данные в коллекцию
                     query.GetValue(0).ToString(), // GetValue(0), где 0 - номер столбца в БД
                     query.GetValue(1).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadCategory(List<Category> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Category`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Category(
                     Convert.ToInt32(query.GetValue(0)),
                     query.GetValue(1).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadRequest(List<Request> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Request`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Request(
                     Convert.ToInt32(query.GetValue(0)),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     query.GetValue(5).ToString(),
                     query.GetValue(6).ToString(),
                     query.GetValue(7).ToString(),
                     query.GetValue(8).ToString(),
                     query.GetValue(9).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadService(List<Service> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Service`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Service(
                     Convert.ToInt32(query.GetValue(0)),
                     Convert.ToInt32(query.GetValue(1)),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     query.GetValue(5).ToString(),
                     query.GetValue(6).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadSubCategory(List<SubCategory> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `SubCategory`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new SubCategory(
                     Convert.ToInt32(query.GetValue(0)),
                     Convert.ToInt32(query.GetValue(1)),
                     query.GetValue(2).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadSubService(List<SubService> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `SubService`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new SubService(
                     Convert.ToInt32(query.GetValue(0)),
                     Convert.ToInt32(query.GetValue(1)),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     query.GetValue(5).ToString(),
                     query.GetValue(6).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
