using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<List<Order>> GetAllAsync(int? userId = null, OrderStatus? status = null)
    {
        IQueryable<Order> query = db.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .AsNoTracking();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId);

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        return query.OrderByDescending(o => o.Id).ToListAsync();
    }

    public Task<Order?> GetByIdAsync(int id) =>
        db.Orders
          .Include(o => o.Items)
          .Include(o => o.User)
          .FirstOrDefaultAsync(o => o.Id == id);

    public Task<bool> HasReceivedProductAsync(int userId, int productId) =>
        db.Orders.AnyAsync(o => o.UserId == userId
                                && o.Status == OrderStatus.Completed
                                && o.Items.Any(i => i.ProductId == productId));

    public async Task AddAsync(Order order) => await db.Orders.AddAsync(order);

    public void Remove(Order order) => db.Orders.Remove(order);
}
