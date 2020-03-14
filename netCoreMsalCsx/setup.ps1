Install-Package Microsoft.CodeAnalysis.Scripting.CSharp

dotnet tool install -g dotnet-script

# https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples#multi
dotnet add package Microsoft.CodeAnalysis.CSharp.Scripting

using Microsoft.CodeAnalysis;

$dllpath = "C:\Users\user\.nuget\packages\microsoft.codeanalysis.csharp.scripting\3.4.0\lib\netstandard2.0\Microsoft.CodeAnalysis.CSharp.Scripting.dll"
$r = [System.Reflection.Assembly]::LoadFile($dllpath)

Microsoft.CodeAnalysis.CSharp.Scripting
Microsoft.CodeAnalysis.Scripting

Microsoft.CodeAnalysis.Scripting.CSharp <-- not found