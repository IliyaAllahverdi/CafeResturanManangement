namespace CafeResturanManangement.Models;

public class Food : MenuItem
{
    public int CookingTimeMinutes { get; set; }

    public override decimal GetPrice() => Math.Round(BasePrice * 1.10m, 0, MidpointRounding.AwayFromZero);

    public override string GetItemType() => "غذا";
}
