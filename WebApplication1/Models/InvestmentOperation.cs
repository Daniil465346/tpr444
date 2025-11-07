namespace InvestmentApi.Models
{
    public class InvestmentOperation
    {
        public int Id { get; set; }
        public int SecurityId { get; set; } 
        public int Quantity { get; set; }   
        public decimal PurchasePricePerShare { get; set; } 
        public decimal Commission { get; set; } 
        public decimal? TargetBuyPrice { get; set; } 

        
        public decimal TotalCost => (Quantity * PurchasePricePerShare) + Commission;
    }
}