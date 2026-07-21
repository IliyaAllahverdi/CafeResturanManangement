namespace CafeResturanManangement.Models;


public class InsufficientStockException : Exception
{
    public InsufficientStockException(string itemName, int available, int requested)
        : base($"موجودی «{itemName}» کافی نیست. موجودی فعلی: {available} | تعداد درخواستی: {requested}")
    {
    }
}

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException(int invoiceNumber)
        : base($"سفارشی با شماره فاکتور {invoiceNumber} در صف سفارش‌ها یافت نشد.")
    {
    }
}

public class InvalidDiscountCodeException : Exception
{
    public InvalidDiscountCodeException(string code)
        : base($"کد تخفیف «{code}» نامعتبر است یا قبلاً استفاده شده است.")
    {
    }
}
