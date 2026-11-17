using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Repositories;

public class ReviewRepository(AppDbContext db) : IReviewRepository
{
    public Task<List<Review>> GetForProductAsync(int productId) =>
        db.Reviews
          .Include(r => r.User)
          .AsNoTracking()
          .Where(r => r.ProductId == productId)
          .OrderByDescending(r => r.CreatedAt)
          .ToListAsync();

    public Task<List<Review>> GetLatestAsync(int count) =>
        db.Reviews
          .Include(r => r.User)
          .Include(r => r.Product)
          .AsNoTracking()
          .OrderByDescending(r => r.CreatedAt)
          .Take(count)
          .ToListAsync();

    public Task<Review?> GetByIdAsync(int id) =>
        db.Reviews
          .Include(r => r.User)
          .Include(r => r.Product)
          .FirstOrDefaultAsync(r => r.Id == id);

    public Task<bool> ExistsAsync(int productId, int userId) =>
        db.Reviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId);

    public async Task<(double Average, int Count)> GetStatsAsync(int productId)
    {
        var ratings = db.Reviews.Where(r => r.ProductId == productId).Select(r => r.Rating);
        var count = await ratings.CountAsync();
        var average = count == 0 ? 0 : await ratings.AverageAsync();
        return (Math.Round(average, 1), count);
    }

    public async Task AddAsync(Review review) => await db.Reviews.AddAsync(review);

    public void Remove(Review review) => db.Reviews.Remove(review);
}
