using System.ComponentModel.DataAnnotations;

namespace MiniShop.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class CategoryUpsertDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Описание не длиннее 500 символов")]
    public string? Description { get; set; }
}
