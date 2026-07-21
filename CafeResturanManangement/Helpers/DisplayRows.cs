using CafeResturanManangement.Models;

namespace CafeResturanManangement.Helpers;


public class MenuItemRow
{
    public MenuItem Source { get; }

    public int Id => Source.Id;
    public string Type => Source.GetItemType();

    public string Name
    {
        get => Source.Name;
        set => Source.Name = value;
    }

    public decimal BasePrice
    {
        get => Source.BasePrice;
        set => Source.BasePrice = value;
    }

    public decimal FinalPrice => Source.GetPrice();

    public int StockQuantity
    {
        get => Source.StockQuantity;
        set => Source.StockQuantity = value;
    }

    public string Category => Source.Category;

    public string Details => Source switch
    {
        Food f => $"زمان پخت: {f.CookingTimeMinutes} دقیقه",
        Drink d => $"{d.VolumeMl:0} میلی‌لیتر - {(d.IsCold ? "سرد" : "گرم")}",
        _ => ""
    };

    public MenuItemRow(MenuItem source) => Source = source;
}

public class OrderQueueRow
{
    public Order Source { get; }

    public int InvoiceNumber => Source.InvoiceNumber;
    public string Time => Source.OrderDateTime.ToString("HH:mm");
    public string CustomerOrTable => Source.CustomerName ?? Source.TableNumber ?? "-";
    public string TakeawayText => Source.IsTakeaway ? "بیرون‌بر" : "حضوری";
    public string ItemsSummary => string.Join("، ", Source.Items.Select(i => $"{i.ItemName} × {i.Quantity}"));
    public string Elapsed => $"{Math.Max(0, (int)(DateTime.Now - Source.OrderDateTime).TotalMinutes)} دقیقه پیش";
    public decimal GrandTotal => Source.GrandTotal;

    public OrderQueueRow(Order source) => Source = source;
}

public class OrderHistoryRow
{
    public Order Source { get; }

    public int InvoiceNumber => Source.InvoiceNumber;
    public string DateTimeText => Source.OrderDateTime.ToString("yyyy/MM/dd HH:mm");
    public string CustomerOrTable => Source.CustomerName ?? Source.TableNumber ?? "-";
    public int ItemCount => Source.Items.Sum(i => i.Quantity);
    public decimal GrandTotal => Source.GrandTotal;
    public string DiscountText => Source.DiscountCodeUsed is null ? "-" : $"{Source.DiscountCodeUsed} (٪{Source.DiscountPercent:0})";
    public string Status => Source.Status switch
    {
        OrderStatus.Delivered => "تحویل شده",
        OrderStatus.Preparing => "در حال آماده‌سازی",
        _ => "لغو شده"
    };

    public OrderHistoryRow(Order source) => Source = source;
}
