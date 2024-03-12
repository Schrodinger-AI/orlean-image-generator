FROM mcr.microsoft.com/dotnet/sdk:8.0 AS fetch-env
WORKDIR /App

COPY . ./
RUN dotnet restore

FROM fetch-env AS build-env
ARG servicename
RUN dotnet publish $servicename/$servicename.csproj -o /App/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENV RUNCMD="dotnet $servicename.dll"
CMD $RUNCMD