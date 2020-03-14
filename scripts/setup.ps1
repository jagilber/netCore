dotnet tool install -g dotnet-warp
dotnet-warp -nc -v
#Unhandled Exception: System.InvalidOperationException: Sequence contains no elements
dotnet tool install --global dotnet-trace
dotnet-trace

dotnet-trace collect --process-id <PID> --providers Microsoft-Windows-DotNETRuntime

Press <Enter> to exit...
Connecting to process: <Full-Path-To-Process-Being-Profiled>/dotnet.exe
Collecting to file: <Full-Path-To-Trace>/trace.nettrace
  Session Id: <SessionId>
  Recording trace 721.025 (KB)