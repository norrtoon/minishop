using MiniShop.Data;

namespace MiniShop.Repositories;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
