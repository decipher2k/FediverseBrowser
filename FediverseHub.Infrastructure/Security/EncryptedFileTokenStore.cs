using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FediverseHub.Core.Domain;
using FediverseHub.Core.Interfaces;

namespace FediverseHub.Infrastructure.Security;

public sealed class EncryptedFileTokenStore : ISecureTokenStore
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly string _directory;
    private readonly string _keyPath;

    public EncryptedFileTokenStore(string? dataDirectory = null)
    {
        _directory = Path.Combine(
            dataDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FediverseHub"),
            "tokens");
        Directory.CreateDirectory(_directory);
        _keyPath = Path.Combine(_directory, "local.key");
    }

    public async Task SaveTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var key = await GetOrCreateKeyAsync(cancellationToken).ConfigureAwait(false);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintext = Encoding.UTF8.GetBytes(accessToken);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new TokenPayload(
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));

        var fileName = GetTokenPath(sourceType, accountId);
        await File.WriteAllTextAsync(
            fileName,
            JsonSerializer.Serialize(payload),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        CancellationToken cancellationToken)
    {
        var fileName = GetTokenPath(sourceType, accountId);
        if (!File.Exists(fileName))
        {
            return null;
        }

        var payload = JsonSerializer.Deserialize<TokenPayload>(
            await File.ReadAllTextAsync(fileName, cancellationToken).ConfigureAwait(false));
        if (payload is null)
        {
            return null;
        }

        var key = await GetOrCreateKeyAsync(cancellationToken).ConfigureAwait(false);
        var nonce = Convert.FromBase64String(payload.Nonce);
        var tag = Convert.FromBase64String(payload.Tag);
        var ciphertext = Convert.FromBase64String(payload.Ciphertext);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    public Task DeleteTokenAsync(
        FediverseSourceType sourceType,
        string accountId,
        CancellationToken cancellationToken)
    {
        var fileName = GetTokenPath(sourceType, accountId);
        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(_directory, "*.token"))
        {
            File.Delete(file);
        }

        return Task.CompletedTask;
    }

    private async Task<byte[]> GetOrCreateKeyAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_keyPath))
        {
            return Convert.FromBase64String(
                await File.ReadAllTextAsync(_keyPath, cancellationToken).ConfigureAwait(false));
        }

        var key = RandomNumberGenerator.GetBytes(32);
        await File.WriteAllTextAsync(_keyPath, Convert.ToBase64String(key), cancellationToken)
            .ConfigureAwait(false);
        return key;
    }

    private string GetTokenPath(FediverseSourceType sourceType, string accountId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{sourceType}:{accountId}"));
        return Path.Combine(_directory, $"{Convert.ToHexString(hash)}.token");
    }

    private sealed record TokenPayload(string Nonce, string Tag, string Ciphertext);
}
