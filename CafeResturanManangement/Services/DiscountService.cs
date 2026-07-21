using System.IO;
using System.Text.Json;
using CafeResturanManangement.Models;

namespace CafeResturanManangement.Services;

public static class DiscountService
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string FilePath = Path.Combine(DataFolder, "discounts.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<DiscountCode> Load()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            if (!File.Exists(FilePath))
            {
                var seed = new List<DiscountCode>
                {
                    new() { Code = "WELCOME10", PercentOff = 10 },
                    new() { Code = "VIP20",     PercentOff = 20 },
                };
                Save(seed);
                return seed;
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<DiscountCode>>(json, JsonOptions) ?? new List<DiscountCode>();
        }
        catch
        {
            return new List<DiscountCode>();
        }
    }

    public static void Save(List<DiscountCode> codes)
    {
        Directory.CreateDirectory(DataFolder);
        var json = JsonSerializer.Serialize(codes, JsonOptions);
        File.WriteAllText(FilePath, json);
    }

    public static bool IsValid(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var codes = Load();
        return codes.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase) && !c.IsUsed);
    }

    public static decimal Redeem(string code)
    {
        var codes = Load();
        var found = codes.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (found is null || found.IsUsed)
            throw new InvalidDiscountCodeException(code);

        found.IsUsed = true;
        found.UsedAt = DateTime.Now;
        Save(codes);
        return found.PercentOff;
    }
}
