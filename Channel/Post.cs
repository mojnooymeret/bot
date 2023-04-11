using System;

namespace Chanel
{
   public class Post
   {
      public string id { get; set; }
      public string reply { get; set; }
      public Post(string id, string reply)
      {
         this.id = id;
         this.reply = reply;
      }

      internal object Last()
      {
         throw new NotImplementedException();
      }
   }
}
