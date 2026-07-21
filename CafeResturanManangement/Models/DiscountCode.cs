namespace CafeResturanManangement.Models;

public class DiscountCode
{
   
        public string Code { get; set; } = string.Empty;
        public decimal PercentOff { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
  
}