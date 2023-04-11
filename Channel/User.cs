namespace Chanel
{
   public class User
   {
      public string id { get; set; }
      public string message { get; set; }
      public User(string id, string message)
      {
         this.id = id;
         this.message = message;
      }
   }
}
