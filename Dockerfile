FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG servicename
WORKDIR /app
COPY out/$servicename .