using System.Collections.ObjectModel; using CafePOS.Application.Interfaces; using CafePOS.Application.Services; using CafePOS.Domain.Entities; using CafePOS.Domain.Enums; using CafePOS.Infrastructure.Persistence; using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input;
namespace CafePOS.Desktop.ViewModels;

public partial class MainViewModel(IPosStore store,CheckoutService checkout,CashService cash,IBackupService backups):ObservableObject
{
 private Guid? currentOrderId;
 public ObservableCollection<Product> Products {get;}=[]; public ObservableCollection<CartLine> Cart {get;}=[]; public ObservableCollection<Order> History {get;}=[];
 [ObservableProperty] private string comments=""; [ObservableProperty] private string statusMessage="Listo"; [ObservableProperty] private decimal paymentAmount; [ObservableProperty] private string transferReference=""; [ObservableProperty] private decimal openingAmount; [ObservableProperty] private decimal movementAmount; [ObservableProperty] private string movementConcept=""; [ObservableProperty] private decimal countedAmount; [ObservableProperty] private CashSession? currentSession;
 public decimal Subtotal=>Cart.Sum(x=>x.Total); public decimal Total=>Subtotal; public decimal ExpectedCash=>CurrentSession?.ExpectedBalance??0;
 public async Task InitializeAsync(){Products.Clear();foreach(var p in await store.GetActiveProductsAsync())Products.Add(p);await RefreshHistory();CurrentSession=await store.GetOpenCashSessionAsync();OnPropertyChanged(nameof(ExpectedCash));}
 [RelayCommand] private void AddProduct(Product p){if(!CanEditCart())return;var line=Cart.FirstOrDefault(x=>x.Product.Id==p.Id);if(line is null)Cart.Add(new CartLine(p));else line.Quantity++;TotalsChanged();}
 [RelayCommand] private void Increment(CartLine line){if(!CanEditCart())return;line.Quantity++;TotalsChanged();} [RelayCommand] private void Decrement(CartLine line){if(!CanEditCart())return;if(--line.Quantity<=0)Cart.Remove(line);TotalsChanged();}
 [RelayCommand] private void NewOrder(){currentOrderId=null;Cart.Clear();Comments="";PaymentAmount=0;TransferReference="";TotalsChanged();StatusMessage="Nueva orden lista";}
 [RelayCommand] private async Task PayCash()=>await Pay(PaymentMethod.Cash); [RelayCommand] private async Task PayTransfer()=>await Pay(PaymentMethod.Transfer);
 private async Task Pay(PaymentMethod method)
 {
  try
  {
   if (Cart.Count == 0) throw new InvalidOperationException("Agregue al menos un producto.");
   var amountToPay = PaymentAmount <= 0 ? Total : PaymentAmount;
   Order resultOrder;
   if (currentOrderId is null)
   {
    var newOrder = new Order { EmployeeId = Seed.DefaultEmployeeId, Comments = Comments, Items = Cart.Select(x => new OrderItem { ProductId = x.Product.Id, ProductName = x.Product.Name, UnitPrice = x.Product.Price, Quantity = x.Quantity }).ToList() };
    resultOrder = await checkout.CreateOrderAndPayAsync(newOrder, method, amountToPay, TransferReference);
    currentOrderId = resultOrder.Id;
   }
   else
   {
    resultOrder = await checkout.AddPaymentAsync(currentOrderId.Value, method, amountToPay, TransferReference);
   }
   PaymentAmount = resultOrder.BalanceDue;
   TransferReference = "";
   if (resultOrder.Status == OrderStatus.Paid) { var number = resultOrder.DailyNumber; NewOrder(); StatusMessage = $"Orden #{number} cobrada."; }
   else { StatusMessage = $"Pago parcial guardado. Resta {resultOrder.BalanceDue:C}."; }
   await RefreshHistory();
   CurrentSession = await store.GetOpenCashSessionAsync();
   OnPropertyChanged(nameof(ExpectedCash));
  }
  catch (Exception ex) { StatusMessage = ex.Message; }
 }
 [RelayCommand] private async Task RefreshHistory(){History.Clear();var now=DateTimeOffset.Now;foreach(var o in await store.GetOrdersAsync(now.Date.AddDays(-30),now.Date.AddDays(1),null))History.Add(o);}
 [RelayCommand] private async Task OpenCash(){try{CurrentSession=await cash.OpenAsync(Seed.DefaultEmployeeId,1,OpeningAmount);StatusMessage="Caja abierta";OnPropertyChanged(nameof(ExpectedCash));}catch(Exception ex){StatusMessage=ex.Message;}}
 [RelayCommand] private async Task AddIncome()=>await Movement(CashMovementType.ManualIncome); [RelayCommand] private async Task AddExpense()=>await Movement(CashMovementType.ManualExpense);
 private async Task Movement(CashMovementType type){try{CurrentSession=await cash.AddMovementAsync(type,MovementAmount,MovementConcept);MovementAmount=0;MovementConcept="";OnPropertyChanged(nameof(ExpectedCash));}catch(Exception ex){StatusMessage=ex.Message;}}
 [RelayCommand] private async Task CloseCash(){try{CurrentSession=await cash.CloseAsync(CountedAmount);StatusMessage=$"Corte guardado. Esperado: {CurrentSession.ExpectedAtClose:C}; diferencia: {CountedAmount-CurrentSession.ExpectedAtClose:C}";OnPropertyChanged(nameof(ExpectedCash));}catch(Exception ex){StatusMessage=ex.Message;}}
 [RelayCommand] private async Task Backup(){var path=await backups.CreateAsync("manual");StatusMessage=path is null?"Aún no existe base para respaldar.":$"Backup: {path}";}
 private void TotalsChanged(){OnPropertyChanged(nameof(Subtotal));OnPropertyChanged(nameof(Total));if(PaymentAmount==0)PaymentAmount=Total;}
 private bool CanEditCart(){if(currentOrderId is null)return true;StatusMessage="La orden ya tiene un pago. Complete el cobro o pulse Nueva orden; sus importes guardados no se modificarán.";return false;}
}
public partial class CartLine(Product product):ObservableObject { public Product Product{get;}=product;[ObservableProperty][NotifyPropertyChangedFor(nameof(Total))]private int quantity=1;public decimal Total=>Product.Price*Quantity; }
