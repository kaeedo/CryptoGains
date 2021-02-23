$version = dotnet fsi updateVersion.fsx

dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj
dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj
dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj

echo "Versionreg: $version"
