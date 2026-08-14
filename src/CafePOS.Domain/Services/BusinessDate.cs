using System.Globalization;

namespace CafePOS.Domain.Services;

public static class BusinessDate
{
    public static string Today => FromUtc(DateTime.UtcNow);

    public static string FromUtc(DateTime value) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeZoneInfo.Local)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FromLocalDate(DateTime value) =>
        value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public static class CustomerDetails
{
    public static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is < 10 or > 15)
            throw new InvalidOperationException("Capture un teléfono válido de 10 a 15 dígitos.");
        return $"+{digits}";
    }
}
