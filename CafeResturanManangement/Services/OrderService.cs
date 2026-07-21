using System.IO;
using System.Text.Json;
using CafeResturanManangement.Models;

namespace CafeResturanManangement.Services;

public class OrderService
{
    private readonly string _dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private readonly string _activeOrdersPath;
    private readonly string _counterPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public List<Order> ActiveOrders { get; private set; } = new();

    public OrderService()
    {
        Directory.CreateDirectory(_dataFolder);
        _activeOrdersPath = Path.Combine(_dataFolder, "active_orders.json");
        _counterPath = Path.Combine(_dataFolder, "invoice_counter.txt");
        LoadActiveOrders();
    }

    private void LoadActiveOrders()
    {
        try
        {
            if (File.Exists(_activeOrdersPath))
            {
                var json = File.ReadAllText(_activeOrdersPath);
                ActiveOrders = JsonSerializer.Deserialize<List<Order>>(json, _jsonOptions) ?? new List<Order>();
            }
        }
        catch
        {
            ActiveOrders = new List<Order>();
        }
    }

    private void SaveActiveOrders()
    {
        var json = JsonSerializer.Serialize(ActiveOrders, _jsonOptions);
        File.WriteAllText(_activeOrdersPath, json);
    }

    private int GetNextInvoiceNumber()
    {
        try
        {
            int next = 1000;
            if (File.Exists(_counterPath) && int.TryParse(File.ReadAllText(_counterPath), out var stored))
                next = stored + 1;
            File.WriteAllText(_counterPath, next.ToString());
            return next;
        }
        catch
        {
            return new Random().Next(9000, 9999); 
        }
    }

    public Order PlaceOrder(List<MenuItem> menu, List<OrderItem> cart, bool isTakeaway,
        string? customerName, string? tableNumber, string? discountCode)
    {
        if (cart.Count == 0)
            throw new InvalidOperationException("سبد سفارش خالی است.");

        bool hasDiscount = !string.IsNullOrWhiteSpace(discountCode);
        if (hasDiscount && !DiscountService.IsValid(discountCode!))
            throw new InvalidDiscountCodeException(discountCode!);

        foreach (var cartItem in cart)
        {
            var menuItem = menu.FirstOrDefault(m => m.Id == cartItem.MenuItemId)
                ?? throw new KeyNotFoundException($"آیتم منو با شناسه {cartItem.MenuItemId} یافت نشد.");

            if (menuItem.StockQuantity < cartItem.Quantity)
                throw new InsufficientStockException(menuItem.Name, menuItem.StockQuantity, cartItem.Quantity);
        }

        foreach (var cartItem in cart)
        {
            var menuItem = menu.First(m => m.Id == cartItem.MenuItemId);
            menuItem.StockQuantity -= cartItem.Quantity;
        }
        MenuService.Save(menu);

        var order = new Order
        {
            InvoiceNumber = GetNextInvoiceNumber(),
            OrderDateTime = DateTime.Now,
            Items = cart,
            Status = OrderStatus.Preparing,
            IsTakeaway = isTakeaway,
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName!.Trim(),
            TableNumber = string.IsNullOrWhiteSpace(tableNumber) ? null : tableNumber!.Trim(),
        };

        if (hasDiscount)
        {
            order.DiscountPercent = DiscountService.Redeem(discountCode!);
            order.DiscountCodeUsed = discountCode;
        }

        ActiveOrders.Add(order);
        SaveActiveOrders();
        return order;
    }

    public void MarkDelivered(int invoiceNumber)
    {
        var order = ActiveOrders.FirstOrDefault(o => o.InvoiceNumber == invoiceNumber)
            ?? throw new OrderNotFoundException(invoiceNumber);

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.Now;

        ActiveOrders.Remove(order);
        SaveActiveOrders();
        SalesLogService.Append(order);
    }
}
