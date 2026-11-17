using MiniShop.DTOs;
using MiniShop.Models;

namespace MiniShop.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
}

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<bool> HasProductsAsync(int id);
    Task AddAsync(Category category);
    void Remove(Category category);
}

public interface IProductRepository
{
    Task<PagedResult<Product>> GetPagedAsync(ProductQueryDto query);
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids);
    Task AddAsync(Product product);
    void Remove(Product product);
}

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(int? userId = null, OrderStatus? status = null);
    Task<Order?> GetByIdAsync(int id);
    Task<bool> HasReceivedProductAsync(int userId, int productId);
    Task AddAsync(Order order);
    void Remove(Order order);
}

public interface IReviewRepository
{
    Task<List<Review>> GetForProductAsync(int productId);
    Task<List<Review>> GetLatestAsync(int count);
    Task<Review?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int productId, int userId);
    Task<(double Average, int Count)> GetStatsAsync(int productId);
    Task AddAsync(Review review);
    void Remove(Review review);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
