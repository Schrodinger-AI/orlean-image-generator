FROM cgr.dev/chainguard/aspnet-runtime:latest
ARG servicename
WORKDIR /App
COPY out/$servicename .
ENTRYPOINT ["dotnet", "$servicename.dll"]
