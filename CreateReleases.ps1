$version = dotnet fsi updateVersion.fsx

Remove-Item "./output/" -recurse

dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj
mv "./output/CryptoGains.Console.exe" "./output/CryptoGains.windows.${version}.exe"

dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj
mv "./output/CryptoGains.Console" "./output/CryptoGains.linux.${version}.exe"

dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true --self-contained true -o "./output" .\src\CryptoGains.Console\CryptoGains.Console.fsproj
mv "./output/CryptoGains.Console" "./output/CryptoGains.mac.${version}.exe"

Remove-Item "./output/*.pdb"

echo "Built version: $version"
