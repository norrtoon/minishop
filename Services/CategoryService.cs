using MiniShop.DTOs;
using MiniShop.Exceptions;
using MiniShop.Models;
using MiniShop.Repositories;

namespace MiniShop.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryUpsertDto dto);
    Task<CategoryDto> UpdateAsync(int id, CategoryUpsertDto dto);
    Task DeleteAsync(int id);
}

public class CategoryService(ICategoryRepository categories, IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync() =>
        (await categories.GetAllAsync()).Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> GetByIdAsync(int id) =>
        (await GetEntityAsync(id)).ToDto();

    public async Task<CategoryDto> CreateAsync(CategoryUpsertDto dto)
    {
        var name = dto.Name.Trim();
        if (await categories.NameExistsAsync(name))
            throw new ConflictException($"Категория «{name}» уже существует.");

        var category = new Category { Name = name, Description = dto.Description?.Trim() };
        await categories.AddAsync(category);
        await unitOfWork.SaveChangesAsync();
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpsertDto dto)
    {
        var category = await GetEntityAsync(id);
        var name = dto.Name.Trim();

        if (await categories.NameExistsAsync(name, excludeId: id))
            throw new ConflictException($"Категория «{name}» уже существует.");

        category.Name = name;
        category.Description = dto.Description?.Trim();
        await unitOfWork.SaveChangesAsync();
        return category.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await GetEntityAsync(id);

        if (await categories.HasProductsAsync(id))
            throw new ConflictException("Нельзя удалить категорию, в которой есть товары. Сначала перенесите или удалите их.");

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<Category> GetEntityAsync(int id) =>
        await categories.GetByIdAsync(id) ?? throw new NotFoundException("Категория", id);
}
