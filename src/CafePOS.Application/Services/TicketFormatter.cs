using System.Globalization;
using System.Text;
using CafePOS.Application.Interfaces;
using CafePOS.Domain.Entities;
using CafePOS.Domain.Enums;

namespace CafePOS.Application.Services;

public sealed class TicketFormatter(ISettingsService settings)
{
    private const int Width = 42;

    public string Kitchen(Order order)
    {
        var text = new StringBuilder();
        Center(text, "COMANDA COCINA");
        Center(text, $"ORDEN #{order.DailyNumber}");
        Line(text);
        text.AppendLine($"Fecha: {LocalTime(order.CreatedAt):dd/MM/yyyy HH:mm}");
        text.AppendLine($"Entrega: {order.DeliveryOptionName}");
        if (!string.IsNullOrWhiteSpace(order.CustomerName)) text.AppendLine($"Cliente: {order.CustomerName}");
        Line(text);
        foreach (var item in order.Items)
        {
            text.AppendLine($"{item.Quantity} x {item.ProductName}");
            if (!string.IsNullOrWhiteSpace(item.Notes)) text.AppendLine($"  > {item.Notes}");
        }
        if (!string.IsNullOrWhiteSpace(order.Comments))
        {
            Line(text);
            text.AppendLine("INDICACIONES:");
            text.AppendLine(order.Comments);
        }
        Line(text);
        text.AppendLine("\n\n");
        return text.ToString();
    }

    public string Customer(Order order)
    {
        var text = new StringBuilder();
        Center(text, settings.Current.StoreName);
        Center(text, "TICKET DE VENTA");
        text.AppendLine($"Orden #{order.DailyNumber}");
        text.AppendLine($"Fecha: {LocalTime(order.CreatedAt):dd/MM/yyyy HH:mm}");
        if (!string.IsNullOrWhiteSpace(order.CustomerName)) text.AppendLine($"Cliente: {order.CustomerName}");
        Line(text);
        foreach (var item in order.Items)
        {
            text.AppendLine($"{item.Quantity} x {item.ProductName}");
            text.AppendLine(Right(item.LineTotal.ToString("C", CultureInfo.CurrentCulture)));
        }
        Line(text);
        text.AppendLine(Right($"Subtotal: {order.Subtotal.ToString("C", CultureInfo.CurrentCulture)}"));
        if (order.DeliveryFee != 0) text.AppendLine(Right($"Entrega: {order.DeliveryFee.ToString("C", CultureInfo.CurrentCulture)}"));
        text.AppendLine(Right($"TOTAL: {order.Total.ToString("C", CultureInfo.CurrentCulture)}"));
        var payment = order.CurrentCollections.LastOrDefault();
        if (payment is not null) text.AppendLine($"Pago: {(payment.Method == PaymentMethod.Cash ? "Efectivo" : "Tarjeta")}");
        Line(text);
        Center(text, "Gracias por su compra");
        text.AppendLine("\n\n");
        return text.ToString();
    }

    private static DateTime LocalTime(DateTime value) => value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    private static void Line(StringBuilder text) => text.AppendLine(new string('-', Width));
    private static void Center(StringBuilder text, string value) => text.AppendLine(value.Length >= Width ? value : value.PadLeft((Width + value.Length) / 2));
    private static string Right(string value) => value.Length >= Width ? value : value.PadLeft(Width);
}
