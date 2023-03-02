using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace StarTaxi.DataBase
{
   internal class Connect
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|taxistar.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadUser(List<User> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `User`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new User(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadLastMessage(List<LastMessage> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `LastMessage`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new LastMessage(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadDriver(List<Driver> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Driver`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Driver(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     query.GetValue(5).ToString(),
                     query.GetValue(6).ToString(),
                     query.GetValue(7).ToString(),
                     query.GetValue(8).ToString(),
                     query.GetValue(9).ToString(),
                     query.GetValue(10).ToString(),
                     query.GetValue(11).ToString(),
                     query.GetValue(12).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadAnswer(List<Answer> data)
      {
         data.Clear();
         SQLiteDataReader query = Query("select * from `Answer`;");
         if (query != null) {
            while (query.Read()) {
               data.Add(new Answer(
                  query.GetValue(0).ToString(),
                  Convert.ToInt32(query.GetValue(1)),
                  query.GetValue(2).ToString(),
                  query.GetValue(3).ToString()
               ));
            }
         }
      }

      public static void LoadWork(List<Work> data)
      {
         data.Clear();
         SQLiteDataReader query = Query("select * from `Work`;");
         if (query != null) {
            while (query.Read()) {
               data.Add(new Work(
                  Convert.ToInt32(query.GetValue(0)),
                  query.GetValue(1).ToString(),
                  query.GetValue(2).ToString(),
                  query.GetValue(3).ToString(),
                  query.GetValue(4).ToString()
               ));
            }
         }
      }

      public static void LoadOrder(List<Order> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Order`");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Order(
                     Convert.ToInt32(query.GetValue(0)),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     Convert.ToDouble(query.GetValue(5)),
                     Convert.ToDouble(query.GetValue(6)),
                     Convert.ToDouble(query.GetValue(7)),
                     Convert.ToDouble(query.GetValue(8)),
                     query.GetValue(9).ToString(),
                     query.GetValue(10).ToString(),
                     query.GetValue(11).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
