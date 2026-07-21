using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CafeResturanManangement.Helpers;
using CafeResturanManangement.Models;
using CafeResturanManangement.Services;
using MenuItem = CafeResturanManangement.Models.MenuItem;


namespace CafeResturanManangement;

public partial class MainWindow : Window
{
    private List<MenuItem> _menu = new();
    private readonly ObservableCollection<OrderItem> _cart = new();
    private readonly OrderService _orderService = new();
    private DispatcherTimer? _timer;

    public MainWindow()
    {
        InitializeComponent();

        CartGrid.ItemsSource = _cart;

        LoadMenu();
        RefreshAvailableItemsGrid();
        RefreshOrderSummary();
        RefreshQueue();
        RefreshReports();
        SetupTimer();
    }


    private void LoadMenu()
    {
        _menu = MenuService.Load();
        RefreshMenuGrid();
    }

    private void RefreshMenuGrid()
    {
        var filter = SearchMenuBox?.Text?.Trim() ?? "";
        var rows = _menu
            .Where(m => string.IsNullOrEmpty(filter) ||
                        m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                        m.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(m => new MenuItemRow(m))
            .ToList();
        MenuDataGrid.ItemsSource = rows;
    }

    private void SearchMenuBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshMenuGrid();

    private void TypeFoodRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (FoodFieldsPanel != null) FoodFieldsPanel.Visibility = Visibility.Visible;
        if (DrinkFieldsPanel != null) DrinkFieldsPanel.Visibility = Visibility.Collapsed;
    }

    private void TypeDrinkRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (FoodFieldsPanel != null) FoodFieldsPanel.Visibility = Visibility.Collapsed;
        if (DrinkFieldsPanel != null) DrinkFieldsPanel.Visibility = Visibility.Visible;
    }

    private void AddMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = NewItemNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("نام آیتم را وارد کنید.", "خطای ورودی", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price = decimal.Parse(NewItemPriceBox.Text.Trim());
            int stock = int.Parse(NewItemStockBox.Text.Trim());
            var category = string.IsNullOrWhiteSpace(NewItemCategoryBox.Text) ? "عمومی" : NewItemCategoryBox.Text.Trim();

            if (price < 0 || stock < 0)
            {
                MessageBox.Show("قیمت و موجودی نمی‌توانند منفی باشند.", "خطای ورودی", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int newId = MenuService.GetNextId(_menu);
            MenuItem newItem;

            if (TypeFoodRadio.IsChecked == true)
            {
                int cookTime = int.Parse(string.IsNullOrWhiteSpace(CookingTimeBox.Text) ? "10" : CookingTimeBox.Text.Trim());
                newItem = new Food { Id = newId, Name = name, BasePrice = price, StockQuantity = stock, Category = category, CookingTimeMinutes = cookTime };
            }
            else
            {
                double volume = double.Parse(string.IsNullOrWhiteSpace(VolumeBox.Text) ? "250" : VolumeBox.Text.Trim());
                newItem = new Drink { Id = newId, Name = name, BasePrice = price, StockQuantity = stock, Category = category, VolumeMl = volume, IsCold = ColdCheck.IsChecked == true };
            }

            _menu.Add(newItem);
            MenuService.Save(_menu);
            RefreshMenuGrid();
            RefreshAvailableItemsGrid();

            NewItemNameBox.Clear();
            NewItemPriceBox.Clear();
            NewItemStockBox.Clear();
            NewItemCategoryBox.Clear();
            CookingTimeBox.Clear();
            VolumeBox.Clear();
            ColdCheck.IsChecked = false;

            MessageBox.Show($"«{name}» با موفقیت به منو اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (FormatException)
        {
            MessageBox.Show("قیمت، موجودی، زمان پخت یا حجم باید مقدار عددی معتبر باشند.",
                "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطای غیرمنتظره: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveMenuChangesButton_Click(object sender, RoutedEventArgs e)
    {
        
        MenuService.Save(_menu);
        RefreshMenuGrid();
        RefreshAvailableItemsGrid();
        MessageBox.Show("تغییرات منو با موفقیت ذخیره شد.", "ذخیره شد", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeleteMenuItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (MenuDataGrid.SelectedItem is not MenuItemRow row)
        {
            MessageBox.Show("ابتدا یک آیتم را از جدول انتخاب کنید.", "راهنما", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show($"آیا از حذف «{row.Name}» مطمئن هستید؟", "تایید حذف",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        _menu.RemoveAll(m => m.Id == row.Id);
        MenuService.Save(_menu);
        RefreshMenuGrid();
        RefreshAvailableItemsGrid();
    }

    private void MenuDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MenuService.Save(_menu);
            RefreshAvailableItemsGrid();
        }), DispatcherPriority.Background);
    }


    private void RefreshAvailableItemsGrid()
    {
        var filter = OrderSearchBox?.Text?.Trim() ?? "";
        var rows = _menu
            .Where(m => string.IsNullOrEmpty(filter) || m.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(m => new MenuItemRow(m))
            .ToList();
        AvailableItemsGrid.ItemsSource = rows;
    }

    private void OrderSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshAvailableItemsGrid();

    private void AddToCartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (AvailableItemsGrid.SelectedItem is not MenuItemRow row)
            {
                MessageBox.Show("یک آیتم از منو انتخاب کنید.", "راهنما", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int qty = int.Parse(QuantityBox.Text.Trim());
            if (qty <= 0)
            {
                MessageBox.Show("تعداد باید بزرگ‌تر از صفر باشد.", "خطای ورودی", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var menuItem = _menu.First(m => m.Id == row.Id);

            int alreadyInCart = _cart.Where(c => c.MenuItemId == row.Id).Sum(c => c.Quantity);
            if (menuItem.StockQuantity < alreadyInCart + qty)
                throw new InsufficientStockException(menuItem.Name, menuItem.StockQuantity, alreadyInCart + qty);

            var existing = _cart.FirstOrDefault(c => c.MenuItemId == row.Id);
            if (existing != null)
            {
                existing.Quantity += qty;
                CartGrid.Items.Refresh();
            }
            else
            {
                _cart.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    ItemName = menuItem.Name,
                    ItemType = menuItem.GetItemType(),
                    UnitPrice = menuItem.GetPrice(), 
                    Quantity = qty
                });
            }

            RefreshOrderSummary();
            QuantityBox.Text = "1";
        }
        catch (FormatException)
        {
            MessageBox.Show("تعداد باید یک عدد صحیح معتبر باشد.", "ورودی نامعتبر", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (InsufficientStockException ex)
        {
            MessageBox.Show(ex.Message, "موجودی ناکافی", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RemoveFromCartButton_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is OrderItem item)
        {
            _cart.Remove(item);
            RefreshOrderSummary();
        }
        else
        {
            MessageBox.Show("یک ردیف از سبد سفارش را انتخاب کنید.", "راهنما", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void TakeawayCheck_Changed(object sender, RoutedEventArgs e) => RefreshOrderSummary();

    private void RefreshOrderSummary()
    {
        decimal subtotal = _cart.Sum(i => i.LineTotal);
        decimal vat = Math.Round(subtotal * Order.VatRate, 0, MidpointRounding.AwayFromZero);
        decimal packaging = TakeawayCheck.IsChecked == true ? Order.PackagingFee : 0m;
        decimal total = subtotal + vat + packaging;

        SubtotalText.Text = $"جمع اقلام: {subtotal:N0} تومان";
        VatText.Text = $"مالیات ارزش افزوده (۹٪): {vat:N0} تومان";
        PackagingText.Text = $"هزینه بسته‌بندی: {packaging:N0} تومان";
        TotalPreviewText.Text = $"مبلغ قابل پرداخت (تخمینی، بدون احتساب تخفیف): {total:N0} تومان";
    }

    private void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("سبد سفارش خالی است.", "راهنما", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var cartCopy = _cart.Select(i => new OrderItem
            {
                MenuItemId = i.MenuItemId,
                ItemName = i.ItemName,
                ItemType = i.ItemType,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();

            var order = _orderService.PlaceOrder(
                _menu,
                cartCopy,
                TakeawayCheck.IsChecked == true,
                CustomerNameBox.Text,
                TableNumberBox.Text,
                DiscountCodeBox.Text.Trim());

            _cart.Clear();
            RefreshOrderSummary();
            CustomerNameBox.Clear();
            TableNumberBox.Clear();
            DiscountCodeBox.Clear();
            TakeawayCheck.IsChecked = false;

            RefreshMenuGrid();
            RefreshAvailableItemsGrid();
            RefreshQueue();

            var discountLine = order.DiscountAmount > 0 ? $"تخفیف ({order.DiscountCodeUsed}): {order.DiscountAmount:N0} تومان\n" : "";
            var packagingLine = order.PackagingCost > 0 ? $"هزینه بسته‌بندی: {order.PackagingCost:N0} تومان\n" : "";

            MessageBox.Show(
                $"فاکتور شماره {order.InvoiceNumber} با موفقیت صادر شد.\n\n" +
                $"جمع اقلام: {order.SubTotal:N0} تومان\n" +
                discountLine +
                $"مالیات ارزش افزوده: {order.VatAmount:N0} تومان\n" +
                packagingLine +
                $"مبلغ نهایی: {order.GrandTotal:N0} تومان\n\n" +
                "سفارش به صف آشپزخانه اضافه شد.",
                "فاکتور صادر شد", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (InsufficientStockException ex)
        {
            MessageBox.Show(ex.Message, "موجودی ناکافی", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidDiscountCodeException ex)
        {
            MessageBox.Show(ex.Message, "کد تخفیف نامعتبر", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (KeyNotFoundException ex)
        {
            MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "راهنما", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطای غیرمنتظره در ثبت سفارش: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshQueue()
    {
        var rows = _orderService.ActiveOrders
            .OrderBy(o => o.OrderDateTime)
            .Select(o => new OrderQueueRow(o))
            .ToList();
        QueueGrid.ItemsSource = rows;
    }

    private void QueueGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QueueGrid.SelectedItem is OrderQueueRow row)
            DeliverInvoiceBox.Text = row.InvoiceNumber.ToString();
    }

    private void MarkDeliveredButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int invoiceNumber = int.Parse(DeliverInvoiceBox.Text.Trim());
            _orderService.MarkDelivered(invoiceNumber);
            DeliverInvoiceBox.Clear();
            RefreshQueue();
            RefreshReports();
            MessageBox.Show($"فاکتور شماره {invoiceNumber} با موفقیت تحویل داده شد.", "موفق",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (FormatException)
        {
            MessageBox.Show("شماره فاکتور باید یک عدد صحیح معتبر باشد.", "ورودی نامعتبر",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (OrderNotFoundException ex)
        {
            MessageBox.Show(ex.Message, "سفارش یافت نشد", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshQueueButton_Click(object sender, RoutedEventArgs e) => RefreshQueue();


    private void RefreshReports()
    {
        TodayRevenueText.Text = $"درآمد امروز: {SalesLogService.GetTodayRevenue():N0} تومان";
        TotalRevenueText.Text = $"درآمد کل: {SalesLogService.GetTotalRevenue():N0} تومان";

        var history = SalesLogService.LoadAll()
            .OrderByDescending(o => o.OrderDateTime)
            .Select(o => new OrderHistoryRow(o))
            .ToList();
        HistoryGrid.ItemsSource = history;
        DeliveredCountText.Text = $"تعداد فاکتورهای تحویل‌شده: {history.Count(h => h.Status == "تحویل شده")}";

        var bestSellers = SalesLogService.GetBestSellers()
            .Select(b => new { b.Name, b.Count })
            .ToList();
        BestSellersGrid.ItemsSource = bestSellers;

        DiscountGrid.ItemsSource = DiscountService.Load();
    }

    private void RefreshReportsButton_Click(object sender, RoutedEventArgs e) => RefreshReports();

    private void AddDiscountButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var code = NewCodeBox.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("کد تخفیف را وارد کنید.", "خطای ورودی", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal percent = decimal.Parse(NewPercentBox.Text.Trim());
            if (percent <= 0 || percent > 100)
            {
                MessageBox.Show("درصد تخفیف باید بین ۱ تا ۱۰۰ باشد.", "خطای ورودی", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var codes = DiscountService.Load();
            if (codes.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("این کد تخفیف قبلاً ثبت شده است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            codes.Add(new DiscountCode { Code = code, PercentOff = percent });
            DiscountService.Save(codes);

            NewCodeBox.Clear();
            NewPercentBox.Clear();
            RefreshReports();
        }
        catch (FormatException)
        {
            MessageBox.Show("درصد تخفیف باید یک مقدار عددی معتبر باشد.", "ورودی نامعتبر",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetupTimer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("yyyy/MM/dd   HH:mm:ss");
            RefreshQueue();
        };
        _timer.Start();
        ClockText.Text = DateTime.Now.ToString("yyyy/MM/dd   HH:mm:ss");
    }
}
