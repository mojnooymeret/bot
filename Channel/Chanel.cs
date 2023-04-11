namespace Chanel
{
   public class Chanel
   {
      public long id { get; set; }
      public string topic { get; set; }
      public string title { get; set; }
      public Chanel(long id, string topic, string title)
      {
         this.id = id;
         this.topic = topic;
         this.title = title;
      }
   }
}
