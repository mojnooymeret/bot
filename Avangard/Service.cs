namespace Avangard
{
   public class Service
   {
      public int id { get; set; }
      public int id_category { get; set; }
      public string name { get; set; }
      public string desc { get; set; }
      public string url { get; set; }
      public string type { get; set; }
      public string price { get; set; }
      public Service(int id, int id_category, string name, string desc, string url, string type, string price)
      {
         this.id = id;
         this.id_category = id_category;
         this.name = name;
         this.desc = desc;
         this.url = url;
         this.type = type;
         this.price = price;
      }
   }
}
