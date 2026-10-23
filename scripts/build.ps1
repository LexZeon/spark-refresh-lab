param([ValidateSet('x64','x86')][string]$Architecture = 'x64', [ValidateSet('zh-CN','en-US')][string]$Language = 'zh-CN', [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sdkVersion = '1.0.4314.35'
$sdkSHA256 = '90778DEB1B77E36292828B4B5833C9255C27533F40E0DDD55891AE718944EDC4'
$sdkURL = 'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4314.35/microsoft.web.webview2.1.0.4314.35.nupkg'
$dependencyRoot = Join-Path $projectRoot '.deps'
$package = Join-Path $dependencyRoot ('microsoft.web.webview2.' + $sdkVersion + '.nupkg')
$sdkRoot = Join-Path $dependencyRoot ('webview2-' + $sdkVersion)
$output = if($OutputDirectory) { if([IO.Path]::IsPathRooted($OutputDirectory)){[IO.Path]::GetFullPath($OutputDirectory)}else{[IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))} }else{Join-Path $projectRoot ('dist\v0.3.0-' + $Language + '-win-' + $Architecture)}
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
$english = $Language -eq 'en-US'
$exeName = if($english){'SparkRefreshLab-EN.exe'}else{'SparkRefreshLab-ZH.exe'}
$exe = Join-Path $output $exeName
$compilerArgs = @('/nologo','/target:winexe',('/platform:'+$platform),'/optimize+','/checked+','/utf8output',('/out:'+$exe),('/win32manifest:'+(Join-Path $projectRoot 'src\app.manifest')),('/resource:'+(Join-Path $projectRoot 'src\animation.html')+',animation.html'),('/reference:'+(Join-Path $framework 'System.Windows.Forms.dll')),('/reference:'+(Join-Path $framework 'System.Drawing.dll')),('/reference:'+(Join-Path $framework 'System.Web.Extensions.dll')),('/reference:'+(Join-Path $sdkRoot 'lib\net462\Microsoft.Web.WebView2.Core.dll')),(Join-Path $projectRoot 'src\SparkRefreshLab.cs'))
$compilerArgs += @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll','WindowsFormsIntegration.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_)) }
$compilerArgs += '/reference:' + (Join-Path $framework 'System.Xaml.dll')
$compilerArgs += Join-Path $projectRoot 'src\NativeRefreshSurfaces.cs'
$compilerArgs += Join-Path $projectRoot 'src\UiText.cs'
if($english){$compilerArgs += '/define:UI_ENGLISH'}
& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "C# build failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $sdkRoot 'lib\net462\Microsoft.Web.WebView2.Core.dll') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot ('runtimes\win-'+$Architecture+'\native\WebView2Loader.dll')) -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\SparkRefreshLab.exe.config') -Destination ($exe + '.config') -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot 'LICENSE.txt') -Destination (Join-Path $output 'WebView2.LICENSE.txt') -Force
Copy-Item -LiteralPath (Join-Path $sdkRoot 'NOTICE.txt') -Destination (Join-Path $output 'WebView2.NOTICE.txt') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination (Join-Path $output 'LICENSE') -Force
# Each language package gets its own complete, matching launchers and guide.
$launcherTitle=if($english){'Screen Refresh Helper (English)'}else{'屏幕刷新助手（中文版）'}
$launcherError=if($english){'The app file was not found. Extract the complete package and try again.'}else{'找不到程序文件。请完整解压软件包后重试。'}
function Write-LocalizedLauncher([string]$Name,[string]$Arguments){
    $template=@'
Option Explicit
Dim shell, files, folder, executable
Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
folder = files.GetParentFolderName(WScript.ScriptFullName)
executable = files.BuildPath(folder, "__EXE__")
If files.FileExists(executable) Then
  shell.Run Chr(34) & executable & Chr(34) & "__ARGS__", 1, False
Else
  MsgBox "__ERROR__", 48, "__TITLE__"
End If
'@
    $content=$template.Replace('__EXE__',$exeName).Replace('__ARGS__',$Arguments).Replace('__ERROR__',$launcherError).Replace('__TITLE__',$launcherTitle)
    [IO.File]::WriteAllText((Join-Path $output $Name),$content,[Text.Encoding]::Unicode)
}
if($english){
    Write-LocalizedLauncher 'Open controls.vbs' ''
    Write-LocalizedLauncher 'Start moving ball.vbs' ' --mode ball'
    Write-LocalizedLauncher 'Start canvas redraw.vbs' ' --mode canvas'
    $guide=Join-Path $projectRoot 'docs\continuous-guide-en.txt';$guideName='README.txt'
}else{
    Write-LocalizedLauncher '打开控制面板.vbs' ''
    Write-LocalizedLauncher '小球移动（持续）.vbs' ' --mode ball'
    Write-LocalizedLauncher '画布重画（持续）.vbs' ' --mode canvas'
    $guide=Join-Path $projectRoot 'docs\本地持续模式使用说明.txt';$guideName='使用说明.txt'
}
if(Test-Path -LiteralPath $guide){Copy-Item -LiteralPath $guide -Destination (Join-Path $output $guideName) -Force}
[ordered]@{appVersion='0.3.0';language=$Language;executable=$exeName;sdkVersion=$sdkVersion;sdkURL=$sdkURL;sdkSHA256=$sdkSHA256;architecture=$Architecture;platform=$platform;compiler=$compiler;files=@(Get-ChildItem -LiteralPath $output -File | Where-Object Name -ne 'build-manifest.json' | ForEach-Object { [ordered]@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'build-manifest.json') -Encoding UTF8
Write-Output $exe
