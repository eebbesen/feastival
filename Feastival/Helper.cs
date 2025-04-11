using System.Reflection;
using System.Text.RegularExpressions;

namespace Feastival.Feastival
{
    public static partial class Helper
    {

        [GeneratedRegex(@"^[^-]*-(.*)")]
        private static partial Regex MyRegex();
        private static readonly Regex PATTERN = MyRegex();

        public static Dictionary<string, List<string>> Filter(Dictionary<string, List<string>> data,
            string filter)
        {
            return data.Where(kvp => PATTERN.Match(kvp.Key).Groups[1].ToString().StartsWith(filter))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static string GetVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute?.InformationalVersion ?? string.Empty;
        }
    }
}
