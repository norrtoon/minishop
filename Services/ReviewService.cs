using MiniShop.DTOs;
using MiniShop.Exceptions;
using MiniShop.Models;
using MiniShop.Repositories;

namespace MiniShop.Services;

public interface IReviewService
{
    Task<ProductReviewsDto> GetForProductAsync(int productId, int? currentUserId);
    Task<List<ReviewDto>> GetLatestAsync(int count);
    Task<ReviewDto> CreateAsync(int productId, int userId, ReviewUpsertDto dto);
    Task<ReviewDto> UpdateAsync(int reviewId, int userId, ReviewUpsertDto dto);
    Task DeleteAsync(int reviewId, int userId, bool isAdmin);
}

public class ReviewService(
    IReviewRepository reviews,
    IProductRepository products,
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IReviewService
{
    public async Task<ProductReviewsDto> GetForProductAsync(int productId, int? currentUserId)
    {
        var product = await GetProductAsync(productId);
        var items = await reviews.GetForProductAsync(productId);

        var result = new ProductReviewsDto
        {
            ProductId = productId,
            Rating = product.Rating,
            ReviewCount = product.ReviewCount,
            Items = items.Select(r => r.ToDto()).ToList()
        };

        foreach (var r in items)
            result.Distribution[r.Rating - 1]++;

        if (currentUserId is null)
        {
            result.CannotReviewReason = "Войдите, чтобы оставить отзыв.";
            return result;
        }

        var mine = items.FirstOrDefault(r => r.UserId == currentUserId);
        if (mine is not null)
        {
            result.MyReviewId = mine.Id;
            return result;
        }

        result.CanReview = await orders.HasReceivedProductAsync(currentUserId.Value, productId);
        if (!result.CanReview)
            result.CannotReviewReason = "Отзыв можно оставить после получения заказа с этим товаром.";

        return result;
    }

    public async Task<List<ReviewDto>> GetLatestAsync(int count) =>
        (await reviews.GetLatestAsync(count)).Select(r => r.ToDto()).ToList();

    public async Task<ReviewDto> CreateAsync(int productId, int userId, ReviewUpsertDto dto)
    {
        await GetProductAsync(productId);

        if (await reviews.ExistsAsync(productId, userId))
            throw new ConflictException("Вы уже оставили отзыв на этот товар. Его можно отредактировать.");

        if (!await orders.HasReceivedProductAsync(userId, productId))
            throw new ForbiddenException("Отзыв можно оставить только на товар из полученного заказа.");

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = dto.Rating!.Value,
            Text = Normalize(dto.Text)
        };

        await reviews.AddAsync(review);
        await unitOfWork.SaveChangesAsync();
        await RecalculateRatingAsync(productId);

        return (await GetReviewAsync(review.Id)).ToDto();
    }

    public async Task<ReviewDto> UpdateAsync(int reviewId, int userId, ReviewUpsertDto dto)
    {
        var review = await GetReviewAsync(reviewId);

        if (review.UserId != userId)
            throw new ForbiddenException("Редактировать можно только свой отзыв.");

        review.Rating = dto.Rating!.Value;
        review.Text = Normalize(dto.Text);
        review.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        await RecalculateRatingAsync(review.ProductId);

        return review.ToDto();
    }

    public async Task DeleteAsync(int reviewId, int userId, bool isAdmin)
    {
        var review = await GetReviewAsync(reviewId);

        if (!isAdmin && review.UserId != userId)
            throw new ForbiddenException("Удалить можно только свой отзыв.");

        reviews.Remove(review);
        await unitOfWork.SaveChangesAsync();
        await RecalculateRatingAsync(review.ProductId);
    }

    private async Task RecalculateRatingAsync(int productId)
    {
        var product = await GetProductAsync(productId);
        (product.Rating, product.ReviewCount) = await reviews.GetStatsAsync(productId);
        await unitOfWork.SaveChangesAsync();
    }

    private static string? Normalize(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private async Task<Product> GetProductAsync(int id) =>
        await products.GetByIdAsync(id) ?? throw new NotFoundException("Товар", id);

    private async Task<Review> GetReviewAsync(int id) =>
        await reviews.GetByIdAsync(id) ?? throw new NotFoundException("Отзыв", id);
}
