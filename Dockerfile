FROM cgr.dev/chainguard/aspnet-runtime:latest
ARG servicename
WORKDIR /app
COPY out/$servicename .
