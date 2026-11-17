using Microsoft.EntityFrameworkCore;
using MiniShop.Data;
using MiniShop.Models;

namespace MiniShop.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public Task<bool> EmailExistsAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email.ToLower());

    public async Task AddAsync(User user) => await db.Users.AddAsync(user);
}
