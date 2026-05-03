using System.Text.RegularExpressions;

namespace Feastival.FeastivalTest;

internal static partial class TestHelpers
{
    [GeneratedRegex(@"^(\d+\.\d+\.\d+)\+([a-f0-9]{40})$")]
    internal static partial Regex VersionRegex();
}
