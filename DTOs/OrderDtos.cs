using System.ComponentModel.DataAnnotations;
using MiniShop.Models;

namespace MiniShop.DTOs;

public class CreateOrderItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Некорректный товар")]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Корзина пуста")]
    [MinLength(1, ErrorMessage = "Корзина пуста")]
    [MaxLength(50, ErrorMessage = "Слишком много позиций в заказе")]
    public List<CreateOrderItemDto> Items { get; set; } = new();

    [Required(ErrorMessage = "Адрес доставки обязателен")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Адрес должен быть от 5 до 300 символов")]
    public string Address { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Комментарий не длиннее 500 символов")]
    public string? Comment { get; set; }
}

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Статус обязателен")]
    [EnumDataType(typeof(OrderStatus), ErrorMessage = "Неизвестный статус заказа")]
    public OrderStatus? Status { get; set; }
}

public class OrderItemDto
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
