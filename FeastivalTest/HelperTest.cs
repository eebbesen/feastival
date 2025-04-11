using Feastival.Feastival;

using System.Text.Json;
using System.Text.RegularExpressions;

namespace FeastivalTest
{
    public partial class HelperTest
    {
        private static readonly string basePath = Path.Combine("..", "..", "..");
        private static readonly string json =
            File.ReadAllText(Path.Combine(basePath, "data", "2025.json"));
        private static readonly Dictionary<string, List<string>> data =
            JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);

        [GeneratedRegex(@"^(\d+\.\d+\.\d+)\+([a-f0-9]{40})$")]
        private static partial Regex MyRegex();

        [Fact]
        public void GetVersion()
        {
            string version = Helper.GetVersion();

            Assert.True(MyRegex().Match(version).Success, $"Version '{version}' could not be parsed into parts");
        }

        [Fact]
        public void Filter_ShouldReturnAllDataForMonth()
        {
            Assert.Equal(29, Helper.Filter(data, "02").Count);
        }

        [Fact]
        public void Filter_ShouldReturnEmptyForInvalidMonth()
        {
            Assert.Empty(Helper.Filter(data, "9999"));
        }

        [Fact]
        public void Filter_ShouldReturnAllDataForDay()
        {
            var result = Helper.Filter(data, "04-15");
            Assert.Single(result);
            Assert.Equal(["McDonald's Day", "National Glazed Spiral Ham Day"], result.Values.First());
        }

        [Fact]
        public void Filter_ShouldReturnEmptyForInvalidDay()
        {
            Assert.Empty(Helper.Filter(data, "05-9"));
        }

        [Fact]
        public void Filter_ShouldReturnValuesWhenOnlyFirstDigitOfDayProvided()
        {
            Assert.Equal(9, Helper.Filter(data, "05-0").Count);
        }
    }
}
