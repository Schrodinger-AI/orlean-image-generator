FROM mcr.microsoft.com/dotnet/aspnet:8.0.3-jammy-chiseled
ARG servicename
WORKDIR /App
COPY out/$servicename .
CMD ["dotnet", "$servicename.dll"]