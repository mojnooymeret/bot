namespace StarTaxi.DataBase
{
   internal class User
   {
      public string id { get; set; }
      public string username { get; set; }
      public string phone { get; set; }
      public string telegram { get; set; }
      public User(string id, string username, string phone, string telegram)
      {
         this.id = id;
         this.username = username;
         this.phone = phone;
         this.telegram = telegram;
      }
   }
}
