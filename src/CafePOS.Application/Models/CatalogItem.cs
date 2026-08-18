using CafePOS.Domain.Enums;

namespace CafePOS.Application.Models;

public sealed record CatalogItem(Guid Id, string Name, ProductCategory Category, decimal Price);
public enum CatalogSection { ColdBeverages, HotBeverages, Food, Desserts, Combos, Extras, Notes }
public sealed record DeliveryOptionItem(Guid Id, string Name, DeliveryType Type, decimal Fee);
