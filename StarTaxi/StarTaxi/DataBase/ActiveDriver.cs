namespace StarTaxi.DataBase
{
#pragma warning disable IDE1006
   internal class ActiveDriver
   {
      public string id { get; set; }
      public string query { get; set; }
      public string status { get; set; }
      public string messages { get; set; }
      public ActiveDriver(string id, string query, string status, string messages)
      {
         this.id = id;
         this.query = query;
         this.status = status;
         this.messages = messages;
      }
   }
#pragma warning restore IDE1006
}
