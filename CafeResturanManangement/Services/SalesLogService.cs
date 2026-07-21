using System.IO;
using System.Text.Json;
using System.Windows;
using CafeResturanManangement.Models;

namespace CafeResturanManangement.Services;

public static class SalesLogService
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string LogFilePath = Path.Combine(DataFolder, "sales_log.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<Order> LoadAll()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            if (!File.Exists(LogFilePath)) return new List<Order>();
            var json = File.ReadAllText(LogFilePath);
            return JsonSerializer.Deserialize<List<Order>>(json, JsonOptions) ?? new List<Order>();
        }
        catch
        {
            return new List<Order>();
        }
    }

    public static void Append(Order order)
    {
        try
        {
            var all = LoadAll();
            all.Add(order);
            Directory.CreateDirectory(DataFolder);
            var json = JsonSerializer.Serialize(all, JsonOptions);
            File.WriteAllText(LogFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ثبت لاگ فروش: {ex.Message}",
                "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public static decimal GetTodayRevenue() =>
        LoadAll().Where(o => o.Status == OrderStatus.Delivered && o.OrderDateTime.Date == DateTime.Today)
                 .Sum(o => o.GrandTotal);

    public static decimal GetTotalRevenue() =>
        LoadAll().Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.GrandTotal);

    public static List<(string Name, int Count)> GetBestSellers(int top = 5) =>
        LoadAll()
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ItemName)
            .Select(g => (Name: g.Key, Count: g.Sum(i => i.Quantity)))
            .OrderByDescending(x => x.Count)
            .Take(top)
            .ToList();
}
