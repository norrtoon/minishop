namespace MiniShop.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension);
    void Delete(string? url);
    bool IsStoredHere(string? url);
}

public class LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private const string Folder = "uploads";

    private string RootPath => Path.Combine(env.WebRootPath, Folder);

    public async Task<string> SaveAsync(Stream content, string extension)
    {
        Directory.CreateDirectory(RootPath);
        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using var file = File.Create(Path.Combine(RootPath, fileName));
        await content.CopyToAsync(file);

        return $"/{Folder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (!IsStoredHere(url))
            return;

        var path = Path.Combine(RootPath, Path.GetFileName(url!));
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Не удалось удалить файл {Path}", path);
        }
    }

    public bool IsStoredHere(string? url) =>
        url is not null && url.StartsWith($"/{Folder}/", StringComparison.Ordinal);
}
