FROM cgr.dev/chainguard/aspnet-runtime:latest
ARG servicename
WORKDIR /App
COPY out/$servicename .
ENV APPNAME=$servicename
ENTRYPOINT ["dotnet", "${APPNAME}.dll"]
