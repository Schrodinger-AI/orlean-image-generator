FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG servicename
USER 1000
WORKDIR /App
COPY out/$servicename .
ENV RUNCMD="dotnet $servicename.dll"
CMD $RUNCMD