using System.ComponentModel.DataAnnotations;

namespace MiniShop.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ReviewUpsertDto
{
    [Required(ErrorMessage = "Поставьте оценку")]
    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
    public int? Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Отзыв не длиннее 1000 символов")]
    public string? Text { get; set; }
}

public class ProductReviewsDto
{
    public int ProductId { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int[] Distribution { get; set; } = new int[5];
    public bool CanReview { get; set; }
    public string? CannotReviewReason { get; set; }
    public int? MyReviewId { get; set; }
    public List<ReviewDto> Items { get; set; } = new();
}
