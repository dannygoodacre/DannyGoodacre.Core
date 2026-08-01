namespace DannyGoodacre.Identity.Application.Abstractions.Services;

public interface IPasswordHashingService
{
    string Hash(string value);

    bool Verify(string value, string hashedValue);
}
