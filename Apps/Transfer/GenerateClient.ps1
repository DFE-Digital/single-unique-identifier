dotnet build ./src/SUI.Transfer.API/SUI.Transfer.API.csproj
Copy-Item -Path ./src/SUI.Transfer.API/obj/SUI.Transfer.API.json -Destination ./src/SUI.Transfer.API.Client/SUI.Transfer.API.json -Force
dotnet nswag run