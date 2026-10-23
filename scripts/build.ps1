param([ValidateSet('x64','x86')][string]$Architecture = 'x64')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdkVersion = '1.0.4314.35'
$sdkSHA256 = '90778DEB1B77E36292828B4B5833C9255C27533F40E0DDD55891AE718944EDC4'
$sdkURL = 'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4314.35/microsoft.web.webview2.1.0.4314.35.nupkg'
$dependencyRoot = Join-Path $projectRoot '.deps'
$package = Join-Path $dependencyRoot ('microsoft.web.webview2.' + $sdkVersion + '.nupkg')
$sdkRoot = Join-Path $dependencyRoot ('webview2-' + $sdkVersion)
$output = Join-Path $projectRoot ('dist\win-' + $Architecture)
New-Item -ItemType Directory -Path $dependencyRoot,$output -Force | Out-Null
if (-not (Test-Path -LiteralPath $package)) { Invoke-WebRequest -Uri $sdkURL -OutFile $package }
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $sdkSHA256) { throw 'WebView2 SDK SHA256 mismatch. Do not use this package.' }
if (-not (Test-Path -LiteralPath $sdkRoot)) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($package,$sdkRoot)
}
# Verify the exact extracted files used by the build against the verified archive.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$requiredEntries=@('lib/net462/Microsoft.Web.WebView2.Core.dll',('runtimes/win-'+$Architecture+'/native/WebView2Loader.dll'),'LICENSE.txt','NOTICE.txt')
$archive=[IO.Compression.ZipFile]::OpenRead($package)
try {
    foreach($entryName in $requiredEntries) {
        $entry=$archive.GetEntry($entryName)
        if($null -eq $entry){throw "SDK archive entry missing: $entryName"}
        $entryStream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
        try{$expected=([BitConverter]::ToString($sha.ComputeHash($entryStream))).Replace('-','')}finally{$entryStream.Dispose();$sha.Dispose()}
        $actualPath=Join-Path $sdkRoot $entryName
        if(-not(Test-Path -LiteralPath $actualPath) -or (Get-FileHash -LiteralPath $actualPath -Algorithm SHA256).Hash -ne $expected){throw "Extracted SDK file differs from verified package: $entryName"}
    }
} finally {$archive.Dispose()}
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) { $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The built-in .NET Framework compiler is unavailable. No software was installed.' }
$platform = $Architecture
$exe = Join-Path $output 'SparkRefreshLab.exe'
$compilerArgs = @('/nologo','/target:winexe',('/platform:'+$platform),'/optimize+','/checked+','/utf8output',('/out:'+$exe),('/win32manifest:'+(Join-Path $projectRoot 'src\app.manifest')),('/resource:'+(Join-Path $projectRoot 'src\animation.html')+',animation.html'),('/reference:'+(Join-Path $framework 'System.Windows.Forms.dll')),('/reference:'+(Join-Path $framework 'System.Drawing.dll')),('/reference:'+(Join-Path $framework 'System.Web.Extensions.dll')),('/reference:'+(Join-Path $sdkRoot 'lib\net462\Microsoft.Web.WebView2.Core.dll')),(Join-Path $projectRoot 'src\SparkRefreshLab.cs'))
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "C# build failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $sdkRoot 'lib\net462\Microsoft.Web.WebView2.Core.dll') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot ('runtimes\win-'+$Architecture+'\native\WebView2Loader.dll')) -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\SparkRefreshLab.exe.config') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot 'LICENSE.txt') -Destination (Join-Path $output 'WebView2.LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot 'NOTICE.txt') -Destination (Join-Path $output 'WebView2.NOTICE.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Start hidden.vbs'),(Join-Path $PSScriptRoot 'Open controls.vbs') -Destination $output -Force
[ordered]@{sdkVersion=$sdkVersion;sdkURL=$sdkURL;sdkSHA256=$sdkSHA256;architecture=$Architecture;platform=$platform;compiler=$compiler;files=@(Get-ChildItem -LiteralPath $output -File | Where-Object Name -ne 'build-manifest.json' | ForEach-Object { [ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'build-manifest.json') -Encoding UTF8
Write-Output $exe
