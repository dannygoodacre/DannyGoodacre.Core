using System.Runtime.InteropServices;
using System.Text;

namespace DannyGoodacre.Primitives;

public class ValidationState
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public IReadOnlyDictionary<string, List<string>> Errors => _errors;

    public void AddError(string property, string error)
    {
        ref List<string>? list = ref CollectionsMarshal.GetValueRefOrAddDefault(_errors, property, out _);

        list ??= [];

        list.Add(error);
    }

    public bool HasErrors => _errors.Count > 0;

    public override string ToString()
    {
        if (!HasErrors)
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        foreach (var (property, errors) in Errors)
        {
            stringBuilder.AppendLine($"{property}:");

            foreach (string error in errors)
            {
                stringBuilder.AppendLine($"  - {error}");
            }
        }

        return stringBuilder.ToString().TrimEnd();
    }
}
