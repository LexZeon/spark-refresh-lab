param([ValidateSet('x64','x86')][string]$Architecture='x64',[ValidateSet('zh-CN','en-US')][string]$Language='zh-CN',[switch]$ExperimentalBackgroundTimers,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$output=if($OutputDirectory){if([IO.Path]::IsPathRooted($OutputDirectory)){[IO.Path]::GetFullPath($OutputDirectory)}else{[IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))}}else{Join-Path $projectRoot ('dist\v0.3.0-'+$Language+'-win-'+$Architecture)}
$exe=Join-Path $output $(if($Language -eq 'en-US'){'SparkRefreshLab-EN.exe'}else{'SparkRefreshLab-ZH.exe'})
if(-not(Test-Path -LiteralPath $exe)){throw 'Build the app before the finite smoke test.'}
if(Get-Process SparkRefreshLab,SparkRefreshLab-ZH,SparkRefreshLab-EN -ErrorAction SilentlyContinue){throw 'A language version is already running. Stop it before the finite smoke test; no existing process was changed.'}
$args=@('--self-test')
if($ExperimentalBackgroundTimers){$args+='--experimental-background-timers'}
# Self-test never paints animation: hidden 4s, visible static 4s, stopped 1s, then closes.
$process=Start-Process -FilePath $exe -ArgumentList $args -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(35000)){
    # Stop only this launched app and runtimes matching its dedicated self-test profile.
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    $testProfile=Join-Path $env:LOCALAPPDATA ('SparkRefreshLab\WebView2-SelfTest-'+$process.Id)
    Get-CimInstance Win32_Process -Filter "Name='msedgewebview2.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf($testProfile,[StringComparison]::OrdinalIgnoreCase) -ge 0 } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    throw 'Finite smoke exceeded 35 seconds; only its own app/profile processes were terminated. This is a failed test.'
}
$logRoot=Join-Path $env:LOCALAPPDATA 'SparkRefreshLab\Logs'
$log=Get-ChildItem -LiteralPath $logRoot -File -Filter ('*-'+$process.Id+'.jsonl')|Sort-Object LastWriteTime -Descending|Select-Object -First 1
if($null -eq $log){throw 'Smoke test log missing.'}
$events=@(Get-Content -LiteralPath $log.FullName|ForEach-Object{$_|ConvertFrom-Json})
$init=@($events|Where-Object eventName -EQ 'initialized')
$shutdown=@($events|Where-Object eventName -EQ 'shutdown')
$samples=@($events|Where-Object eventName -EQ 'sample')
$errors=@($events|Where-Object { $_.eventName -match 'error|fatal|timeout' })
$modes=@($events|Where-Object eventName -EQ 'mode')
$hidden=@($samples|Where-Object {$_.data.mode -eq 'Hidden'})
$visible=@($samples|Where-Object {$_.data.mode -eq 'Visible'})
$stopped=@($modes|Where-Object {$_.data.mode -eq 'Stopped'})
$stoppedSamples=@($samples|Where-Object {$_.data.mode -eq 'Stopped'})
$stoppedConfirmed=$stoppedSamples.Count -gt 0 -and @($stoppedSamples|Where-Object {$_.data.page.running -ne $false -or $_.data.controllerIsVisible -ne $false -or $_.data.hostIsWindowVisible -ne $false}).Count -eq 0
$hiddenActuallyHidden=$hidden.Count -gt 0 -and @($hidden|Where-Object {$_.data.hostIsWindowVisible -ne $false}).Count -eq 0
$visibleActuallyVisible=$visible.Count -gt 0 -and @($visible|Where-Object {$_.data.hostIsWindowVisible -ne $true}).Count -eq 0
$noPaint=@($samples|Where-Object {$_.data.page.noPaint -ne $true}).Count -eq 0
$browserGone=$false
if($init.Count){$browserGone=$null -eq (Get-Process -Id $init[0].data.browserPid -ErrorAction SilentlyContinue)}
$result=[ordered]@{exitCode=$process.ExitCode;initialized=($init.Count -eq 1);runtimeVersion=if($init.Count){$init[0].data.runtimeVersion}else{$null};browserPid=if($init.Count){$init[0].data.browserPid}else{$null};browserExitObserved=if($shutdown.Count){$shutdown[-1].data.browserExitObserved}else{$false};browserProcessGone=$browserGone;errorCount=$errors.Count;hiddenActuallyHidden=$hiddenActuallyHidden;visibleActuallyVisible=$visibleActuallyVisible;stoppedStagePresent=($stopped.Count -gt 0);stoppedConfirmed=$stoppedConfirmed;selfTestNoPaint=$noPaint;samples=@($samples|ForEach-Object{$_.data});measurement='rAF callbacks only, not presentation or scanout.'}
$resultPath=Join-Path $projectRoot ('tests\smoke-'+$Architecture+$(if($ExperimentalBackgroundTimers){'-background-timers'}else{''})+'.json')
$result|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $resultPath -Encoding UTF8
if($process.ExitCode -ne 0 -or $init.Count -ne 1 -or $shutdown.Count -ne 1 -or -not $shutdown[-1].data.browserExitObserved -or -not $browserGone -or $errors.Count -gt 0 -or -not $hiddenActuallyHidden -or -not $visibleActuallyVisible -or $stopped.Count -eq 0 -or -not $stoppedConfirmed -or -not $noPaint){throw "Smoke test failed; see $resultPath and $($log.FullName)"}
Write-Output $resultPath
