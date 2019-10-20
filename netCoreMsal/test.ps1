<# dotnet run /resource https://db.kusto.windows.net/ 
    /clientId f18d4593-7c21-49d5-a233-0f74c4a600f1 
    /redirectUri msalf18d4593-7c21-49d5-a233-0f74c4a600f1://auth 
    /tenantId 5488c913-eff7-43ba-9c66-e440e82b3b71 
    /scope "https://db.kusto.windows.net//user_impersonation"

#>
param(
    $clientId,
    $tenantId,
    $redirectUri, #msal$clientId://auth 
    $resource,
    [string[]]$scope = @('user.read')
)
dotnet build;dotnet run /resource $resource /clientId $clientId /redirectUri $redirectUri /tenantId $tenantId /scope $scope