using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync() =>
        db.Categories
          .Include(c => c.Products)
          .AsNoTracking()
          .OrderBy(c => c.Name)
          .ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> ExistsAsync(int id) =>
        db.Categories.AnyAsync(c => c.Id == id);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
        db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower()
                                    && (excludeId == null || c.Id != excludeId));

    public Task<bool> HasProductsAsync(int id) =>
        db.Products.AnyAsync(p => p.CategoryId == id);

    public async Task AddAsync(Category category) => await db.Categories.AddAsync(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
