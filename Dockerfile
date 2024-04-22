FROM cgr.dev/chainguard/aspnet-runtime:latest
ARG servicename
WORKDIR /app
COPY out/$servicename .
COPY out/$servicename/$servicename.dll app.dll
ENTRYPOINT ["dotnet", "app.dll"]
