namespace StarTaxi.DataBase
{
   internal class Answer
   {
      public string id { get; set; }
      public int id_message { get; set; }
      public string text { get; set; }
      public string status { get; set; }
      public Answer(string id, int id_message, string text, string status)
      {
         this.id = id;
         this.id_message = id_message;
         this.text = text;
         this.status = status;
      }
   }
}
