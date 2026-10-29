# Claude Code hook: run Unity EditMode tests before the agent may finish a turn that touched .cs files.
#   mark  (PostToolUse Edit|Write|MultiEdit) : flag that tests are needed when a .cs file changed
#   gate  (Stop)                              : if flagged, run verify.ps1 (batchmode) or ask the agent to use Unity MCP
param([Parameter(Mandatory)][ValidateSet('mark', 'gate')][string]$Mode)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path "$PSScriptRoot\..\..").Path
$flag = Join-Path $project '.claude\.unity-tests-pending'
$attempts = Join-Path $project '.claude\.unity-tests-attempts'
$maxAttempts = 3

$hookInput = [Console]::In.ReadToEnd() | ConvertFrom-Json

if ($Mode -eq 'mark') {
    if ("$($hookInput.tool_input.file_path)" -like '*.cs') { Set-Content $flag '' }
    exit 0
}

if (-not (Test-Path $flag)) { Remove-Item $attempts -ErrorAction SilentlyContinue; exit 0 }

# ponytail: retry cap stops an endless fix loop; raise $maxAttempts if 3 is too few
$n = 1 + [int](Get-Content $attempts -ErrorAction SilentlyContinue)
if ($n -gt $maxAttempts) {
    Remove-Item $flag, $attempts -ErrorAction SilentlyContinue
    [Console]::Error.WriteLine("Unity tests still failing after $maxAttempts attempts. Stop and report the failures to the user.")
    exit 0
}
Set-Content $attempts $n

# The editor holds Temp/UnityLockfile while the project is open; batchmode cannot open the same project then.
$lock = Join-Path $project 'Temp\UnityLockfile'
$editorOpen = $false
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() } catch { $editorOpen = $true }
}

if ($editorOpen) {
    [Console]::Error.WriteLine(@"
C# files changed and the Unity editor is open. Before finishing:
1. Use the UnityMCP tools to refresh/compile, read the console for errors, and run EditMode tests.
2. Fix any failure.
3. When everything passes, delete .claude/.unity-tests-pending.
"@)
    exit 2
}

$report = (& "$project\verify.ps1") -join "`n"
switch ($LASTEXITCODE) {
    0 { Remove-Item $flag, $attempts -ErrorAction SilentlyContinue; exit 0 }
    3 { [Console]::Error.WriteLine("Tests were NOT run; tell the user.`n$report"); exit 0 }
    default { [Console]::Error.WriteLine("verify.ps1 failed (attempt $n/$maxAttempts). Fix before finishing.`n$report"); exit 2 }
}
