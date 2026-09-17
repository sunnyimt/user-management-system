# Test Report Generator
# Runs all tests and generates an HTML report

param(
    [string]$OutputPath = "TestReport.html",
    [string]$TestFilter = ""
)

Write-Host "🧪 Test Report Generator" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Get current directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectDir = $scriptDir

Write-Host "Working directory: $testProjectDir" -ForegroundColor Gray
Write-Host ""

# Run tests with JSON logger
Write-Host "Running tests..." -ForegroundColor Yellow

$testArgs = "test --logger json"
if ($TestFilter) {
    $testArgs += " --filter $TestFilter"
}

$process = Start-Process -FilePath "dotnet" -ArgumentList $testArgs -WorkingDirectory $testProjectDir -NoNewWindow -PassThru -RedirectStandardOutput "test-output.json" -RedirectStandardError "test-error.txt"
$exitCode = $process.ExitCode

if (Test-Path "test-error.txt") {
    $errorContent = Get-Content "test-error.txt" -Raw
    if ($errorContent -and $errorContent.Trim()) {
        Write-Host "Errors occurred:" -ForegroundColor Red
        Write-Host $errorContent
    }
}

# Read test results
$testResults = @()
if (Test-Path "test-output.json") {
    try {
        $jsonContent = Get-Content "test-output.json" -Raw
        $json = ConvertFrom-Json $jsonContent

        if ($json.PSObject.Properties['tests']) {
            $testResults = $json.tests
        }
    } catch {
        Write-Host "Warning: Could not parse JSON output" -ForegroundColor Yellow
    }
}

# Fallback: Parse raw test output
if ($testResults.Count -eq 0) {
    Write-Host "Running tests with verbose output..." -ForegroundColor Yellow

    $process2 = Start-Process -FilePath "dotnet" -ArgumentList $testArgs -WorkingDirectory $testProjectDir -NoNewWindow -PassThru -RedirectStandardOutput "test-verbose.txt"
    $null = $process2.WaitForExit()

    $verboseOutput = Get-Content "test-verbose.txt" -Raw

    # Parse test names and results
    $lines = $verboseOutput -split "`n"
    foreach ($line in $lines) {
        if ($line -match "✓|PASSED|Passed") {
            $testName = $line -replace ".*?::|✓|\[PASS.*?\]", "" -replace ".*?Error.*", ""
            if ($testName.Trim()) {
                $testResults += @{
                    name = $testName.Trim()
                    outcome = "Passed"
                    duration = "N/A"
                    errorMessage = ""
                }
            }
        } elseif ($line -match "✗|FAILED|Failed") {
            $testName = $line -replace ".*?::|✗|\[FAIL.*?\]", "" -replace ".*?Error.*", ""
            if ($testName.Trim()) {
                $testResults += @{
                    name = $testName.Trim()
                    outcome = "Failed"
                    duration = "N/A"
                    errorMessage = $line
                }
            }
        }
    }
}

# Calculate statistics
$passed = ($testResults | Where-Object { $_.outcome -eq "Passed" } | Measure-Object).Count
$failed = ($testResults | Where-Object { $_.outcome -eq "Failed" } | Measure-Object).Count
$total = $testResults.Count

Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Total:  $total" -ForegroundColor White
Write-Host "  Passed: $passed" -ForegroundColor Green
Write-Host "  Failed: $failed" -ForegroundColor Red
Write-Host ""

# Generate HTML Report
$htmlContent = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Test Report - User Management API</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
        }

        .container {
            max-width: 1000px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            overflow: hidden;
        }

        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }

        .header h1 {
            font-size: 32px;
            margin-bottom: 10px;
        }

        .header p {
            opacity: 0.9;
            font-size: 14px;
            margin-bottom: 20px;
        }

        .timestamp {
            font-size: 12px;
            opacity: 0.8;
        }

        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 15px;
            padding: 30px;
            background: #f8f9fa;
        }

        .stat {
            text-align: center;
            padding: 20px;
            background: white;
            border-radius: 8px;
            border-left: 4px solid #6c757d;
        }

        .stat.passed {
            border-left-color: #28a745;
        }

        .stat.failed {
            border-left-color: #dc3545;
        }

        .stat-value {
            font-size: 32px;
            font-weight: bold;
            color: #333;
        }

        .stat-label {
            font-size: 13px;
            color: #666;
            margin-top: 8px;
            text-transform: uppercase;
            font-weight: 500;
        }

        .content {
            padding: 30px;
        }

        .section-title {
            font-size: 18px;
            font-weight: 600;
            color: #333;
            margin-bottom: 20px;
            border-bottom: 2px solid #667eea;
            padding-bottom: 10px;
        }

        .test-list {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .test-item {
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 16px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .test-item.passed {
            background: #f0f8f5;
            border-left: 4px solid #28a745;
        }

        .test-item.failed {
            background: #fdf7f7;
            border-left: 4px solid #dc3545;
        }

        .test-name {
            font-weight: 500;
            color: #333;
            flex: 1;
        }

        .test-status {
            display: inline-block;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            text-transform: uppercase;
        }

        .status-passed {
            background: #d4edda;
            color: #155724;
        }

        .status-failed {
            background: #f8d7da;
            color: #721c24;
        }

        .test-error {
            margin-top: 10px;
            padding: 10px;
            background: #fff3cd;
            border-left: 3px solid #ffc107;
            border-radius: 4px;
            font-size: 12px;
            color: #856404;
            font-family: 'Courier New', monospace;
        }

        .footer {
            background: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #666;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }

        .summary {
            background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 30px;
            border-left: 4px solid #667eea;
        }

        .summary p {
            color: #333;
            font-size: 14px;
            line-height: 1.6;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🧪 Test Report</h1>
            <p>Section E: Authorization & Access Control Tests</p>
            <p class="timestamp">Generated: $(Get-Date -Format 'MMMM dd, yyyy HH:mm:ss')</p>
        </div>

        <div class="stats">
            <div class="stat">
                <div class="stat-value">$total</div>
                <div class="stat-label">Total Tests</div>
            </div>
            <div class="stat passed">
                <div class="stat-value">$passed</div>
                <div class="stat-label">Passed</div>
            </div>
            <div class="stat failed">
                <div class="stat-value">$failed</div>
                <div class="stat-label">Failed</div>
            </div>
        </div>

        <div class="content">
            <div class="summary">
                <p><strong>Summary:</strong> $total tests executed. $passed passed, $failed failed. Success rate: $(if ($total -gt 0) { [math]::Round(($passed/$total)*100, 2) }% else { "N/A" })</p>
            </div>

            <div class="section-title">Test Results</div>
            <div class="test-list">
"@

# Add test results to HTML
foreach ($test in $testResults) {
    $statusClass = if ($test.outcome -eq "Passed") { "passed" } else { "failed" }
    $statusBadgeClass = if ($test.outcome -eq "Passed") { "status-passed" } else { "status-failed" }
    $statusText = if ($test.outcome -eq "Passed") { "✅ Passed" } else { "❌ Failed" }

    $htmlContent += @"
                <div class="test-item $statusClass">
                    <div class="test-name">$($test.name -replace '^[A-Za-z0-9]+\s*', '')</div>
                    <span class="test-status $statusBadgeClass">$statusText</span>
                </div>
"@

    if ($test.errorMessage -and $test.outcome -eq "Failed") {
        $htmlContent += @"
                <div class="test-error">
                    $($test.errorMessage -replace '<', '&lt;' -replace '>', '&gt;')
                </div>
"@
    }
}

$htmlContent += @"
            </div>
        </div>

        <div class="footer">
            <p>Generated by Test Report Generator • User Management API Test Suite</p>
        </div>
    </div>
</body>
</html>
"@

# Write HTML to file
$htmlContent | Out-File -FilePath $OutputPath -Encoding UTF8
Write-Host "✅ Report generated: $OutputPath" -ForegroundColor Green
Write-Host ""

# Clean up temporary files
Remove-Item -Force -ErrorAction SilentlyContinue "test-output.json"
Remove-Item -Force -ErrorAction SilentlyContinue "test-error.txt"
Remove-Item -Force -ErrorAction SilentlyContinue "test-verbose.txt"

# Open report in browser if requested
if ($PSVersionTable.Platform -eq "Win32NT" -or $PSVersionTable.OS -like "Windows*") {
    $fullPath = (Resolve-Path $OutputPath).Path
    Write-Host "Opening report in browser..." -ForegroundColor Cyan
    Start-Process $fullPath
}
