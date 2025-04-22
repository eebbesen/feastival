using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Web;

namespace Feastival.Feastival
{
    public class HttpTriggerFunc(ILogger<HttpTriggerFunc> logger)
    {
        private readonly ILogger<HttpTriggerFunc> _logger = logger;
        private static readonly string DATA_PATH = Path.Combine("data", "2025.json");
        public static readonly string FILTER_MESSAGE =
            "Please provide a filter in the query string, e.g. ?filter=04-15"
            + " for April 15th or ?filter=02 for February. "
            + "Partial months are also supported, e.g. ?filter=1 for February 10th - 19th. "
            + "Partial days are also supported, e.g., ?filter=05-0 for May 1st - 9th.";
        public static readonly string START_DATE_MESSAGE = "Please provide a valid startDate in the query string MM-dd, e.g. ?startDate=04-15";
        public static readonly string END_DATE_MESSAGE = "Please provide a valid endDate in the query string MM-dd, e.g. ?endDate=04-15";

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
            string timeSpan, string startDate = "", string endDate = "")
        {
            _logger
                .LogInformation("{TimeSpan} startDate: {StartDate} endDate: {EndDate}",
                HttpUtility.UrlEncode(timeSpan),
                HttpUtility.UrlEncode(startDate),
                HttpUtility.UrlEncode(endDate));
            Dictionary<string, List<string>> data;

            try
            {
                string jsonString = GetData(basePath);
                data = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString)
                        ?? [];

                if (timeSpan == "RANGE")
                {
                    data = Helper.FilterRange(data, startDate, endDate);
                }
                else if (timeSpan != "YEAR")
                {
                    data = Helper.Filter(data, startDate);
                }
                else
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

            var result = new OkObjectResult(data);
            result.ContentTypes.Add("application/json");

            return result;
        }

        [Function("today")]
        public IActionResult RunToday([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                "TODAY", DateTime.Now.ToString("MM-dd"));
        }

        [Function("range")]
        public IActionResult RunRange([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            string startDate = req.Query["startDate"];
            string endDate = req.Query["endDate"];

            if (string.IsNullOrEmpty(startDate))
            {
                return new BadRequestObjectResult(START_DATE_MESSAGE);
            }

            if (string.IsNullOrEmpty(endDate))
            {
                return new BadRequestObjectResult(END_DATE_MESSAGE);
            }

            var startDateParsed = DateTime.ParseExact(startDate, "MM-dd", null);
            var endDateParsed = DateTime.ParseExact(endDate, "MM-dd", null);

            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                "RANGE", startDateParsed.ToString("yyyy-MM-dd"), endDateParsed.ToString("yyyy-MM-dd"));
        }

        [Function("year")]
        public IActionResult RunYear([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                "YEAR");
        }

        [Function("month-day")]
        public IActionResult RunMonthDay([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
    FunctionContext executionContext)
        {
            if (string.IsNullOrEmpty(req.Query["filter"]))
            {
                return new BadRequestObjectResult(FILTER_MESSAGE);
            }

            return BuildResult(executionContext.FunctionDefinition.PathToAssembly,
                "MONTH-DAY", req.Query["filter"].ToString() ?? string.Empty);
        }

        [Function("about")]
        public IActionResult RunAbout([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("ABOUT request");
            return new OkObjectResult(Helper.GetVersion());
        }
    }
}
