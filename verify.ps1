# One verification command: EditMode tests (smoke + missing references) in Unity batchmode.
# Exit codes: 0 passed, 2 failed (tests or compile), 3 could not run (editor open / Unity not installed).
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$results = Join-Path $project 'TestResults-EditMode.xml'
$log = Join-Path $project 'TestResults-EditMode.log'
$emptyRefs = Join-Path $project 'TestResults-EditMode-EmptyRefs.txt'

# The editor holds Temp/UnityLockfile while the project is open; batchmode cannot open the same project then.
$lock = Join-Path $project 'Temp\UnityLockfile'
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
    catch { Write-Output 'Unity editor has this project open. Close it or run tests through Unity MCP.'; exit 3 }
}

$version = (Select-String -Path "$project\ProjectSettings\ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
$unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
if (-not (Test-Path $unity)) { Write-Output "Unity $version not found at $unity."; exit 3 }

Remove-Item $results, $emptyRefs -ErrorAction SilentlyContinue
$p = Start-Process $unity -Wait -PassThru -NoNewWindow -RedirectStandardOutput "$log.stdout" -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', "`"$project`"",
    '-runTests', '-testPlatform', 'EditMode', '-testResults', "`"$results`"", '-logFile', "`"$log`"")
Remove-Item "$log.stdout" -ErrorAction SilentlyContinue

$code = if ($p.ExitCode -eq 0) { 0 } else { 2 }
if (Test-Path $results) {
    $run = ([xml](Get-Content $results -Raw)).SelectSingleNode('//test-run')
    Write-Output "Tests: $($run.passed)/$($run.total) passed, $($run.failed) failed"
    $run.SelectNodes("//test-case[@result='Failed']") |
        ForEach-Object { Write-Output "- $($_.fullname): $($_.failure.message.'#cdata-section'.Trim())" }
} else {
    Write-Output "No test results (Unity exit $($p.ExitCode)). Compile errors:"
    Select-String -Path $log -Pattern 'error CS\d+' | Select-Object -First 10 | ForEach-Object { Write-Output "- $($_.Line.Trim())" }
}
if (Test-Path $emptyRefs) { Write-Output "Empty fields (report only): $(@(Get-Content $emptyRefs).Count) -> $emptyRefs" }
Write-Output "Report: $results"
Write-Output "Log: $log"
Write-Output "Exit: $code"
exit $code
