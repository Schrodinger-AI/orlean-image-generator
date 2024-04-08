FROM mcr.microsoft.com/dotnet/aspnet:8.0.3-jammy-chiseled

WORKDIR /app
COPY bin/Release/net8.0/publish/ /app/

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["dotnet", "WebApiClient.dll"]