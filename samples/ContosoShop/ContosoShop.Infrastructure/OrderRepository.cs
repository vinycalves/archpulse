namespace ContosoShop.Infrastructure;

public sealed class OrderRepository
{
    private readonly Dictionary<Guid, object> _orders = [];

    public object? Find(Guid id) => _orders.GetValueOrDefault(id);
}
