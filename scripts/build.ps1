	
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishedTrimmed=true

dotnet publish --self-contained

dotnet publish -r win-x64 -c Release --self-contained $true --no-dependencies -p:PublishSingleFile=true -p:PublishedTrimmed=true

