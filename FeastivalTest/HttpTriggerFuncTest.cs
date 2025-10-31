using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Moq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Feastival.Feastival;

namespace Feastival.FeastivalTest;

public partial class HttpTriggerFuncTest
{
    private readonly HttpTriggerFunc _httpTriggerFunc;
    private readonly Mock<ILogger<HttpTriggerFunc>> _loggerMock;
    private readonly MethodInfo _getJsonFilePathMethod;
    private readonly MethodInfo _getDataMethod;
    private readonly MethodInfo _buildResultMethod;
    private readonly Mock<FunctionContext> mockContext;
    private static readonly string basePath = Path.Combine("..", "..", "..");
    private static readonly string expectedJson = File.ReadAllText(Path.Combine(basePath, "data", "2025.json"));
    [GeneratedRegex(@"^(\d+\.\d+\.\d+)\+([a-f0-9]{40})$")]
    private static partial Regex MyRegex();

    public HttpTriggerFuncTest()
    {
        _loggerMock = new Mock<ILogger<HttpTriggerFunc>>();
        _httpTriggerFunc = new HttpTriggerFunc(_loggerMock.Object);
        _getJsonFilePathMethod = typeof(HttpTriggerFunc).GetMethod("GetJsonFilePath",
                BindingFlags.NonPublic | BindingFlags.Static);
        _getDataMethod = typeof(HttpTriggerFunc).GetMethod("GetData",
                BindingFlags.NonPublic | BindingFlags.Instance);
        _buildResultMethod = typeof(HttpTriggerFunc).GetMethod("BuildResult",
                BindingFlags.NonPublic | BindingFlags.Instance);

        var mockFunctionDefinition = new Mock<FunctionDefinition>();
        mockFunctionDefinition.Setup(fd => fd.PathToAssembly).Returns(basePath);
        mockContext = new Mock<FunctionContext>();
        mockContext.Setup(fc => fc.FunctionDefinition).Returns(mockFunctionDefinition.Object);
    }

    [Fact]
    public void GetJsonFilePath_ShouldReturnCorrectPathWhenNotDevelopment()
    {
        string expectedPath = Path.Combine(basePath, "data", "2025.json");

        string result = (string)_getJsonFilePathMethod.Invoke(null,
            [basePath]);

        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void GetJsonFilePath_ShouldReturnCorrectPathWhenDevelopment()
    {
        string devPath = Path.Combine("dev", basePath);
        string expectedPath = Path.Combine(devPath, "data", "2025.json");
        Environment.SetEnvironmentVariable("AzureWebJobsScriptRoot", devPath);

        string result = (string)_getJsonFilePathMethod.Invoke(null,
            [basePath]);

        Assert.Equal(expectedPath, result);
    }

    [Fact]
    public void GetDate_ShouldReturnJsonString()
    {
        string result = (string)_getDataMethod.Invoke(_httpTriggerFunc,
            [Path.Combine("..", "..", "..")]);

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public void BuildResult_Year_ShouldReturnOkObjectResult()
    {
        var result = (IActionResult)_buildResultMethod.Invoke(_httpTriggerFunc,
            [basePath, "", "", ""]);
        System.Diagnostics.Debug.WriteLine($"AResult: {((OkObjectResult)result).Value}");

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        var dict = ((OkObjectResult)result).Value as Dictionary<string, List<string>>;
        Assert.Equal(JsonSerializer.Deserialize<Dictionary<string, List<string>>>(expectedJson).Count,
           dict.Count);
    }

    [Fact]
    public void BuildResult_Year_ShouldReturnBadRequestObjectResult()
    {
        var result = (IActionResult)_buildResultMethod.Invoke(_httpTriggerFunc,
            ["badpath", "", "", ""]);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Could not find a part of the path",
            ((BadRequestObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunToday_ShouldReturnFullYear()
    {
        var mockReq = new Mock<HttpRequest>();

        var result = _httpTriggerFunc.RunToday(mockReq.Object, mockContext.Object);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(1, ((OkObjectResult)result).Value is Dictionary<string, List<string>> data ? data.Count : 0);
    }

    [Fact]
    public void RunYear_ShouldReturnFullYear()
    {
        var mockReq = new Mock<HttpRequest>();

        var result = _httpTriggerFunc.RunYear(mockReq.Object, mockContext.Object);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(JsonSerializer.Deserialize<Dictionary<string, List<string>>>(expectedJson),
            ((OkObjectResult)result).Value);
    }

    [Fact]
    public void RunMonthDay_ShouldReturnMessageWhenNoFilter()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["filter"]).Returns("");

        var result = _httpTriggerFunc.RunMonthDay(mockReq.Object, mockContext.Object);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, ((BadRequestObjectResult)result).StatusCode);
        Assert.Equal(HttpTriggerFunc.FILTER_MESSAGE,
            ((BadRequestObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunMonthDay_ShouldReturnEmptyWhenInvalidMonth()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["filter"]).Returns("22");

        var result = _httpTriggerFunc.RunMonthDay(mockReq.Object, mockContext.Object);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(0,
            ((OkObjectResult)result).Value is Dictionary<string, List<string>> data ? data.Count : 0);
    }

    [Fact]
    public void RunMonthDay_ShouldReturnOnlyForDayRequested()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["filter"]).Returns("02-15");

        var result = (OkObjectResult)_httpTriggerFunc.RunMonthDay(mockReq.Object, mockContext.Object);
        var values = result.Value as Dictionary<string, List<string>>;

        Assert.Equal("application/json", result.ContentTypes[0]);
        Assert.Equal(1, values?.Count);
        Assert.Equal("National Gumdrop Day", values.First().Value[0]);
    }

    [Fact]
    public void RunMonthDay_ShouldReturnEmptyWhenInvalidDay()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["filter"]).Returns("03-32");

        var result = _httpTriggerFunc.RunMonthDay(mockReq.Object, mockContext.Object);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(0,
            ((OkObjectResult)result).Value is Dictionary<string, List<string>> data ? data.Count : 0);
    }

    [Fact]
    public void RunAbout_ShouldReturnSha()
    {
        var mockReq = new Mock<HttpRequest>();

        var result = _httpTriggerFunc.RunAbout(mockReq.Object);

        Assert.IsType<OkObjectResult>(result);
        Assert.Matches(MyRegex(), ((OkObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunMonthDay_ShouldReturnJustDataForRequestedMonth()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["filter"]).Returns(("02"));

        var result = _httpTriggerFunc.RunMonthDay(mockReq.Object, mockContext.Object);

        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(29,
            ((OkObjectResult)result).Value is Dictionary<string, List<string>> data ? data.Count : 0);
    }

    [Fact]
    public void RunRange_ShouldReturnDataOverMonthEndIgnoreInvalidLeapDay()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["startDate"]).Returns(("02-28"));
        mockReq.Setup(req => req.Query["endDate"]).Returns(("03-03"));

        var result = _httpTriggerFunc.RunRange(mockReq.Object, mockContext.Object);

        Assert.Equal("application/json", ((OkObjectResult)result).ContentTypes[0]);
        Assert.Equal(4,
            ((OkObjectResult)result).Value is Dictionary<string, List<string>> data ? data.Count : 0);
    }

    [Fact]
    public void RunRange_ShouldReturnErrorWithoutStartDate()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["endDate"]).Returns(("03-03"));

        var result = _httpTriggerFunc.RunRange(mockReq.Object, mockContext.Object);

        Assert.Equal(HttpTriggerFunc.START_DATE_MESSAGE,
            ((BadRequestObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunRange_ShouldReturnErrorWithoutStartDateOrEndDate()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["slug"]).Returns(("03-03"));

        var result = _httpTriggerFunc.RunRange(mockReq.Object, mockContext.Object);

        Assert.Equal(HttpTriggerFunc.START_DATE_MESSAGE,
            ((BadRequestObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunRange_ShouldReturnErrorWithoutEndDate()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["startDate"]).Returns(("02-28"));

        var result = _httpTriggerFunc.RunRange(mockReq.Object, mockContext.Object);

        Assert.Equal(HttpTriggerFunc.END_DATE_MESSAGE,
            ((BadRequestObjectResult)result).Value.ToString());
    }

    [Fact]
    public void RunRange_ShouldReturnErrorWithInvalidStartDate()
    {
        var mockReq = new Mock<HttpRequest>();
        mockReq.Setup(req => req.Query["startDate"]).Returns(("02-31"));

        var result = _httpTriggerFunc.RunRange(mockReq.Object, mockContext.Object);

        Assert.Equal(HttpTriggerFunc.END_DATE_MESSAGE,
            ((BadRequestObjectResult)result).Value.ToString());
    }
}
