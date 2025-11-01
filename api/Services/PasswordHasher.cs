using System;
using System.Linq;
using System.Security.Cryptography;

namespace LexiFlow.Api.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        var payload = new byte[SaltSize + KeySize];
        Buffer.BlockCopy(salt, 0, payload, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, payload, SaltSize, KeySize);
        return Convert.ToBase64String(payload);
    }

    public static bool Verify(string password, string encoded)
    {
        try
        {
            var payload = Convert.FromBase64String(encoded);
            var salt = payload.Take(SaltSize).ToArray();
            var storedHash = payload.Skip(SaltSize).Take(KeySize).ToArray();
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }
}
