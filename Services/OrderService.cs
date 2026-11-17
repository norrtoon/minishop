using MiniShop.DTOs;
using MiniShop.Exceptions;
using MiniShop.Models;
using MiniShop.Repositories;

namespace MiniShop.Services;

public interface IOrderService
{
    Task<List<OrderDto>> GetForUserAsync(int userId);
    Task<List<OrderDto>> GetAllAsync(OrderStatus? status);
    Task<OrderDto> GetByIdAsync(int orderId, int currentUserId, bool isAdmin);
    Task<OrderDto> CreateAsync(int userId, CreateOrderDto dto);
    Task<OrderDto> UpdateStatusAsync(int orderId, OrderStatus newStatus);
    Task<OrderDto> CancelAsync(int orderId, int currentUserId, bool isAdmin);
    Task DeleteAsync(int orderId);
}

public class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IOrderService
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.New] = [OrderStatus.Processing, OrderStatus.Cancelled],
        [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    public async Task<List<OrderDto>> GetForUserAsync(int userId) =>
        (await orders.GetAllAsync(userId: userId)).Select(o => o.ToDto()).ToList();

    public async Task<List<OrderDto>> GetAllAsync(OrderStatus? status) =>
        (await orders.GetAllAsync(status: status)).Select(o => o.ToDto()).ToList();

    public async Task<OrderDto> GetByIdAsync(int orderId, int currentUserId, bool isAdmin)
    {
        var order = await GetEntityAsync(orderId);
        EnsureAccess(order, currentUserId, isAdmin);
        return order.ToDto();
    }

    public async Task<OrderDto> CreateAsync(int userId, CreateOrderDto dto)
    {
        var requested = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        var found = await products.GetByIdsAsync(requested.Select(r => r.ProductId));
        var order = new Order
        {
            UserId = userId,
            Address = dto.Address.Trim(),
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim()
        };

        foreach (var line in requested)
        {
            var product = found.FirstOrDefault(p => p.Id == line.ProductId)
                          ?? throw new BadRequestException($"Товар с id={line.ProductId} не найден.");

            if (product.Stock < line.Quantity)
                throw new BadRequestException(
                    $"Недостаточно товара «{product.Name}» на складе: доступно {product.Stock}, запрошено {line.Quantity}.");

            product.Stock -= line.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price, // цена на момент покупки
                Quantity = line.Quantity
            });
        }

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        await orders.AddAsync(order);
        await unitOfWork.SaveChangesAsync();

        return (await GetEntityAsync(order.Id)).ToDto();
    }

    public async Task<OrderDto> UpdateStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await GetEntityAsync(orderId);

        if (order.Status == newStatus)
            return order.ToDto();

        if (!AllowedTransitions[order.Status].Contains(newStatus))
            throw new BadRequestException($"Нельзя перевести заказ из статуса {order.Status} в {newStatus}.");

        if (newStatus == OrderStatus.Cancelled)
            await ReturnToStockAsync(order);

        order.Status = newStatus;
        await unitOfWork.SaveChangesAsync();
        return order.ToDto();
    }

    public async Task<OrderDto> CancelAsync(int orderId, int currentUserId, bool isAdmin)
    {
        var order = await GetEntityAsync(orderId);
        EnsureAccess(order, currentUserId, isAdmin);

        if (order.Status != OrderStatus.New && !isAdmin)
            throw new BadRequestException("Отменить можно только новый заказ. Обратитесь к администратору.");

        return await UpdateStatusAsync(orderId, OrderStatus.Cancelled);
    }

    public async Task DeleteAsync(int orderId)
    {
        var order = await GetEntityAsync(orderId);

        if (order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
            throw new ConflictException("Удалить можно только завершённый или отменённый заказ.");

        orders.Remove(order);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task ReturnToStockAsync(Order order)
    {
        var ids = order.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value);
        var items = await products.GetByIdsAsync(ids);
        foreach (var item in order.Items)
        {
            var product = items.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is not null)
                product.Stock += item.Quantity;
        }
    }

    private static void EnsureAccess(Order order, int currentUserId, bool isAdmin)
    {
        if (!isAdmin && order.UserId != currentUserId)
            throw new ForbiddenException("Это чужой заказ.");
    }

    private async Task<Order> GetEntityAsync(int id) =>
        await orders.GetByIdAsync(id) ?? throw new NotFoundException("Заказ", id);
}
