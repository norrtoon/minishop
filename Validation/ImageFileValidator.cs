using MiniShop.Exceptions;

namespace MiniShop.Validation;

public static class ImageFileValidator
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, byte[][]> Signatures = new()
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    public static async Task<string> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Файл не выбран.");

        if (file.Length > MaxBytes)
            throw new BadRequestException($"Файл слишком большой: максимум {MaxBytes / 1024 / 1024} МБ.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Signatures.TryGetValue(extension, out var signatures))
            throw new BadRequestException("Допустимые форматы: JPG, PNG, GIF, WEBP.");

        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        }

        // проверяем сигнатуру, а не только расширение
        var matches = signatures.Any(sig => header.AsSpan().StartsWith(sig));
        if (extension == ".webp")
            matches &= header.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        if (!matches)
            throw new BadRequestException("Содержимое файла не похоже на изображение.");

        return extension == ".jpeg" ? ".jpg" : extension;
    }
}
