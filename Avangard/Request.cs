namespace Avangard
{
   public class Request
   {
      public int id { get; set; }
      public string id_user { get; set; }
      public string id_service { get; set; }
      public string id_subservice { get; set; }
      public string fio { get; set; }
      public string phone { get; set; }
      public string address { get; set; }
      public string comment { get; set; }
      public string date { get; set; }
      public string square { get; set; }
      public Request(int id, string id_user, string id_service, string id_subservice, string fio, string phone, string address, string comment, string date, string square)
      {
         this.id = id;
         this.id_user = id_user;
         this.id_service = id_service;
         this.id_subservice = id_subservice;
         this.fio = fio;
         this.phone = phone;
         this.address = address;
         this.comment = comment;
         this.date = date;
         this.square = square;
      }
   }
}
