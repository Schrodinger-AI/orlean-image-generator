# Use the official Microsoft .NET 5.0 SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

# Set the working directory in the image to /app
WORKDIR /app

# Copy the .csproj file and restore the NuGet packages
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the official Microsoft .NET 5.0 runtime image as the base image
FROM mcr.microsoft.com/dotnet/runtime:5.0

# Set the working directory in the image to /app
WORKDIR /app

# Copy the output of the dotnet publish command from the build image
COPY --from=build /app/out .

# Set the entry point of the application
ENTRYPOINT ["dotnet", "SiloHost.dll"]