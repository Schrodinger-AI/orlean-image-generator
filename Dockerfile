FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG servicename
WORKDIR /App
COPY out/$servicename /App/out .
ENV RUNCMD="dotnet $servicename.dll"
CMD $RUNCMD