param(
    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2",
    $login = "https://login.microsoftonline.com/",
    $tenantId = "common",
    $redirectUri = (New-Object system.uri("http://localhost")),
    $scope = [collections.arraylist]@()
)

$error.clear()
$erroractionpreference = "continue"
#Add-Type -Path "$psscriptroot\bin\Debug\netcoreapp3.0\Microsoft.Identity.Client.dll"
Add-Type -Path ".\bin\Debug\netcoreapp3.0\Microsoft.Identity.Client.dll"

if($scope.count -lt 1)
{
    $scope.Add(".default")
    #$scope.Add("https://graph.microsoft.com/user.read")
}

$global:authenticationResult = $null
$global:publicClientApp = [Microsoft.Identity.Client.PublicClientApplicationBuilder]::Create($clientId).WithAuthority("https://login.microsoftonline.com", $tenantId).WithDefaultRedirectUri().Build()
$defaultAccount = ($publicClientApp.GetAccountsAsync().GetAwaiter().GetResult()| select -first 1)

try 
{
    #$global:authenticationResult = $publicClientApp.AcquireTokenSilent($scope, $publicClientApp.GetAccountsAsync().Result.FirstOrDefault())
    $global:authenticationResult = $publicClientApp.AcquireTokenSilent($scope, $defaultAccount)

    #$async = start-job -scriptblock { $global:authenticationResult = $publicClientApp.AcquireTokenSilent($scope, $publicClientApp.GetAccountsAsync().Result.FirstOrDefault()) }
    #wait-job $async
    #receive-job $async
}
catch 
{
    write-host ($error | out-string)
    $error.clear()
    #$global:authenticationResult = $publicClientApp.AcquireTokenInteractive($scope).ExecuteAsync().Result
    $global:authenticationResult = $publicClientApp.AcquireTokenInteractive($scope).ExecuteAsync().GetAwaiter().GetResult()

    #$async = start-job -scriptblock { $global:authenticationResult = $publicClientApp.AcquireTokenInteractive($scope).ExecuteAsync().Result }
    #wait-job $async
    #receive-job $async
}

write-host $global:authenticationResult
