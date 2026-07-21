namespace CafeResturanManangement.Models;


public class Drink : MenuItem
{
    public double VolumeMl { get; set; }

    public bool IsCold { get; set; }

    public override decimal GetPrice() => BasePrice;

    public override string GetItemType() => "نوشیدنی";
}
