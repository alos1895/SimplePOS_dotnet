using System.Text; using CafePOS.Application.Interfaces; using CafePOS.Domain.Entities; using Microsoft.Extensions.Logging;
namespace CafePOS.Infrastructure.Printing;
public sealed class DebugReceiptPrinter(IDataPathProvider paths,ISettingsService settings,ILogger<DebugReceiptPrinter> log):IReceiptPrinter
{
 public async Task PrintAsync(Order order,CancellationToken ct=default) { var dir=Path.Combine(paths.DataDirectory,"receipts");Directory.CreateDirectory(dir);var b=new StringBuilder().AppendLine(settings.Current.StoreName).AppendLine($"Orden #{order.DailyNumber}").AppendLine(order.CreatedAt.ToLocalTime().ToString("g")).AppendLine(new string('-',32));foreach(var x in order.Items)b.AppendLine($"{x.Quantity}x {x.ProductName}  {x.LineTotal:C}");b.AppendLine(new string('-',32)).AppendLine($"TOTAL {order.Total:C}").AppendLine(settings.Current.TicketFooter);var file=Path.Combine(dir,$"ticket-{order.DailyNumber}-{order.CreatedAt:yyyyMMddHHmmss}.txt");await File.WriteAllTextAsync(file,b.ToString(),ct);log.LogInformation("Receipt written to {File}",file); }
}
