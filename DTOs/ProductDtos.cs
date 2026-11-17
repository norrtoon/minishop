using System.ComponentModel.DataAnnotations;
using MiniShop.Validation;

namespace MiniShop.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
}

public class ProductUpsertDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 200 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Описание не длиннее 2000 символов")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Укажите цену")]
    [Range(0.01, 10_000_000, ErrorMessage = "Цена должна быть от 0.01 до 10 000 000")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Укажите остаток")]
    [Range(0, 100_000, ErrorMessage = "Остаток должен быть от 0 до 100 000")]
    public int? Stock { get; set; }

    [ImageUrl]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Выберите категорию")]
    [Range(1, int.MaxValue, ErrorMessage = "Выберите категорию")]
    public int? CategoryId { get; set; }
}

public class ProductQueryDto : IValidatableObject
{
    [MaxLength(100)]
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Минимальная цена не может быть отрицательной")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Максимальная цена не может быть отрицательной")]
    public decimal? MaxPrice { get; set; }

    public bool InStockOnly { get; set; }

    [RegularExpression("^(name|price_asc|price_desc|newest|rating)$",
        ErrorMessage = "Допустимая сортировка: name, price_asc, price_desc, newest, rating")]
    public string? Sort { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы начинается с 1")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Размер страницы от 1 до 50")]
    public int PageSize { get; set; } = 12;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
        {
            yield return new ValidationResult(
                "Минимальная цена не может быть больше максимальной",
                new[] { nameof(MinPrice), nameof(MaxPrice) });
        }
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
