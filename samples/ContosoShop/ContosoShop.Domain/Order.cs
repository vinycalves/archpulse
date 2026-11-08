namespace ContosoShop.Domain;

public sealed record Order(Guid Id, string Customer, decimal Total);
