using System.ComponentModel;
using DannyGoodacre.Identity.Core;

namespace DannyGoodacre.Identity.Application.Abstractions.Data.Repositories;

public interface IRoleRepository
{
    void Add(string name);

    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
