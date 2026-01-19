cd C:\Users\Guilherme\source\repos\TheThroneOfGames
dotnet build 2>&1 | Select-String "êxito|error" | Select-Object -Last 1
