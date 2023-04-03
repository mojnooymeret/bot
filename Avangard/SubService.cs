namespace Avangard
{
   public class SubService
   {
      public int id { get; set; }
      public int id_subcategory { get; set; }
      public string name { get; set; }
      public string desc { get; set; }
      public string url { get; set; }
      public string type { get; set; }
      public string price { get; set; }
      public SubService(int id, int id_subcategory, string name, string desc, string url, string type, string price)
      {
         this.id = id;
         this.id_subcategory = id_subcategory;
         this.name = name;
         this.desc = desc;
         this.url = url;
         this.type = type;
         this.price = price;
      }
   }
}
