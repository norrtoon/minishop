using MiniShop.Models;

namespace MiniShop.DTOs;

public static class Mappings
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        Name = u.Name,
        Role = u.Role
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        ProductCount = c.Products.Count
    };

    public static ProductDto ToDto(this Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        Stock = p.Stock,
        ImageUrl = p.ImageUrl,
        CategoryId = p.CategoryId,
        CategoryName = p.Category?.Name ?? string.Empty,
        Rating = p.Rating,
        ReviewCount = p.ReviewCount
    };

    public static ReviewDto ToDto(this Review r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        ProductName = r.Product?.Name ?? string.Empty,
        UserId = r.UserId,
        UserName = r.User?.Name ?? string.Empty,
        Rating = r.Rating,
        Text = r.Text,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    public static OrderDto ToDto(this Order o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        UserName = o.User?.Name ?? string.Empty,
        UserEmail = o.User?.Email ?? string.Empty,
        CreatedAt = o.CreatedAt,
        Status = o.Status.ToString(),
        Address = o.Address,
        Comment = o.Comment,
        Total = o.Total,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}
