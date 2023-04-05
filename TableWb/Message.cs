namespace TableWb
{
   public class Message
   {
      public string article { get; set; }
      public string title { get; set; }
      public Message(string article, string title)
      {
         this.article = article;
         this.title = title;
      }
   }
}
