using System.Text.Json.Serialization;

namespace CafeResturanManangement.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$itemType")]
[JsonDerivedType(typeof(Food), typeDiscriminator: "food")]
[JsonDerivedType(typeof(Drink), typeDiscriminator: "drink")]
public abstract class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int StockQuantity { get; set; }
 
    public string Category { get; set; } = "عمومی";
    public abstract decimal GetPrice();
    
    public abstract string GetItemType();
}
