namespace Northwind.Api.Repositories;

using Northwind.Api.Models;

/// <summary>
/// Deterministic seed data: fixed ids and dates, so tests, screenshots and the
/// static explorer agree about every row.
/// </summary>
public static class SeedData
{
    public static Guid Guid(string suffix) => System.Guid.Parse($"00000000-0000-0000-0000-{suffix.PadLeft(12, '0')}");

    public static IReadOnlyList<Customer> Customers() => new List<Customer>
    {
        new(Guid("000101"), "Aarav Sharma", "aarav.sharma@example.com", "Nepal", "enterprise", At(2026, 1, 8)),
        new(Guid("000102"), "Mira Karki", "mira.karki@example.com", "Nepal", "growth", At(2026, 2, 14)),
        new(Guid("000103"), "Daniel Okafor", "daniel.okafor@example.com", "Nigeria", "growth", At(2026, 3, 2)),
        new(Guid("000104"), "Sofia Rossi", "sofia.rossi@example.com", "Italy", "free", At(2026, 3, 21)),
        new(Guid("000105"), "Kenji Watanabe", "kenji.watanabe@example.com", "Japan", "enterprise", At(2026, 4, 9)),
        new(Guid("000106"), "Priya Nair", "priya.nair@example.com", "India", "growth", At(2026, 5, 4)),
        new(Guid("000107"), "Lucas Meyer", "lucas.meyer@example.com", "Germany", "free", At(2026, 5, 26)),
        new(Guid("000108"), "Amara Diallo", "amara.diallo@example.com", "Senegal", "growth", At(2026, 6, 17)),
        new(Guid("000109"), "Nora Jensen", "nora.jensen@example.com", "Denmark", "enterprise", At(2026, 7, 1)),
        new(Guid("000110"), "Rafael Costa", "rafael.costa@example.com", "Brazil", "free", At(2026, 8, 12)),
        new(Guid("000111"), "Hana Kim", "hana.kim@example.com", "South Korea", "growth", At(2026, 9, 3)),
        new(Guid("000112"), "Elias Haddad", "elias.haddad@example.com", "Lebanon", "free", At(2026, 10, 6)),
    };

    private static readonly (Guid Id, string Name, decimal Price)[] Products =
    {
        (Guid("000201"), "Khukuri chef knife", 89.00m),
        (Guid("000202"), "Pashmina throw", 64.50m),
        (Guid("000203"), "Cast iron skillet", 42.00m),
        (Guid("000204"), "Himalayan tea sampler", 28.75m),
        (Guid("000205"), "Leather field journal", 36.00m),
        (Guid("000206"), "Copper water bottle", 51.25m),
        (Guid("000207"), "Wool hiking socks", 19.90m),
        (Guid("000208"), "Bamboo desk tray", 24.00m),
    };

    public static IReadOnlyList<Order> Orders()
    {
        var customers = Customers();
        var orders = new List<Order>();
        var statuses = new[]
        {
            OrderStatus.Delivered, OrderStatus.Delivered, OrderStatus.Shipped, OrderStatus.Paid,
            OrderStatus.Pending, OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Paid,
        };

        for (var index = 0; index < 24; index++)
        {
            var customer = customers[index % customers.Count];
            var first = Products[index % Products.Length];
            var second = Products[(index + 3) % Products.Length];
            var lines = new List<OrderLine>
            {
                new(first.Id, first.Name, 1 + (index % 3), first.Price),
                new(second.Id, second.Name, 1, second.Price),
            };
            if (index % 4 == 0)
            {
                var third = Products[(index + 5) % Products.Length];
                lines.Add(new OrderLine(third.Id, third.Name, 2, third.Price));
            }

            orders.Add(new Order(
                Guid($"0003{index + 1:00}"),
                customer.Id,
                lines,
                statuses[index % statuses.Length],
                At(2026, 1 + (index % 9), 1 + (index % 27))));
        }

        return orders;
    }

    private static DateTimeOffset At(int year, int month, int day) =>
        new(year, month, Math.Clamp(day, 1, 28), 9, 30, 0, TimeSpan.Zero);
}
