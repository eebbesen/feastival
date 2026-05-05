using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using System.Web;

namespace Feastival.Feastival
{
    public class HttpTriggerFunc(ILogger<HttpTriggerFunc> logger)
    {
        private readonly ILogger<HttpTriggerFunc> _logger = logger;
        private static readonly string DATA_PATH = Path.Combine("data", "2026.json");
        private static char DetectSeparator(string date) => date.Contains('/') ? '/' : '-';
        private static string NormalizeDateSeparator(string date) => date.Replace('/', '-');
        private static Dictionary<string, List<string>> ApplySeparator(
            Dictionary<string, List<string>> data, char separator)
        {
            if (separator == '-') return data;
            return data.ToDictionary(kvp => kvp.Key.Replace('-', separator), kvp => kvp.Value);
        }
        private const string FilterToday = "TODAY";
        private const string FilterRange = "RANGE";
        private const string FilterYear = "YEAR";
        private const string FilterMonthDay = "MONTH-DAY";
        public static readonly string FILTER_MESSAGE =
            "Please provide a filter in the query string, e.g. ?filter=04-15 or ?filter=04/15"
            + " for April 15th or ?filter=02 for all days in February. "
            + "Partial months are also supported, e.g. ?filter=1 for October - December. "
            + "Partial days are also supported, e.g., ?filter=05-0 for May 1st - 9th.";
        public static readonly string START_DATE_MESSAGE =
            "Please provide a valid startDate in the query string MM-dd or MM/dd, e.g. ?startDate=04-15 or ?startDate=04/15";
        public static readonly string END_DATE_MESSAGE =
            "Please provide a valid endDate in the query string MM-dd or MM/dd, e.g. ?endDate=04-15 or ?endDate=04/15";

        // If running in development use the AzureWebJobsScriptRoot instead of basePath
        // basePath comes from the FunctionContext
        private static string GetJsonFilePath(string basePath)
        {
            var devBasePath = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            return Path.Combine(devBasePath ?? basePath, DATA_PATH);
        }

        private string GetData(string basePath)
        {
            var filePath = GetJsonFilePath(basePath);
            _logger.LogDebug("File path: {FilePath}", filePath);

            var jsonString = File.ReadAllText(filePath);
            _logger.LogDebug("JSON: {JsonString}", jsonString);

            return jsonString;
        }

        private IActionResult BuildResult(string basePath,
            string timeSpan, string startDate = "", string endDate = "", char separator = '-')
        {
            _logger
                .LogInformation("span: {TimeSpan} startDate: {StartDate} endDate: {EndDate} separator: {Separator}",
                HttpUtility.UrlEncode(timeSpan),
                HttpUtility.UrlEncode(startDate),
                HttpUtility.UrlEncode(endDate),
                HttpUtility.UrlEncode(separator.ToString()));
            Dictionary<string, List<string>> data;

            try
            {
                string jsonString = GetData(basePath);
                data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString)
                        ?? [];

                if (timeSpan == FilterRange)
                {
                    data = Helper.FilterRange(data, startDate, endDate);
                }
                else if (timeSpan != FilterYear)
                {
                    data = Helper.Filter(data, startDate);
                }
                else if (!string.IsNullOrEmpty(startDate))
                {
                    data = data.Where(kvp => kvp.Key.StartsWith(startDate))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new BadRequestObjectResult(ex.Message);
            }

            var result = new OkObjectResult(ApplySeparator(data, separator));
            result.ContentTypes.Add("application/json");

            return result;
        }

        [Function("today")]
        public IActionResult RunToday([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                FilterToday, DateTime.Now.ToString("MM-dd"));
        }

        [Function("range")]
        public IActionResult RunRange([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            string? startDate = req.Query["startDate"];
            string? endDate = req.Query["endDate"];

            if (string.IsNullOrEmpty(startDate))
            {
                return new BadRequestObjectResult(START_DATE_MESSAGE);
            }

            if (string.IsNullOrEmpty(endDate))
            {
                return new BadRequestObjectResult(END_DATE_MESSAGE);
            }

            var separator = DetectSeparator(startDate);
            var startDateParsed = DateTime.ParseExact(NormalizeDateSeparator(startDate), "MM-dd", CultureInfo.InvariantCulture);
            var endDateParsed = DateTime.ParseExact(NormalizeDateSeparator(endDate), "MM-dd", CultureInfo.InvariantCulture);

            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                FilterRange,
                startDateParsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDateParsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                separator);
        }

        [Function("year")]
        public IActionResult RunYear([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                FilterYear);
        }

        [Function("month-day")]
        public IActionResult RunMonthDay([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            var filter = req.Query["filter"].ToString();

            if (string.IsNullOrEmpty(filter))
            {
                return new BadRequestObjectResult(FILTER_MESSAGE);
            }

            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                FilterMonthDay, NormalizeDateSeparator(filter), separator: DetectSeparator(filter));
        }

        [Function("about")]
        public IActionResult RunAbout([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("ABOUT request");
            return new OkObjectResult(Helper.GetVersion());
        }
    }
}
