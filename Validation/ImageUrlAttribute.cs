using System.ComponentModel.DataAnnotations;

namespace MiniShop.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class ImageUrlAttribute : ValidationAttribute
{
    public ImageUrlAttribute()
        : base("Укажите ссылку вида https://... или загрузите файл") { }

    public override bool IsValid(object? value)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return true;

        if (url.StartsWith("/uploads/", StringComparison.Ordinal) && !url.Contains(".."))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
