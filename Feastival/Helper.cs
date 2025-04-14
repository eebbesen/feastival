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

        public static Dictionary<string, List<string>> FilterRange(Dictionary<string, List<string>> data,
            string startDate, string endDate)
        {
            Dictionary<string, List<string>> result = [];
            DateTime sd = DateTime.ParseExact(startDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            DateTime ed = DateTime.ParseExact(endDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            for (DateTime date = sd; date <= ed; date = date.AddDays(1))
            {
                var toAdd = Filter(data, date.ToString("MM-dd"));
                result.Add(date.ToString("yyyy-MM-dd"), [.. toAdd.Values.SelectMany(v => v)]);
            }

            return result;
        }

        public static string GetVersion()
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute?.InformationalVersion ?? string.Empty;
        }
    }
}
