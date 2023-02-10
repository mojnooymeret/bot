namespace StarTaxi.DataBase
{
   internal class Order
   {
      public int id { get; set; }
      public string id_client { get; set; }
      public string id_driver { get; set; }
      public string date { get; set; }
      public string price { get; set; }
      public double start_latitude { get; set; }
      public double start_longitude { get; set; }
      public double end_latitude { get; set; }
      public double end_logitude { get; set; }
      public string grade { get; set; }
      public string status { get; set; }
      public Order(int id, string id_client, string id_driver, string date, string price, double start_latitude, double start_longitude, double end_latitude, double end_logitude, string grade, string status)
      {
         this.id = id;
         this.id_client = id_client;
         this.id_driver = id_driver;
         this.date = date;
         this.price = price;
         this.start_latitude = start_latitude;
         this.start_longitude = start_longitude;
         this.end_latitude = end_latitude;
         this.end_logitude = end_logitude;
         this.grade = grade;
         this.status = status;
      }
   }
}
