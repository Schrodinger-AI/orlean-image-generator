FROM mcr.microsoft.com/dotnet/runtime:8.0.3-jammy-chiseled
ARG servicename
WORKDIR /App
COPY out/$servicename .
ENV RUNCMD="dotnet $servicename.dll"
CMD $RUNCMD