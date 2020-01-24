# script to build .net core msal projects on .net core 3.1 lts
# not working. project may need to be updated to 3.1 or this to 3.0
param(
    [string]$localProjectPath = "$env:temp\netCoreMsal",
    [string]$msalProject = "https://raw.githubusercontent.com/jagilber/netCore/master/netCoreMsal",
    [string]$dotnet = "C:\Program Files\dotnet\dotnet.exe"
)

function main() {
    $error.Clear()
    
    if ((test-path $dotnet)) {
        $dotnetVersion = [version](.$dotnet --list-runtimes).split()[1]
    }

    # install .net core 3.1
    if ($dotnetVersion -le "3.1") {
        write-host " updating dotnet $dotnetversion"
        #3.1 core runtime
        # https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-3.1.0-windows-x64-installer
        #$downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/9f010da2-d510-4271-8dcc-ad92b8b9b767/d2dd394046c20e0563ce5c45c356653f/dotnet-runtime-3.1.0-win-x64.exe"

        # 3.1 desktop
        # https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.0-windows-x64-installer
        #$downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/a1510e74-b31a-4434-b8a0-8074ff31fb3f/b7de8ecba4a14d8312551cfdc745dea1/windowsdesktop-runtime-3.1.0-win-x64.exe"

        # 3.1 sdk
        # https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.100-windows-x64-installer
        $downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/639f7cfa-84f8-48e8-b6c9-82634314e28f/8eb04e1b5f34df0c840c1bffa363c101/dotnet-sdk-3.1.100-win-x64.exe"

        $file = [io.path]::GetFileName($downloadUrl)
        download-file -sourceUri $downloadUrl -destinationPath $env:temp
        # /install /repair /uninstall /layout /passive /quiet /norestart /log
        start-process -wait -filePath "$env:temp\$file" -argumentList "/norestart /quiet /install /log `"$env:temp/$file.log`""
        type "$env:temp/$file.log"
    }
    else {
        write-host "dotnet $dotnetversion already installed"
    }

    if (!(test-path $localProjectPath)) {
        [io.directory]::CreateDirectory($localProjectPath)
        download-file -sourceUri ("$msalProject/netCoreMsal.csproj") -destinationPath $localProjectPath
        download-file -sourceUri ("$msalProject/Program.cs") -destinationPath $localProjectPath
        download-file -sourceUri ("$msalProject/TokenCacheHelper.cs") -destinationPath $localProjectPath
        #. $dotnet build $localProjectPath -r win-x64 -c Release
        . $dotnet -d publish $localProjectPath\netCoreMsal.csproj -c Release
    }
}

function download-file($sourceUri, $destinationPath = $PSScriptRoot) {
    # download
    $destinationFile = "$destinationPath\$([io.path]::GetFileName($sourceUri))"
    write-host "downloading $sourceUri to $destinationFile"
    (new-object net.webclient).downloadfile($sourceUri, $destinationFile)
}

main