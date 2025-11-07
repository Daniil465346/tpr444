namespace InvestmentApi.Models
{
    public class Security
    {
        public int Id { get; set; }
        public string Ticker { get; set; } 
        public string Name { get; set; }   
        public decimal CurrentPrice { get; set; } 
    }
}