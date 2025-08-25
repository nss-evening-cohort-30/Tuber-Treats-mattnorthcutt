using TuberTreats.Models;
using TuberTreats.Models.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

List<Customer> customers = new List<Customer>
{
    new Customer { Id = 1, Name = "Pat Potato", Address = "1 Spud St" },
    new Customer { Id = 2, Name = "Ida Ho",     Address = "2 Yukon Ave" }
};

List<TuberDriver> drivers = new List<TuberDriver>
{
    new TuberDriver { Id = 1, Name = "Chip Rider" },
    new TuberDriver { Id = 2, Name = "Fry Deliver" }
};

List<Topping> toppings = new List<Topping>
{
    new Topping { Id = 1, Name = "Butter" },
    new Topping { Id = 2, Name = "Sour Cream" },
    new Topping { Id = 3, Name = "Chives" },
    new Topping { Id = 4, Name = "Bacon Bits" }
};

List<TuberOrder> orders = new List<TuberOrder>
{
    new TuberOrder { Id = 1, OrderPlacedOnDate = DateTime.Now.AddMinutes(-30), CustomerId = 1, TuberDriverId = 1 },
    new TuberOrder { Id = 2, OrderPlacedOnDate = DateTime.Now.AddMinutes(-10), CustomerId = 2, TuberDriverId = null }
};

List<TuberTopping> tuberToppings = new List<TuberTopping>
{
    new TuberTopping { Id = 1, TuberOrderId = 1, ToppingId = 1 },
    new TuberTopping { Id = 2, TuberOrderId = 1, ToppingId = 4 },
    new TuberTopping { Id = 3, TuberOrderId = 2, ToppingId = 2 }
};

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/toppings", () =>
{
    var list = toppings.Select(t => new Topping { Id = t.Id, Name = t.Name }).ToList();
    return Results.Ok(list);
});

app.MapGet("/tuberdrivers", () =>
{
    var list = drivers.Select(d => new TuberDriver { Id = d.Id, Name = d.Name }).ToList();
    return Results.Ok(list);
});

app.MapGet("/customers", () =>
{
    var list = customers.Select(c => new Customer { Id = c.Id, Name = c.Name, Address = c.Address }).ToList();
    return Results.Ok(list);
});

app.MapGet("/tubertoppings", () =>
{
    var list = tuberToppings.Select(tt => new TuberTopping { Id = tt.Id, TuberOrderId = tt.TuberOrderId, ToppingId = tt.ToppingId }).ToList();
    return Results.Ok(list);
});

app.MapGet("/tuberorders", () =>
{
    var list = orders.Select(o => new TuberOrder { Id = o.Id, OrderPlacedOnDate = o.OrderPlacedOnDate, CustomerId = o.CustomerId, DeliveredOnDate = o.DeliveredOnDate, TuberDriverId = o.TuberDriverId, Toppings = o.Toppings }).ToList();
    return Results.Ok(list);
});

app.MapGet("/tuberorders/{id:int}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order == null) return Results.NotFound();

    var customer = customers.FirstOrDefault(c => c.Id == order.CustomerId);

    var driver = order.TuberDriverId.HasValue ? drivers.FirstOrDefault(d => d.Id == order.TuberDriverId.Value) : null;

    var toppingList = tuberToppings.Where(tt => tt.TuberOrderId == order.Id).Join(toppings, tt => tt.ToppingId, t => t.Id, (tt, t) => new Topping { Id = t.Id, Name = t.Name }).ToList();

    var response = new
    {
        Id = order.Id,
        OrderPlacedOnDate = order.OrderPlacedOnDate,
        DeliveredOnDate = order.DeliveredOnDate,

        CustomerId = order.CustomerId,
        TuberDriverId = order.TuberDriverId,

        Customer = customer == null ? null : new
        {
            Id = customer.Id,
            Name = customer.Name,
            Address = customer.Address
        },

        Driver = driver == null ? null : new
        {
            Id = driver.Id,
            Name = driver.Name
        },

        Toppings = toppingList
    };

    return Results.Ok(response);
});

app.MapPost("/tuberorders", (TuberOrder newOrder) =>
{
    // Give the new order an ID (auto increment based on max id)
    newOrder.Id = orders.Count > 0 ? orders.Max(o => o.Id) + 1 : 1;
    newOrder.OrderPlacedOnDate = DateTime.Now;
    orders.Add(newOrder);

    return Results.Created($"/tuberorders/{newOrder.Id}", newOrder);
});

app.MapPut("/tuberorders/{id}", (int id, int driverId) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order == null) return Results.NotFound();

    order.TuberDriverId = driverId;
    return Results.Ok(order);
});

app.MapPost("/tuberorders/{id}/complete", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    if (order == null) return Results.NotFound();

    order.DeliveredOnDate = DateTime.Now;
    return Results.Ok(order);
});

app.MapGet("/toppings/{id:int}", (int id) =>
{
    var topping = toppings.FirstOrDefault(t => t.Id == id);
    if (topping == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(topping);
});

app.MapPost("/tubertoppings", (TuberTopping newTuberTopping) =>
{
    newTuberTopping.Id = tuberToppings.Any() ? tuberToppings.Max(tt => tt.Id) + 1 : 1;

    tuberToppings.Add(newTuberTopping);

    return Results.Created($"/tubertoppings/{newTuberTopping.Id}", newTuberTopping);
});

app.MapDelete("/tubertoppings/{id:int}", (int id) =>
{
    var tt = tuberToppings.FirstOrDefault(t => t.Id == id);
    if (tt == null) return Results.NotFound();

    tuberToppings.Remove(tt);
    return Results.NoContent();
});

app.MapGet("/customers/{id:int}", (int id) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null) return Results.NotFound();

    var customerOrders = orders.Where(o => o.CustomerId == customer.Id).Select(o => new TuberOrder
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        TuberDriverId = o.TuberDriverId,
        OrderPlacedOnDate = o.OrderPlacedOnDate,
        DeliveredOnDate = o.DeliveredOnDate,
        Toppings = o.Toppings
    }).ToList();

    var result = new Customer
    {
        Id = customer.Id,
        Name = customer.Name,
        Address = customer.Address,
        TuberOrders = customerOrders
    };

    return Results.Ok(result);
});

app.MapPost("/customers", (Customer newCustomer) =>
{
    newCustomer.Id = customers.Any() ? customers.Max(c => c.Id) + 1 : 1;
    customers.Add(newCustomer);

    return Results.Created($"/customers/{newCustomer.Id}", newCustomer);
});

app.MapDelete("/customers/{id:int}", (int id) =>
{
    var customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }

    customers.Remove(customer);

    return Results.NoContent();
});

app.MapGet("/tuberdrivers/{id:int}", (int id) =>
{
    var driver = drivers.FirstOrDefault(d => d.Id == id);
    if (driver == null)
    {
        return Results.NotFound();
    }

    var deliveries = orders.Where(o => o.TuberDriverId == id).Select(o =>
        {
            return new TuberOrder
            {
                Id = o.Id,
                OrderPlacedOnDate = o.OrderPlacedOnDate,
                DeliveredOnDate = o.DeliveredOnDate,
                CustomerId = o.CustomerId,
                TuberDriverId = o.TuberDriverId,
            };
        }).ToList();

    var response = new TuberDriver
    {
        Id = driver.Id,
        Name = driver.Name,
        TuberDeliveries = deliveries
    };

    return Results.Ok(response);
});

app.Run();
//don't touch or move this!
public partial class Program { }
