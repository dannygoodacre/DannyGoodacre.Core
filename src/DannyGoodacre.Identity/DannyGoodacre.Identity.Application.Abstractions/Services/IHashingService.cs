namespace DannyGoodacre.Identity.Application.Abstractions.Services;

public interface IHashingService
{
    string Hash(string value);

    bool Verify(string value, string hashedValue);
}
