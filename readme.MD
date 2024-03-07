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


## Project Tech Specs and Logic



## Project Setup Guide

### MySQL Docker

- Start MySQL via docker using [docker-compose](./setup-guide/docker-compose.yml)

### Database Setup

- OrleanRuntime needs system tables in the database to manage the persistence of inmemory states in Grains
- Refer to [orleans-database-setup](./setup-guide/orleans-database-setup.MD)
- Ensure all database tables, procedures and viewas are created (compare the created entities with the sql script)

### Environment Variables

- Use the document [orleans-environment-variables](./setup-guide/orleans-environment-variables.MD)


## Dependency List



## API Specifications


