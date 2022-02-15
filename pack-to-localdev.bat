@echo off

dotnet new tool-manifest --force
dotnet tool install inedo.extensionpackager

cd Node\InedoExtension
dotnet inedoxpack pack . C:\LocalDev\BuildMaster\Extensions\Node.upack --build=Debug -o
cd ..\..