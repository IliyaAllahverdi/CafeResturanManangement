using System.IO;
using System.Text.Json;
using System.Windows;
using CafeResturanManangement.Models;

namespace CafeResturanManangement.Services;

public static class MenuService
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string MenuFilePath = Path.Combine(DataFolder, "menu.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<MenuItem> Load()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);

            if (!File.Exists(MenuFilePath))
            {
                var seed = CreateSeedData();
                Save(seed);
                return seed;
            }

            var json = File.ReadAllText(MenuFilePath);
            var items = JsonSerializer.Deserialize<List<MenuItem>>(json, JsonOptions);
            return items ?? new List<MenuItem>();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در خواندن فایل منو: {ex.Message}\nمنو خالی بارگذاری شد.",
                "خطا در بارگذاری داده", MessageBoxButton.OK, MessageBoxImage.Warning);
            return new List<MenuItem>();
        }
    }

    public static void Save(List<MenuItem> items)
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            var json = JsonSerializer.Serialize(items, JsonOptions);
            File.WriteAllText(MenuFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره فایل منو: {ex.Message}",
                "خطا در ذخیره‌سازی", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static int GetNextId(List<MenuItem> items) => items.Count == 0 ? 1 : items.Max(i => i.Id) + 1;

    private static List<MenuItem> CreateSeedData()
    {
        return new List<MenuItem>
        {
            new Food  { Id = 1, Name = "چلوکباب کوبیده",  BasePrice = 320000, StockQuantity = 20, CookingTimeMinutes = 25, Category = "غذای اصلی" },
            new Food  { Id = 2, Name = "پیتزا مخصوص",     BasePrice = 280000, StockQuantity = 15, CookingTimeMinutes = 20, Category = "فست فود" },
            new Food  { Id = 3, Name = "قورمه سبزی",      BasePrice = 260000, StockQuantity = 12, CookingTimeMinutes = 30, Category = "غذای اصلی" },
            new Drink { Id = 4, Name = "دوغ",             BasePrice = 25000,  StockQuantity = 40, VolumeMl = 300, IsCold = true,  Category = "نوشیدنی سنتی" },
            new Drink { Id = 5, Name = "چای دبش",         BasePrice = 20000,  StockQuantity = 50, VolumeMl = 200, IsCold = false, Category = "نوشیدنی گرم" },
            new Drink { Id = 6, Name = "نوشابه قوطی",     BasePrice = 30000,  StockQuantity = 60, VolumeMl = 330, IsCold = true,  Category = "نوشیدنی سرد" },
        };
    }
}
