using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("run-section-e")]
        public async Task<IActionResult> RunSectionETests()
        {
            try
            {
                var testResults = await RunDotnetTests("UserManagementAPI.Tests.AuthorizationTests");
                return Ok(new { success = true, results = testResults });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("run-all")]
        public async Task<IActionResult> RunAllTests()
        {
            try
            {
                var testResults = await RunDotnetTests(null);
                return Ok(new { success = true, results = testResults });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        private async Task<List<Dictionary<string, object>>> RunDotnetTests(string? classFilter)
        {
            var testProjectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "UserManagementAPI.Tests");
            var testProjectPath_resolved = Path.GetFullPath(testProjectPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = classFilter != null
                    ? $"test --filter {classFilter} --logger json"
                    : "test --logger json",
                WorkingDirectory = testProjectPath_resolved,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var testResults = new List<Dictionary<string, object>>();

            using (var process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(output))
                    {
                        try
                        {
                            var jsonOutput = JsonDocument.Parse(output);
                            var root = jsonOutput.RootElement;

                            if (root.TryGetProperty("tests", out var testsElement))
                            {
                                foreach (var test in testsElement.EnumerateArray())
                                {
                                    var result = new Dictionary<string, object>();

                                    if (test.TryGetProperty("name", out var nameElement))
                                        result["name"] = nameElement.GetString() ?? "Unknown";

                                    if (test.TryGetProperty("outcome", out var outcomeElement))
                                        result["status"] = outcomeElement.GetString() ?? "unknown";
                                    else
                                        result["status"] = "unknown";

                                    if (test.TryGetProperty("errorMessage", out var errorElement))
                                        result["errorMessage"] = errorElement.GetString() ?? "";

                                    testResults.Add(result);
                                }
                            }
                        }
                        catch
                        {
                            // If JSON parsing fails, parse raw output
                            testResults = ParseRawTestOutput(output + "\n" + error);
                        }
                    }

                    if (testResults.Count == 0)
                    {
                        testResults = ParseRawTestOutput(output + "\n" + error);
                    }
                }
            }

            return testResults;
        }

        private List<Dictionary<string, object>> ParseRawTestOutput(string output)
        {
            var results = new List<Dictionary<string, object>>();
            var lines = output.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains("PASSED") || line.Contains("✓") || line.Contains("Passed"))
                {
                    var testName = ExtractTestName(line);
                    if (!string.IsNullOrEmpty(testName))
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            { "name", testName },
                            { "status", "Passed" }
                        });
                    }
                }
                else if (line.Contains("FAILED") || line.Contains("✗") || line.Contains("Failed"))
                {
                    var testName = ExtractTestName(line);
                    if (!string.IsNullOrEmpty(testName))
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            { "name", testName },
                            { "status", "Failed" },
                            { "errorMessage", line }
                        });
                    }
                }
            }

            return results;
        }

        private string ExtractTestName(string line)
        {
            var parts = line.Split(new[] { "::", "-", "✓", "✗" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                return parts[parts.Length - 1].Trim();
            }
            return null;
        }
    }
}
