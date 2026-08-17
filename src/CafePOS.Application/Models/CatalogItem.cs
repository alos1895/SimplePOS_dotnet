using CafePOS.Domain.Enums;

namespace CafePOS.Application.Models;

public sealed record CatalogItem(Guid Id, string Name, ProductCategory Category, decimal Price, int StockQuantity);
public enum CatalogSection { ColdBeverages, HotBeverages, Food, Desserts, Extras, Notes }
public sealed record DeliveryOptionItem(Guid Id, string Name, DeliveryType Type, decimal Fee);
