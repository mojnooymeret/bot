using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Chanel
{
   public class Connect
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|chanel.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadChannel(List<Chanel> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Chanel`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Chanel(
                     Convert.ToInt64(query.GetValue(0)),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString()
                  ));
               }
            }
         } catch { }
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
                     query.GetValue(1).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadPost(List<Post> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Post`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Post(
                     query.GetValue(0).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
