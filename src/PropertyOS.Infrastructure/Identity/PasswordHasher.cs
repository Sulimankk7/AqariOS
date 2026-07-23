using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using PropertyOS.Application.Identity;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Password hashing implementation using the Argon2id algorithm.
/// Strictly enforces Argon2id for password storage and verification.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128 bits
    private const int HashSize = 32;       // 256 bits
    private const int DefaultIterations = 4;
    private const int DefaultMemorySize = 65536; // 64 MB in KB
    private const int DefaultParallelism = 2;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = ComputeArgon2idHash(password, salt, DefaultIterations, DefaultMemorySize, DefaultParallelism);

        string saltBase64 = Convert.ToBase64String(salt);
        string hashBase64 = Convert.ToBase64String(hash);

        return $"argon2id.{DefaultIterations}.{DefaultMemorySize}.{DefaultParallelism}.{saltBase64}.{hashBase64}";
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var parts = passwordHash.Split('.');

        // Format: argon2id.{iterations}.{memory}.{parallelism}.{salt}.{hash}
        // OR format: argon2id.{salt}.{hash}
        if (parts.Length < 3 || !string.Equals(parts[0], "argon2id", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            int iterations = DefaultIterations;
            int memorySize = DefaultMemorySize;
            int parallelism = DefaultParallelism;
            byte[] salt;
            byte[] expectedHash;

            if (parts.Length == 6)
            {
                if (!int.TryParse(parts[1], out iterations) ||
                    !int.TryParse(parts[2], out memorySize) ||
                    !int.TryParse(parts[3], out parallelism))
                {
                    return false;
                }

                salt = Convert.FromBase64String(parts[4]);
                expectedHash = Convert.FromBase64String(parts[5]);
            }
            else if (parts.Length == 3)
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedHash = Convert.FromBase64String(parts[2]);
            }
            else
            {
                return false;
            }

            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            byte[] actualHash = ComputeArgon2idHash(password, salt, iterations, memorySize, parallelism);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeArgon2idHash(
        string password,
        byte[] salt,
        int iterations,
        int memorySize,
        int parallelism)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = memorySize,
            Iterations = iterations
        };

        return argon2.GetBytes(HashSize);
    }
}
