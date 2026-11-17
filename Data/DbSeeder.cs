using Microsoft.EntityFrameworkCore;
using MiniShop.Models;
using MiniShop.Security;

namespace MiniShop.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        if (await db.Users.AnyAsync())
            return;

        var admin = new User { Email = "admin@shop.local", Name = "Администратор", Role = Roles.Admin, PasswordHash = hasher.Hash("Admin123") };
        var ivan = new User { Email = "user@shop.local", Name = "Иван Покупатель", PasswordHash = hasher.Hash("User123") };
        var maria = new User { Email = "maria@shop.local", Name = "Мария", PasswordHash = hasher.Hash("Maria123") };
        db.Users.AddRange(admin, ivan, maria);

        var electronics = new Category { Name = "Электроника", Description = "Гаджеты и техника" };
        var books = new Category { Name = "Книги", Description = "Художественная и техническая литература" };
        var clothes = new Category { Name = "Одежда", Description = "Одежда и аксессуары" };
        var home = new Category { Name = "Дом", Description = "Всё для дома и кухни" };
        db.Categories.AddRange(electronics, books, clothes, home);

        var laptop = P("Ноутбук ProBook 14", "14\", 16 ГБ ОЗУ, SSD 512 ГБ", 74990, 7, electronics);
        var headphones = P("Беспроводные наушники", "Шумоподавление, 30 часов работы", 8990, 25, electronics);
        var csharpBook = P("C# 12 и .NET 8", "Подробное руководство по языку и платформе", 2890, 30, books);
        var cleanCode = P("Чистый код", "Классика о качестве программного кода", 1590, 18, books);
        var hoodie = P("Худи оверсайз", "Хлопок 100%, серый", 3490, 20, clothes);
        var mugs = P("Набор кружек", "4 шт., керамика", 1290, 22, home);

        db.Products.AddRange(
            laptop, headphones, csharpBook, cleanCode, hoodie, mugs,
            P("Смартфон Nova X", "6.5\" OLED, 128 ГБ", 32990, 12, electronics),
            P("Механическая клавиатура", "Переключатели Brown, подсветка", 6490, 15, electronics),
            P("Умная колонка", "Голосовой помощник, Wi-Fi", 4990, 0, electronics),
            P("Мастер и Маргарита", "Роман М. А. Булгакова", 690, 40, books),
            P("Кроссовки Runner", "Лёгкие беговые кроссовки", 7990, 9, clothes),
            P("Рюкзак городской", "Отделение для ноутбука 15.6\"", 2990, 14, clothes),
            P("Кофеварка капельная", "1.2 л, таймер", 3990, 6, home),
            P("Настольная лампа", "LED, регулировка яркости", 1990, 11, home));

        db.Orders.AddRange(
            CompletedOrder(ivan, 20, (headphones, 1), (cleanCode, 1)),
            CompletedOrder(maria, 14, (headphones, 1), (csharpBook, 1), (laptop, 1)),
            CompletedOrder(maria, 6, (hoodie, 2), (mugs, 1)));

        db.Reviews.AddRange(
            R(maria, headphones, 5, "Звук отличный, шумодав реально работает. Заряда хватает на неделю.", 12),
            R(maria, csharpBook, 4, "Хорошая книга, но местами слишком подробно.", 11),
            R(maria, laptop, 5, null, 10),
            R(maria, hoodie, 3, "Сидит хорошо, но после стирки немного село.", 4),
            R(ivan, cleanCode, 5, "Must read для любого программиста.", 17));

        await db.SaveChangesAsync();

        foreach (var product in await db.Products.Include(p => p.Reviews).Where(p => p.Reviews.Any()).ToListAsync())
        {
            product.ReviewCount = product.Reviews.Count;
            product.Rating = Math.Round(product.Reviews.Average(r => r.Rating), 1);
        }

        await db.SaveChangesAsync();
    }

    private static Product P(string name, string description, decimal price, int stock, Category category) =>
        new() { Name = name, Description = description, Price = price, Stock = stock, Category = category };

    private static Order CompletedOrder(User user, int daysAgo, params (Product Product, int Quantity)[] lines)
    {
        var order = new Order
        {
            User = user,
            Status = OrderStatus.Completed,
            Address = "г. Москва, ул. Тверская, д. 7, кв. 12",
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
        };

        foreach (var (product, quantity) in lines)
        {
            order.Items.Add(new OrderItem
            {
                Product = product,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = quantity
            });
        }

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        return order;
    }

    private static Review R(User user, Product product, int rating, string? text, int daysAgo) =>
        new() { User = user, Product = product, Rating = rating, Text = text, CreatedAt = DateTime.UtcNow.AddDays(-daysAgo) };
}
