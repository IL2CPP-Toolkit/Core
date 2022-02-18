@echo off
setlocal

pushd %~dp0
rmdir /s/q publish
dotnet pack --output publish /p:Configuration=Release -p:DeployOnBuild=true /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg
dotnet nuget push publish\*.nupkg -k %LOCAL_NUGET_APIKEY% -s http://localhost:8090/v3/index.json
popd
