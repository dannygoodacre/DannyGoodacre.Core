using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace TestFoo;

internal sealed class PasswordHasherService
{
    private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA512;

    private const int IterationCount = 600_000;

    private const int SaltSize = 128 / 8;

    private const int BytesRequested = 256 / 8;

    public string HashPassword(string value)
    {
        var salt = new byte[SaltSize];

        RandomNumberGenerator.Fill(salt);

        var subkey = KeyDerivation.Pbkdf2(value, salt, Prf, IterationCount, BytesRequested);

        var outputBytes = new byte[13 + salt.Length + subkey.Length];
        outputBytes[0] = 0x01;

        WriteNetworkByteOrder(outputBytes, 1, (uint)Prf);
        WriteNetworkByteOrder(outputBytes, 5, IterationCount);
        WriteNetworkByteOrder(outputBytes, 9, SaltSize);

        Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);

        Buffer.BlockCopy(subkey, 0, outputBytes, 13 + SaltSize, subkey.Length);

        return Convert.ToBase64String(outputBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        var decodedHash = Convert.FromBase64String(hashedPassword);

        if (decodedHash.Length < 13 || decodedHash[0] != 0x01)
        {
            return false;
        }

        var prf = (KeyDerivationPrf)ReadNetworkByteOrder(decodedHash, 1);
        var iterationCount = (int)ReadNetworkByteOrder(decodedHash, 5);
        var saltLength = (int)ReadNetworkByteOrder(decodedHash, 9);

        if (saltLength < 16)
        {
            return false;
        }

        var salt = decodedHash.AsSpan(13, saltLength).ToArray();

        var subkeyLength = decodedHash.Length - 13 - salt.Length;

        if (subkeyLength < 32)
        {
            return false;
        }

        var expectedSubkey = new byte[subkeyLength];

        Buffer.BlockCopy(decodedHash, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

        var actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterationCount, subkeyLength);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        => ((uint)(buffer[offset + 0]) << 24)
           | ((uint)(buffer[offset + 1]) << 16)
           | ((uint)(buffer[offset + 2]) << 8)
           | buffer[offset + 3];

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
    {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }
}
