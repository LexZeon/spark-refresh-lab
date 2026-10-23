Option Explicit
Dim shell, files, folder, executable
Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
folder = files.GetParentFolderName(WScript.ScriptFullName)
executable = files.BuildPath(folder, "SparkRefreshLab.exe")
If Not files.FileExists(executable) Then
  executable = files.BuildPath(files.GetParentFolderName(folder), "dist\win-x64\SparkRefreshLab.exe")
End If
If files.FileExists(executable) Then
  shell.Run Chr(34) & executable & Chr(34), 0, False
Else
  MsgBox "Build SparkRefreshLab first (scripts\build.ps1).", 48, "Spark Refresh Lab"
End If
