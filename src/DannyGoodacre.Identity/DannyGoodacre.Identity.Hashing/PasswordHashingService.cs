// Substantial portions of this file were derived and adapted from Microsoft's ASP.NET Core Identity:
// File: src/Identity/Extensions.Core/src/PasswordHasher.cs
// Source: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/PasswordHasher.cs
//
// Copyright (c) .NET Foundation and Contributors
// Licensed under the MIT License (https://opensource.org/licenses/MIT)

using System.Security.Cryptography;
using DannyGoodacre.Identity.Application.Abstractions.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DannyGoodacre.Identity.Hashing;

internal sealed class PasswordHashingService : IPasswordHashingService
{
    private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA512;

    private const int IterationCount = 600_000;

    private const int SaltSize = 128 / 8;

    private const int BytesRequested = 256 / 8;

    public string Hash(string password)
    {
        var salt = new byte[SaltSize];

        RandomNumberGenerator.Fill(salt);

        byte[] subkey = KeyDerivation.Pbkdf2(password, salt, Prf, IterationCount, BytesRequested);

        byte[] outputBytes = new byte[13 + salt.Length + subkey.Length];

        outputBytes[0] = 0x01;

        WriteNetworkByteOrder(outputBytes, 1, (uint)Prf);
        WriteNetworkByteOrder(outputBytes, 5, IterationCount);
        WriteNetworkByteOrder(outputBytes, 9, SaltSize);

        Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);

        Buffer.BlockCopy(subkey, 0, outputBytes, 13 + SaltSize, subkey.Length);

        return Convert.ToBase64String(outputBytes);
    }

    public bool Verify(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        byte[] decodedHash = Convert.FromBase64String(hashedPassword);

        if (decodedHash.Length < 13 || decodedHash[0] != 0x01)
        {
            return false;
        }

        KeyDerivationPrf prf = (KeyDerivationPrf)ReadNetworkByteOrder(decodedHash, 1);

        int iterationCount = (int)ReadNetworkByteOrder(decodedHash, 5);

        int saltLength = (int)ReadNetworkByteOrder(decodedHash, 9);

        if (saltLength < 16)
        {
            return false;
        }

        byte[] salt = decodedHash.AsSpan(13, saltLength).ToArray();

        int subkeyLength = decodedHash.Length - 13 - salt.Length;

        if (subkeyLength < 32)
        {
            return false;
        }

        var expectedSubkey = new byte[subkeyLength];

        Buffer.BlockCopy(decodedHash, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

        byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, prf, iterationCount, subkeyLength);

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
