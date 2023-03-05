namespace StarTaxi.DataBase
{
   internal class Driver
   {
      public string id { get; set; }
      public string fio { get; set; }
      public string photo_face { get; set; }
      public string phone { get; set; }
      public string experiense { get; set; }
      public string auto { get; set; }
      public string plate_number { get; set; }
      public string photo_auto { get; set; }
      public string rating { get; set; }
      public string ride_count { get; set; }
      public string bank_card { get; set; }
      public string status { get; set; }
      public string category { get; set; }
      public Driver(string id, string fio, string photo_face, string phone, string experiense, string auto, string plate_number, string photo_auto, string rating, string ride_count, string bank_card, string status, string category)
      {
         this.id = id;
         this.fio = fio;
         this.photo_face = photo_face;
         this.phone = phone;
         this.experiense = experiense;
         this.auto = auto;
         this.plate_number = plate_number;
         this.photo_auto = photo_auto;
         this.rating = rating;
         this.ride_count = ride_count;
         this.bank_card = bank_card;
         this.status = status;
         this.category = category;
      }
   }
}
