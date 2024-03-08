# Image Generator Orleans

API and backend to generate images for Schrodinger-NFT project

## Tech Stack

- OrleansFramework
- C#
- .NET 8.0
- MySQL
- Docker

## Module Specifications

Orlean Solution contains 3 projects
1. Grains
2. SiloHost
3. WebApiClient


## Project Setup Guide

### MySQL Docker

- Start MySQL via docker using [docker-compose](./setup-guide/database/docker-compose.yml)

### Database Setup

- OrleanRuntime needs system tables in the database to manage the persistence of inmemory states in Grains
- Refer to [orleans-database-setup](./setup-guide/database/orleans-database-setup.MD)
- Ensure all database tables, procedures and views are created (compare the created entities with the sql script)

### Environment Variables

- Use the document [orleans-environment-variables](./setup-guide/orleans-environment-variables.MD)

### How to Run

1. Navigate to `SiloHost` directory

```sh
dotnet run
```

Ensure the SiloHost startup is successful

2. Navigate to `WebApiClient` directory

```sh
dotnet run
```

Ensure the WebApiClient startup is successful


### Orleans Image Generation API - Usage

1. Traits are to be configured via API, traits can be added by copying `traits.json` content in this director

- or refer to the postman collection in this directory
[orlean-image-generator-postman-collection](./setup-guide/postman/Orlean%20Image%20Generator.postman_collection.json)
