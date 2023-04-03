using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace TableWb
{
   public class Connect
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|wb.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadMessage(List<Message> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Message`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Message(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
