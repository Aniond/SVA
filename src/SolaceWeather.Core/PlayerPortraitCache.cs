using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace SolaceWeather.Core;

public sealed class PlayerPortraitRecord
{
    public int Version { get; set; } = 1;
    public string FarmId { get; set; } = "";
    public string FarmerId { get; set; } = "";
    public string Status { get; set; } = "Missing";
    public string? Sha256 { get; set; }
    public string? AppearanceHash { get; set; }
    public string Model { get; set; } = GeminiPlayerPortrait.Model;
    public string? FailureCategory { get; set; }
}

public sealed class PlayerPortraitCache
{
    private readonly string directory;
    public string ImagePath => Path.Combine(directory, "portrait.png");
    public PlayerPortraitCache(string root, string farmId, string farmerId)
    {
        if (!Regex.IsMatch(farmId, "^[0-9]+$") || !Regex.IsMatch(farmerId, "^-?[0-9]+$")) throw new ArgumentException("Invalid portrait identity.");
        directory = Path.Combine(root, farmId, farmerId);
    }
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public bool TryClaim(bool explicitRetry)
    {
        Directory.CreateDirectory(directory); string marker = Path.Combine(directory, "attempted");
        if (explicitRetry) { File.WriteAllText(marker, DateTime.UtcNow.ToString("O")); return true; }
        try { using var file = new FileStream(marker, FileMode.CreateNew); return true; }
        catch (IOException) when (File.Exists(marker)) { return false; }
    }
    public PlayerPortraitRecord? ReadRecord() => File.Exists(Path.Combine(directory, "record.json"))
        ? JsonSerializer.Deserialize<PlayerPortraitRecord>(File.ReadAllText(Path.Combine(directory, "record.json"))) : null;
    public void WriteRecord(PlayerPortraitRecord record) => AtomicWrite(Path.Combine(directory, "record.json"), JsonSerializer.SerializeToUtf8Bytes(record));
    public string WriteImage(byte[] bytes) { AtomicWrite(ImagePath, bytes); string hash = Hash(bytes); _ = ReadImage(hash); return hash; }
    /// <summary>Keep the last usable image and metadata if decoding or save-metadata publication fails.</summary>
    public void CommitImage(byte[] bytes, PlayerPortraitRecord record, Action<PlayerPortraitRecord> publish)
    {
        string metadataPath = Path.Combine(directory, "record.json");
        byte[]? previousImage = File.Exists(ImagePath) ? File.ReadAllBytes(ImagePath) : null;
        byte[]? previousMetadata = File.Exists(metadataPath) ? File.ReadAllBytes(metadataPath) : null;
        string? previousHash = record.Sha256; string previousStatus = record.Status;
        try
        {
            record.Sha256 = WriteImage(bytes);
            publish(record);
        }
        catch
        {
            record.Sha256 = previousHash; record.Status = previousStatus;
            if (previousImage != null) AtomicWrite(ImagePath, previousImage); else if (File.Exists(ImagePath)) File.Delete(ImagePath);
            if (previousMetadata != null) AtomicWrite(metadataPath, previousMetadata); else if (File.Exists(metadataPath)) File.Delete(metadataPath);
            throw;
        }
    }
    public byte[] ReadImage(string expectedHash)
    {
        if (new FileInfo(ImagePath).Length > 8 * 1024 * 1024) throw new InvalidDataException("Portrait cache too large.");
        byte[] bytes = File.ReadAllBytes(ImagePath);
        if (Hash(bytes) != expectedHash) throw new InvalidDataException("Portrait cache hash mismatch.");
        return bytes;
    }
    private void AtomicWrite(string file, byte[] bytes)
    {
        Directory.CreateDirectory(directory); string temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, file, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
