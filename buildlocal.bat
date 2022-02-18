@echo off
setlocal

pushd %~dp0
rmdir /s/q publish
dotnet build /p:Configuration=Debug
dotnet pack --output publish /p:Configuration=Debug -p:DeployOnBuild=true /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg
dotnet nuget push publish\*.nupkg -k %LOCAL_NUGET_APIKEY% -s http://localhost:8090/v3/index.json
popd
