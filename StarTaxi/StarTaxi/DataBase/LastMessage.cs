namespace StarTaxi.DataBase
{
   internal class LastMessage
   {
      public string id { get; set; }
      public string bot_message { get; set; }
      public LastMessage(string id, string bot_message)
      {
         this.id = id;
         this.bot_message = bot_message;
      }
   }
}
