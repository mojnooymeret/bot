namespace StarTaxi.DataBase
{
   internal class Work
   {
      public int id {get;set;}
      public string id_driver { get; set; }
      public string status { get; set; }
      public string messages { get; set; }
      public string category { get; set; }
      public Work(int id, string id_driver, string status, string messages, string category)
      {
         this.id = id;
         this.id_driver = id_driver;
         this.status = status;
         this.messages = messages;
         this.category = category;
      }
   }
}
