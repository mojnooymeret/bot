namespace Avangard
{
   public class SubCategory
   {
      public int id { get; set; }
      public int id_category { get; set; }
      public string name { get; set; }
      public SubCategory(int id, int id_category, string name)
      {
         this.id = id;
         this.id_category = id_category;
         this.name = name;
      }
   }
}
