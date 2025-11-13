namespace Game.Server.Services;

public class FileService
{
    private readonly IHostEnvironment environment;
    private readonly ILogger<FileService> logger;

    public FileService(IHostEnvironment environment, ILogger<FileService> logger)
    {
        this.environment = environment;
        this.logger = logger;
    }

    public async Task<(Guid Id, long FileSize)> StoreReplayData(string data)
    {
        var storageDirectory = GetStorageDirectory();
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        var id = Guid.NewGuid();
        var filePath = GetStoragePath(id);
        await File.WriteAllTextAsync(filePath, data, System.Text.Encoding.UTF8);

        var fileSize = new FileInfo(filePath).Length;
        logger.LogInformation(
            "Stored file with ID {FileId} and size {FileSizeInBytes}",
            id,
            fileSize
        );

        return (id, fileSize);
    }

    public void TryDeleteReplay(Guid id)
    {
        try
        {
            File.Delete(GetStoragePath(id));
            logger.LogInformation("Deleted file with ID {FileId}", id);
        }
        catch (FileNotFoundException)
        {
            return;
        }
    }

    public async Task<string> ReadReplay(Guid id)
    {
        return await File.ReadAllTextAsync(GetStoragePath(id));
    }

    private string GetStoragePath(Guid id) => Path.Join(GetStorageDirectory(), $"{id}.json");

    private string GetStorageDirectory() => Path.Join(environment.ContentRootPath, "replays/");
}
