using MiniShop.DTOs;
using MiniShop.Exceptions;
using MiniShop.Models;
using MiniShop.Repositories;
using MiniShop.Storage;
using MiniShop.Validation;

namespace MiniShop.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryDto query);
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(ProductUpsertDto dto);
    Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto);
    Task DeleteAsync(int id);
    Task<ProductDto> SetImageAsync(int id, IFormFile? file);
    Task<ProductDto> RemoveImageAsync(int id);
}

public class ProductService(
    IProductRepository products,
    ICategoryRepository categories,
    IFileStorage fileStorage,
    IUnitOfWork unitOfWork) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryDto query)
    {
        var page = await products.GetPagedAsync(query);
        return new PagedResult<ProductDto>
        {
            Items = page.Items.Select(p => p.ToDto()).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        };
    }

    public async Task<ProductDto> GetByIdAsync(int id) =>
        (await GetEntityAsync(id)).ToDto();

    public async Task<ProductDto> CreateAsync(ProductUpsertDto dto)
    {
        await EnsureCategoryExists(dto.CategoryId!.Value);

        var product = new Product();
        Apply(product, dto);

        await products.AddAsync(product);
        await unitOfWork.SaveChangesAsync();

        return (await GetEntityAsync(product.Id)).ToDto();
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductUpsertDto dto)
    {
        var product = await GetEntityAsync(id);
        await EnsureCategoryExists(dto.CategoryId!.Value);

        var oldImage = product.ImageUrl;
        Apply(product, dto);
        await unitOfWork.SaveChangesAsync();

        if (oldImage != product.ImageUrl)
            fileStorage.Delete(oldImage);

        return (await GetEntityAsync(id)).ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var product = await GetEntityAsync(id);
        products.Remove(product);
        await unitOfWork.SaveChangesAsync();
        fileStorage.Delete(product.ImageUrl);
    }

    public async Task<ProductDto> SetImageAsync(int id, IFormFile? file)
    {
        var product = await GetEntityAsync(id);
        var extension = await ImageFileValidator.ValidateAsync(file);

        string url;
        await using (var stream = file!.OpenReadStream())
        {
            url = await fileStorage.SaveAsync(stream, extension);
        }

        var oldImage = product.ImageUrl;
        product.ImageUrl = url;
        await unitOfWork.SaveChangesAsync();
        fileStorage.Delete(oldImage);

        return product.ToDto();
    }

    public async Task<ProductDto> RemoveImageAsync(int id)
    {
        var product = await GetEntityAsync(id);
        var oldImage = product.ImageUrl;

        product.ImageUrl = null;
        await unitOfWork.SaveChangesAsync();
        fileStorage.Delete(oldImage);

        return product.ToDto();
    }

    private static void Apply(Product product, ProductUpsertDto dto)
    {
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.Price = dto.Price!.Value;
        product.Stock = dto.Stock!.Value;
        product.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        product.CategoryId = dto.CategoryId!.Value;
    }

    private async Task EnsureCategoryExists(int categoryId)
    {
        if (!await categories.ExistsAsync(categoryId))
            throw new BadRequestException($"Категория с id={categoryId} не существует.");
    }

    private async Task<Product> GetEntityAsync(int id) =>
        await products.GetByIdAsync(id) ?? throw new NotFoundException("Товар", id);
}
