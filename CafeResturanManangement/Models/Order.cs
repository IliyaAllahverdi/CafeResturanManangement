namespace CafeResturanManangement.Models;

public class Order
{
    public int InvoiceNumber { get; set; }
    public DateTime OrderDateTime { get; set; } = DateTime.Now;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; } = OrderStatus.Preparing;
    public bool IsTakeaway { get; set; }
    public string? CustomerName { get; set; }
    public string? TableNumber { get; set; }

    public string? DiscountCodeUsed { get; set; }
    public decimal DiscountPercent { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public const decimal VatRate = 0.09m;        
    public const decimal PackagingFee = 15000m;  
    
    public decimal SubTotal => Items.Sum(i => i.LineTotal);

    public decimal DiscountAmount => Math.Round(SubTotal * DiscountPercent / 100m, 0, MidpointRounding.AwayFromZero);

    public decimal TaxableAmount => SubTotal - DiscountAmount;

    public decimal VatAmount => Math.Round(TaxableAmount * VatRate, 0, MidpointRounding.AwayFromZero);

    public decimal PackagingCost => IsTakeaway ? PackagingFee : 0m;

    public decimal GrandTotal => TaxableAmount + VatAmount + PackagingCost;
}
