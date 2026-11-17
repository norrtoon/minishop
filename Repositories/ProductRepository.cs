using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.DTOs;
using MiniShop.Models;

namespace MiniShop.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<PagedResult<Product>> GetPagedAsync(ProductQueryDto q)
    {
        IQueryable<Product> query = db.Products.Include(p => p.Category).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s)
                                     || (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (q.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == q.CategoryId);

        if (q.MinPrice.HasValue)
            query = query.Where(p => p.Price >= q.MinPrice);

        if (q.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= q.MaxPrice);

        if (q.InStockOnly)
            query = query.Where(p => p.Stock > 0);

        query = q.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "rating" => query.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount),
            _ => query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = total
        };
    }

    public Task<Product?> GetByIdAsync(int id) =>
        db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Product>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        return db.Products.Where(p => idList.Contains(p.Id)).ToListAsync();
    }

    public async Task AddAsync(Product product) => await db.Products.AddAsync(product);

    public void Remove(Product product) => db.Products.Remove(product);
}
