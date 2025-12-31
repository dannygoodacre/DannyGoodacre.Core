using System.Text;

namespace DannyGoodacre.Core;

public class ValidationState
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public IReadOnlyDictionary<string, IEnumerable<string>> Errors
        => _errors.ToDictionary(x => x.Key, IEnumerable<string> (x) => x.Value);

    public void AddError(string property, string error)
    {
        if (_errors.TryGetValue(property, out var err))
        {
            err.Add(error);

            return;
        }

        _errors.Add(property, [error]);
    }

    public bool HasErrors => _errors.Count != 0;

    public override string ToString()
    {
        if (!HasErrors)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        foreach (var kvp in Errors)
        {
            stringBuilder.AppendLine($"{kvp.Key}:");

            foreach (var error in kvp.Value)
            {
                stringBuilder.AppendLine($"  - {error}");
            }
        }

        return stringBuilder
            .Remove(stringBuilder.Length - Environment.NewLine.Length, Environment.NewLine.Length)
            .ToString();
    }
}
