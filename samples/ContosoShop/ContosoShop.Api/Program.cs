using ContosoShop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<OrderRepository>();

var app = builder.Build();
app.MapGet("/orders/{id:guid}", (Guid id, OrderRepository repository) => repository.Find(id));
app.Run();
