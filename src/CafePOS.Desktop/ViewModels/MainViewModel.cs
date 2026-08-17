using System.Collections.ObjectModel;
using System.Globalization;
using CafePOS.Application.Interfaces;
using CafePOS.Application.Models;
using CafePOS.Application.Services;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;
using CafePOS.Infrastructure.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CafePOS.Desktop.ViewModels;

public partial class MainViewModel(
    ICatalogRepository catalog,
    IOrderRepository orders,
    OrderService orderService,
    CheckoutService checkout,
    ManualTransactionService manualTransactions,
    CashReportService cashReportService,
    BusinessMetricsService metricsService,
    AdminCatalogService adminCatalog,
    InventoryService inventory,
    IBackupService backups,
    ICurrentUserContext currentUser,
    IUpdateService updates,
    IUpdateInstaller updateInstaller,
    ISettingsService settings) : ObservableObject
{
    private readonly List<CatalogItem> allCatalogItems = [];
    private AdminCatalogData adminCatalogData = new([], []);

    public ObservableCollection<CatalogItem> Products { get; } = [];
    public ObservableCollection<CartLine> Cart { get; } = [];
    public ObservableCollection<DeliveryOptionItem> DeliveryOptions { get; } = [];
    public ObservableCollection<Order> History { get; } = [];
    public ObservableCollection<OrderItem> SelectedOrderItems { get; } = [];
    public ObservableCollection<Payment> PaymentBreakdown { get; } = [];
    public ObservableCollection<Payment> PaymentAudit { get; } = [];
    public ObservableCollection<ManualTransaction> ManualTransactions { get; } = [];
    public ObservableCollection<AdminProductItem> AdminProducts { get; } = [];
    public ObservableCollection<AdminDeliveryOptionItem> AdminDeliveryOptions { get; } = [];
    public ObservableCollection<ProductStockItem> InventoryProducts { get; } = [];
    public ObservableCollection<InventoryMovementItem> InventoryMovements { get; } = [];

    public IReadOnlyList<NamedOption<ProductCategory>> ProductCategories { get; } =
        Enum.GetValues<ProductCategory>().Select(x => new NamedOption<ProductCategory>(x, CategoryLabel(x))).ToList();
    public IReadOnlyList<NamedOption<DeliveryType>> DeliveryTypes { get; } =
        Enum.GetValues<DeliveryType>().Select(x => new NamedOption<DeliveryType>(x, DeliveryLabel(x))).ToList();
    public IReadOnlyList<NamedOption<PaymentMethod>> PaymentMethods { get; } =
        Enum.GetValues<PaymentMethod>().Select(x => new NamedOption<PaymentMethod>(x, PaymentLabel(x))).ToList();
    public IReadOnlyList<NamedOption<InventoryMovementType>> AdjustmentTypes { get; } =
    [
        new(InventoryMovementType.Incoming, "Entrada / proveedor"),
        new(InventoryMovementType.Count, "Conteo físico"),
        new(InventoryMovementType.Waste, "Merma"),
        new(InventoryMovementType.Correction, "Corrección")
    ];
    public IReadOnlyList<NamedOption<DeliveryStatus>> DeliveryStatuses { get; } =
        Enum.GetValues<DeliveryStatus>().Select(x => new NamedOption<DeliveryStatus>(x, x.ToString())).ToList();
    public IReadOnlyList<NamedOption<ManualTransactionType>> ManualTransactionTypes { get; } =
    [
        new(ManualTransactionType.Income, "INGRESO"),
        new(ManualTransactionType.Expense, "GASTO")
    ];

    [ObservableProperty] private string comments = "";
    [ObservableProperty] private string customerName = "";
    [ObservableProperty] private string customerPhone = "";
    [ObservableProperty] private string deliveryAddress = "";
    [ObservableProperty] private bool cashOnDelivery;
    [ObservableProperty] private DeliveryOptionItem? selectedDeliveryOption;
    [ObservableProperty] private CatalogSection selectedCatalogSection = CatalogSection.ColdBeverages;
    [ObservableProperty] private string statusMessage = "Listo";

    [ObservableProperty] private string historySearch = "";
    [ObservableProperty] private string historyFromText = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private string historyToText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private Order? selectedHistoryOrder;
    [ObservableProperty] private string cancellationReason = "";
    [ObservableProperty] private Payment? selectedPayment;
    [ObservableProperty] private NamedOption<PaymentMethod>? selectedPaymentMethod;
    [ObservableProperty] private decimal paymentAmount;
    [ObservableProperty] private string paymentReference = "";
    [ObservableProperty] private string paymentAdjustmentReason = "";
    [ObservableProperty] private NamedOption<DeliveryStatus>? selectedDeliveryStatus;
    [ObservableProperty] private string selectedRiderName = "";
    [ObservableProperty] private string selectedPromisedAtText = "";
    [ObservableProperty] private bool selectedCashOnDelivery;

    [ObservableProperty] private string manualTransactionConcept = "";
    [ObservableProperty] private decimal manualTransactionAmount;
    [ObservableProperty] private NamedOption<ManualTransactionType>? selectedManualTransactionType;
    [ObservableProperty] private string manualReversalReason = "";
    [ObservableProperty] private string cashDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private CashDailyReport cashReport = CashDailyReport.Empty;

    [ObservableProperty] private AdminPage adminPage = AdminPage.Home;
    [ObservableProperty] private Guid? adminEditingProductId;
    [ObservableProperty] private string adminProductName = "";
    [ObservableProperty] private NamedOption<ProductCategory>? selectedAdminProductCategory;
    [ObservableProperty] private decimal adminProductPrice;
    [ObservableProperty] private int adminInitialStock;
    [ObservableProperty] private int adminLowStockThreshold = 5;
    [ObservableProperty] private Guid? adminEditingDeliveryOptionId;
    [ObservableProperty] private string adminDeliveryName = "";
    [ObservableProperty] private NamedOption<DeliveryType>? selectedAdminDeliveryType;
    [ObservableProperty] private decimal adminDeliveryFee;

    [ObservableProperty] private string inventoryDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private ProductStockItem? selectedInventoryProduct;
    [ObservableProperty] private NamedOption<InventoryMovementType>? selectedAdjustmentType;
    [ObservableProperty] private int inventoryAdjustmentQuantity;
    [ObservableProperty] private string inventoryAdjustmentReason = "";
    [ObservableProperty] private string inventoryAdjustmentNotes = "";
    [ObservableProperty] private string inventorySupplier = "";
    [ObservableProperty] private string inventoryReference = "";

    [ObservableProperty] private string metricsFromText = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private string metricsToText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    [ObservableProperty] private BusinessMetrics metrics = BusinessMetrics.Empty;
    [ObservableProperty] private UpdateInfo? availableUpdate;

    public bool IsNotesSection => SelectedCatalogSection == CatalogSection.Notes;
    public bool IsProductSection => !IsNotesSection;
    public bool RequiresDeliveryAddress => SelectedDeliveryOption?.Type is DeliveryType.Delivery or DeliveryType.Walking;
    public bool IsAdminHome => AdminPage == AdminPage.Home;
    public bool IsAdminProducts => AdminPage == AdminPage.Products;
    public bool IsAdminInventory => AdminPage == AdminPage.Inventory;
    public bool IsAdminMetrics => AdminPage == AdminPage.Metrics;
    public bool IsAdmin => currentUser.Current.Role == EmployeeRole.Admin;
    public string CurrentUserRole => $"{currentUser.Current.DisplayName} · {currentUser.Current.Role}";
    public bool CanEditSelectedOrder => SelectedHistoryOrder?.Status == OrderStatus.Open;
    public bool CanCancelSelectedOrder => SelectedHistoryOrder is { Status: not OrderStatus.Cancelled };
    public bool CanUpdateSelectedDelivery => SelectedHistoryOrder is { Status: not OrderStatus.Cancelled, DeliveryType: not DeliveryType.Pickup };
    public bool HasManualTransactions => ManualTransactions.Count > 0;
    public bool IsManualTransactionHistoryEmpty => !HasManualTransactions;
    public bool HasAvailableUpdate => AvailableUpdate is not null;
    public decimal Subtotal => Cart.Sum(x => x.Total);
    public decimal Total => Subtotal + (SelectedDeliveryOption?.Fee ?? 0m);
    public int TotalItems => Cart.Sum(x => x.Quantity);
    public decimal SelectedPaymentBalance => SelectedHistoryOrder?.BalanceDue ?? 0m;
    public string SelectedOrderTitle => SelectedHistoryOrder is null ? "Seleccione una orden" : $"Orden #{SelectedHistoryOrder.DailyNumber}";
    public string SelectedOrderSummary => SelectedHistoryOrder is null ? "" :
        $"{SelectedHistoryOrder.Status} | Total {SelectedHistoryOrder.Total:C} | Pagado {SelectedHistoryOrder.PaidAmount:C} | Saldo {SelectedHistoryOrder.BalanceDue:C}";

    public async Task InitializeAsync()
    {
        allCatalogItems.Clear();
        allCatalogItems.AddRange(await catalog.GetAvailableItemsAsync());
        DeliveryOptions.Clear();
        foreach (var option in await catalog.GetDeliveryOptionsAsync()) DeliveryOptions.Add(option);
        SelectedDeliveryOption = DeliveryOptions.FirstOrDefault();
        SelectedPaymentMethod = PaymentMethods.First();
        SelectedManualTransactionType = ManualTransactionTypes.First();
        SelectedAdminProductCategory = ProductCategories.First();
        SelectedAdminDeliveryType = DeliveryTypes.First();
        SelectedAdjustmentType = AdjustmentTypes.First();
        RefreshCatalog();
        await RefreshHistory();
        await RefreshManualTransactions();
        await RefreshCashReport();
        if (settings.Current.CheckUpdates) await CheckForUpdate();
    }

    [RelayCommand]
    private void SelectCatalogSection(CatalogSection section)
    {
        SelectedCatalogSection = section;
        RefreshCatalog();
        OnPropertyChanged(nameof(IsNotesSection));
        OnPropertyChanged(nameof(IsProductSection));
    }

    [RelayCommand]
    private void AddProduct(CatalogItem product)
    {
        var line = Cart.FirstOrDefault(x => x.Product.Id == product.Id);
        if ((line?.Quantity ?? 0) >= product.StockQuantity)
        {
            StatusMessage = $"No hay más existencias de {product.Name}.";
            return;
        }
        if (line is null) Cart.Add(new CartLine(product));
        else line.Quantity++;
        TotalsChanged();
    }

    [RelayCommand]
    private void Increment(CartLine line)
    {
        if (line.Quantity >= line.Product.StockQuantity)
        {
            StatusMessage = $"No hay más existencias de {line.Product.Name}.";
            return;
        }
        line.Quantity++;
        TotalsChanged();
    }

    [RelayCommand]
    private void Decrement(CartLine line)
    {
        if (--line.Quantity <= 0) Cart.Remove(line);
        TotalsChanged();
    }

    [RelayCommand]
    private void NewOrder()
    {
        Cart.Clear();
        Comments = "";
        CustomerName = "";
        CustomerPhone = "";
        DeliveryAddress = "";
        CashOnDelivery = false;
        SelectedDeliveryOption = DeliveryOptions.FirstOrDefault();
        TotalsChanged();
        StatusMessage = "Nueva orden lista.";
    }

    [RelayCommand]
    private async Task ConfirmOrder()
    {
        try
        {
            var order = await orderService.CreateAsync(CreateDraftOrder());
            var number = order.DailyNumber;
            NewOrder();
            await RefreshCatalogAsync();
            await LoadHistoryAsync(order.Id);
            await RefreshCashReport();
            StatusMessage = $"Orden #{number} guardada; capture su desglose de pago en Historial.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshHistory() => await LoadHistoryAsync(SelectedHistoryOrder?.Id);

    [RelayCommand]
    private async Task ShowTodayHistory()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        HistoryFromText = today;
        HistoryToText = today;
        await RefreshHistory();
    }

    [RelayCommand]
    private async Task SaveSelectedOrder()
    {
        try
        {
            if (SelectedHistoryOrder is null) throw new InvalidOperationException("Seleccione una orden.");
            var saved = await orders.SaveOrderAsync(SelectedHistoryOrder);
            await LoadHistoryAsync(saved.Id);
            StatusMessage = $"Orden #{saved.DailyNumber} actualizada.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CancelSelectedOrder()
    {
        try
        {
            if (SelectedHistoryOrder is null) throw new InvalidOperationException("Seleccione una orden.");
            var cancelled = await checkout.CancelAsync(SelectedHistoryOrder.Id, CancellationReason);
            CancellationReason = "";
            await RefreshCatalogAsync();
            await LoadHistoryAsync(cancelled.Id);
            await RefreshCashReport();
            StatusMessage = $"Orden #{cancelled.DailyNumber} cancelada, inventario devuelto y cobros reembolsados.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpdateSelectedDelivery()
    {
        try
        {
            if (SelectedHistoryOrder is null || SelectedDeliveryStatus is null)
                throw new InvalidOperationException("Seleccione una orden y estado de entrega.");
            DateTime? promisedAt = null;
            if (!string.IsNullOrWhiteSpace(SelectedPromisedAtText))
            {
                if (!DateTime.TryParseExact(SelectedPromisedAtText.Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var parsed))
                    throw new InvalidOperationException("Use fecha prometida yyyy-MM-dd HH:mm.");
                promisedAt = parsed.ToUniversalTime();
            }
            var saved = await checkout.UpdateDeliveryAsync(
                SelectedHistoryOrder.Id, SelectedDeliveryStatus.Value, SelectedRiderName, promisedAt, SelectedCashOnDelivery);
            await LoadHistoryAsync(saved.Id);
            StatusMessage = $"Entrega de orden #{saved.DailyNumber} actualizada.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddPayment()
    {
        try
        {
            EnsureOpenOrderForPayment();
            if (SelectedPaymentMethod is null) throw new InvalidOperationException("Seleccione un método de pago.");
            PaymentBreakdown.Add(new Payment
            {
                Method = SelectedPaymentMethod.Value,
                Amount = PaymentAmount,
                Reference = PaymentReference,
                CreatedAt = DateTime.UtcNow
            });
            await CommitPaymentBreakdownAsync();
            ClearPaymentEditor();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpdatePayment()
    {
        try
        {
            EnsureOpenOrderForPayment();
            if (SelectedPayment is null || SelectedPaymentMethod is null)
                throw new InvalidOperationException("Seleccione un pago para editar.");
            var index = PaymentBreakdown.IndexOf(SelectedPayment);
            if (index < 0) throw new InvalidOperationException("Pago no encontrado.");
            PaymentBreakdown[index] = new Payment
            {
                Id = SelectedPayment.Id,
                Method = SelectedPaymentMethod.Value,
                Amount = PaymentAmount,
                Reference = PaymentReference,
                CreatedAt = SelectedPayment.CreatedAt
            };
            await CommitPaymentBreakdownAsync();
            ClearPaymentEditor();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ReplacePaymentBreakdown()
    {
        try
        {
            EnsureOpenOrderForPayment();
            if (SelectedPaymentMethod is null) throw new InvalidOperationException("Seleccione un método de pago.");
            PaymentBreakdown.Clear();
            PaymentBreakdown.Add(new Payment
            {
                Method = SelectedPaymentMethod.Value,
                Amount = PaymentAmount,
                Reference = PaymentReference,
                CreatedAt = DateTime.UtcNow
            });
            await CommitPaymentBreakdownAsync();
            ClearPaymentEditor();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RemovePayment()
    {
        try
        {
            EnsureOpenOrderForPayment();
            if (SelectedPayment is null) throw new InvalidOperationException("Seleccione un pago para eliminar.");
            PaymentBreakdown.Remove(SelectedPayment);
            await CommitPaymentBreakdownAsync();
            ClearPaymentEditor();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ClearPaymentBreakdown()
    {
        try
        {
            EnsureOpenOrderForPayment();
            PaymentBreakdown.Clear();
            await CommitPaymentBreakdownAsync();
            ClearPaymentEditor();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveManualTransaction()
    {
        try
        {
            if (SelectedManualTransactionType is null) throw new InvalidOperationException("Seleccione un tipo.");
            await manualTransactions.CreateAsync(ManualTransactionConcept, ManualTransactionAmount, SelectedManualTransactionType.Value);
            ManualTransactionConcept = "";
            ManualTransactionAmount = 0;
            await RefreshManualTransactions();
            await RefreshCashReport();
            StatusMessage = "Movimiento guardado.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteManualTransaction(ManualTransaction transaction)
    {
        try
        {
            await manualTransactions.ReverseAsync(transaction.Id, ManualReversalReason);
            ManualReversalReason = "";
            await RefreshManualTransactions();
            await RefreshCashReport();
            StatusMessage = "Movimiento revertido; el historial original se conserva.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenAdminProducts()
    {
        AdminPage = AdminPage.Products;
        await RefreshAdminCatalogAsync();
        BeginNewAdminProduct();
        BeginNewAdminDeliveryOption();
    }

    [RelayCommand] private void CloseAdminProducts() => AdminPage = AdminPage.Home;

    [RelayCommand]
    private async Task OpenAdminInventory()
    {
        AdminPage = AdminPage.Inventory;
        await RefreshInventory();
    }

    [RelayCommand] private void CloseAdminInventory() => AdminPage = AdminPage.Home;

    [RelayCommand]
    private async Task OpenAdminMetrics()
    {
        AdminPage = AdminPage.Metrics;
        await RefreshMetrics();
    }

    [RelayCommand] private void CloseAdminMetrics() => AdminPage = AdminPage.Home;

    [RelayCommand] private void NewAdminProduct() => BeginNewAdminProduct();
    [RelayCommand] private void NewAdminDeliveryOption() => BeginNewAdminDeliveryOption();

    [RelayCommand]
    private void EditAdminProduct(AdminProductItem product)
    {
        AdminEditingProductId = product.Id;
        AdminProductName = product.Name;
        SelectedAdminProductCategory = ProductCategories.Single(x => x.Value == product.Category);
        AdminProductPrice = product.Price;
        AdminInitialStock = product.StockQuantity;
        AdminLowStockThreshold = product.LowStockThreshold;
    }

    [RelayCommand]
    private async Task SaveAdminProduct()
    {
        try
        {
            if (SelectedAdminProductCategory is null) throw new InvalidOperationException("Seleccione una categoría.");
            await adminCatalog.SaveProductAsync(new ProductUpsert(
                AdminEditingProductId,
                AdminProductName,
                SelectedAdminProductCategory.Value,
                AdminProductPrice,
                AdminInitialStock,
                AdminLowStockThreshold));
            await RefreshAdminCatalogAsync();
            await RefreshCatalogAsync();
            BeginNewAdminProduct();
            StatusMessage = "Producto guardado.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateAdminProduct(AdminProductItem product)
    {
        try
        {
            await adminCatalog.DeactivateProductAsync(product.Id);
            await RefreshAdminCatalogAsync();
            await RefreshCatalogAsync();
            StatusMessage = $"{product.Name} desactivado.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void EditAdminDeliveryOption(AdminDeliveryOptionItem option)
    {
        AdminEditingDeliveryOptionId = option.Id;
        AdminDeliveryName = option.Name;
        SelectedAdminDeliveryType = DeliveryTypes.Single(x => x.Value == option.Type);
        AdminDeliveryFee = option.Fee;
    }

    [RelayCommand]
    private async Task SaveAdminDeliveryOption()
    {
        try
        {
            if (SelectedAdminDeliveryType is null) throw new InvalidOperationException("Seleccione un tipo de entrega.");
            await adminCatalog.SaveDeliveryOptionAsync(new DeliveryOptionUpsert(
                AdminEditingDeliveryOptionId,
                AdminDeliveryName,
                SelectedAdminDeliveryType.Value,
                AdminDeliveryFee));
            await RefreshAdminCatalogAsync();
            await RefreshCatalogAsync();
            BeginNewAdminDeliveryOption();
            StatusMessage = "Opción de entrega guardada.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeactivateAdminDeliveryOption(AdminDeliveryOptionItem option)
    {
        try
        {
            await adminCatalog.DeactivateDeliveryOptionAsync(option.Id);
            await RefreshAdminCatalogAsync();
            await RefreshCatalogAsync();
            StatusMessage = $"{option.Name} desactivada.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshInventory()
    {
        try
        {
            var date = ParseDate(InventoryDateText, DateTime.Today);
            InventoryDateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var snapshot = await inventory.GetSnapshotAsync(date);
            InventoryProducts.Clear();
            foreach (var product in snapshot.Products) InventoryProducts.Add(product);
            InventoryMovements.Clear();
            foreach (var movement in snapshot.Movements) InventoryMovements.Add(movement);
            SelectedInventoryProduct ??= InventoryProducts.FirstOrDefault(x => x.IsActive);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveInventoryAdjustment()
    {
        try
        {
            if (SelectedInventoryProduct is null || SelectedAdjustmentType is null)
                throw new InvalidOperationException("Seleccione un producto y tipo de movimiento.");
            await inventory.AdjustAsync(
                SelectedInventoryProduct.Id,
                SelectedAdjustmentType.Value,
                InventoryAdjustmentQuantity,
                string.IsNullOrWhiteSpace(InventoryAdjustmentReason) ? InventoryAdjustmentNotes : InventoryAdjustmentReason,
                InventorySupplier,
                InventoryReference,
                ParseDate(InventoryDateText, DateTime.Today));
            InventoryAdjustmentQuantity = 0;
            InventoryAdjustmentReason = "";
            InventoryAdjustmentNotes = "";
            InventorySupplier = "";
            InventoryReference = "";
            await RefreshInventory();
            await RefreshCatalogAsync();
            StatusMessage = "Movimiento de inventario guardado.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshMetrics()
    {
        try
        {
            Metrics = await metricsService.GetAsync(
                ParseDate(MetricsFromText, DateTime.Today.AddDays(-6)),
                ParseDate(MetricsToText, DateTime.Today));
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RefreshCashReport()
    {
        try
        {
            var date = ParseDate(CashDateText, DateTime.Today);
            CashDateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            CashReport = await cashReportService.GetDailyAsync(date);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ShowTodayCash()
    {
        CashDateText = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await RefreshCashReport();
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        try
        {
            AvailableUpdate = await updates.CheckAsync();
            if (AvailableUpdate is not null)
                StatusMessage = $"Actualización {AvailableUpdate.Version} disponible.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo buscar actualización: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task InstallUpdate()
    {
        try
        {
            if (AvailableUpdate is null) throw new InvalidOperationException("No hay una actualización disponible.");
            StatusMessage = $"Descargando actualización {AvailableUpdate.Version}...";
            var package = await updates.DownloadAsync(AvailableUpdate);
            await updateInstaller.StageAndRestartAsync(package);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            StatusMessage = $"No se pudo instalar la actualización: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Backup()
    {
        var path = await backups.CreateAsync("manual");
        StatusMessage = path is null ? "Aún no existe base para respaldar." : $"Respaldo: {path}";
    }

    partial void OnSelectedHistoryOrderChanged(Order? value)
    {
        SelectedOrderItems.Clear();
        PaymentBreakdown.Clear();
        PaymentAudit.Clear();
        if (value is not null)
        {
            foreach (var item in value.Items) SelectedOrderItems.Add(item);
            foreach (var payment in value.CurrentCollections) PaymentBreakdown.Add(payment);
            foreach (var payment in value.Payments.OrderBy(x => x.CreatedAt)) PaymentAudit.Add(payment);
            SelectedDeliveryStatus = DeliveryStatuses.Single(x => x.Value == value.DeliveryStatus);
            SelectedRiderName = value.RiderName;
            SelectedPromisedAtText = value.PromisedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
            SelectedCashOnDelivery = value.CashOnDelivery;
        }
        ClearPaymentEditor();
        OnPropertyChanged(nameof(SelectedOrderTitle));
        OnPropertyChanged(nameof(SelectedOrderSummary));
        OnPropertyChanged(nameof(SelectedPaymentBalance));
        OnPropertyChanged(nameof(CanEditSelectedOrder));
        OnPropertyChanged(nameof(CanCancelSelectedOrder));
        OnPropertyChanged(nameof(CanUpdateSelectedDelivery));
    }

    partial void OnSelectedPaymentChanged(Payment? value)
    {
        if (value is null) return;
        SelectedPaymentMethod = PaymentMethods.Single(x => x.Value == value.Method);
        PaymentAmount = value.Amount;
        PaymentReference = value.Reference ?? "";
    }

    partial void OnSelectedDeliveryOptionChanged(DeliveryOptionItem? value)
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(RequiresDeliveryAddress));
    }

    partial void OnAvailableUpdateChanged(UpdateInfo? value) => OnPropertyChanged(nameof(HasAvailableUpdate));

    partial void OnAdminPageChanged(AdminPage value)
    {
        OnPropertyChanged(nameof(IsAdminHome));
        OnPropertyChanged(nameof(IsAdminProducts));
        OnPropertyChanged(nameof(IsAdminInventory));
        OnPropertyChanged(nameof(IsAdminMetrics));
    }

    private void RefreshCatalog()
    {
        Products.Clear();
        if (SelectedCatalogSection == CatalogSection.Notes) return;
        var category = SelectedCatalogSection switch
        {
            CatalogSection.ColdBeverages => ProductCategory.Beverages,
            CatalogSection.HotBeverages => ProductCategory.Coffee,
            CatalogSection.Food => ProductCategory.Food,
            CatalogSection.Desserts => ProductCategory.Desserts,
            CatalogSection.Combos => ProductCategory.Combos,
            _ => ProductCategory.Extras
        };
        foreach (var item in allCatalogItems.Where(x => x.Category == category))
            Products.Add(item);
    }

    private async Task RefreshCatalogAsync()
    {
        allCatalogItems.Clear();
        allCatalogItems.AddRange(await catalog.GetAvailableItemsAsync());
        DeliveryOptions.Clear();
        foreach (var option in await catalog.GetDeliveryOptionsAsync()) DeliveryOptions.Add(option);
        if (SelectedDeliveryOption is null || !DeliveryOptions.Any(x => x.Id == SelectedDeliveryOption.Id))
            SelectedDeliveryOption = DeliveryOptions.FirstOrDefault();
        RefreshCatalog();
    }

    private async Task LoadHistoryAsync(Guid? selectedId)
    {
        try
        {
            var from = ParseDate(HistoryFromText, DateTime.Today.AddDays(-30));
            var to = ParseDate(HistoryToText, DateTime.Today).AddDays(1);
            if (to <= from) throw new InvalidOperationException("El rango de fechas no es válido.");

            History.Clear();
            foreach (var order in await orders.GetOrdersAsync(from, to, HistorySearch)) History.Add(order);
            SelectedHistoryOrder = selectedId is null
                ? History.FirstOrDefault()
                : History.FirstOrDefault(x => x.Id == selectedId) ?? History.FirstOrDefault();
            StatusMessage = $"Historial actualizado: {History.Count} órdenes.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task CommitPaymentBreakdownAsync()
    {
        if (SelectedHistoryOrder is null) throw new InvalidOperationException("Seleccione una orden.");
        var order = await checkout.ReplacePaymentsAsync(
            SelectedHistoryOrder.Id, PaymentBreakdown.ToList(), PaymentAdjustmentReason);
        await LoadHistoryAsync(order.Id);
        await RefreshCashReport();
        StatusMessage = order.Status == OrderStatus.Paid
            ? $"Orden #{order.DailyNumber} liquidada."
            : $"Desglose guardado. Saldo pendiente: {order.BalanceDue:C}.";
    }

    private void EnsureOpenOrderForPayment()
    {
        if (SelectedHistoryOrder?.Status != OrderStatus.Open)
            throw new InvalidOperationException("Seleccione una orden abierta.");
    }

    private void ClearPaymentEditor()
    {
        SelectedPayment = null;
        PaymentAmount = 0;
        PaymentReference = "";
        PaymentAdjustmentReason = "";
        SelectedPaymentMethod ??= PaymentMethods.FirstOrDefault();
    }

    private async Task RefreshManualTransactions()
    {
        ManualTransactions.Clear();
        foreach (var transaction in await manualTransactions.GetAllAsync()) ManualTransactions.Add(transaction);
        OnPropertyChanged(nameof(HasManualTransactions));
        OnPropertyChanged(nameof(IsManualTransactionHistoryEmpty));
    }

    private async Task RefreshAdminCatalogAsync()
    {
        adminCatalogData = await adminCatalog.GetAsync();
        AdminProducts.Clear();
        foreach (var product in adminCatalogData.Products) AdminProducts.Add(product);
        AdminDeliveryOptions.Clear();
        foreach (var option in adminCatalogData.DeliveryOptions) AdminDeliveryOptions.Add(option);
    }

    private void BeginNewAdminProduct()
    {
        AdminEditingProductId = null;
        AdminProductName = "";
        AdminProductPrice = 0;
        AdminInitialStock = 0;
        AdminLowStockThreshold = 5;
        SelectedAdminProductCategory = ProductCategories.FirstOrDefault();
    }

    private void BeginNewAdminDeliveryOption()
    {
        AdminEditingDeliveryOptionId = null;
        AdminDeliveryName = "";
        AdminDeliveryFee = 0;
        SelectedAdminDeliveryType = DeliveryTypes.FirstOrDefault();
    }

    private Order CreateDraftOrder() => new()
    {
        EmployeeId = currentUser.Current.EmployeeId,
        Comments = Comments.Trim(),
        CustomerName = CustomerName.Trim(),
        CustomerPhone = CustomerPhone.Trim(),
        DeliveryOptionId = SelectedDeliveryOption?.Id,
        DeliveryOptionName = SelectedDeliveryOption?.Name ?? "Recoge en tienda",
        DeliveryType = SelectedDeliveryOption?.Type ?? DeliveryType.Pickup,
        DeliveryStatus = (SelectedDeliveryOption?.Type ?? DeliveryType.Pickup) == DeliveryType.Pickup
            ? DeliveryStatus.None
            : DeliveryStatus.Preparing,
        DeliveryFee = SelectedDeliveryOption?.Fee ?? 0m,
        DeliveryAddress = DeliveryAddress.Trim(),
        CashOnDelivery = CashOnDelivery,
        Items = Cart.Select(x => new OrderItem
        {
            ProductId = x.Product.Id,
            ProductName = x.Product.Name,
            Category = x.Product.Category,
            UnitPrice = x.Product.Price,
            Quantity = x.Quantity
        }).ToList()
    };

    private static DateTime ParseDate(string value, DateTime fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback.Date;
        if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        throw new InvalidOperationException("Use fechas con formato yyyy-MM-dd.");
    }

    private void TotalsChanged()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalItems));
    }

    private static string CategoryLabel(ProductCategory category) => category switch
    {
        ProductCategory.Coffee => "Bebidas Calientes",
        ProductCategory.Beverages => "Bebidas Frías",
        ProductCategory.Food => "Comida",
        ProductCategory.Desserts => "Postres",
        ProductCategory.Combos => "Combos",
        _ => "Extras"
    };

    private static string DeliveryLabel(DeliveryType type) => type switch
    {
        DeliveryType.Pickup => "Recoger",
        DeliveryType.Walking => "Caminando",
        _ => "Entrega"
    };

    private static string PaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Efectivo",
        PaymentMethod.Transfer => "Transferencia",
        _ => "Tarjeta"
    };
}

public sealed record NamedOption<T>(T Value, string Name);

public partial class CartLine(CatalogItem product) : ObservableObject
{
    public CatalogItem Product { get; } = product;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private int quantity = 1;
    public decimal Total => Product.Price * Quantity;
}
