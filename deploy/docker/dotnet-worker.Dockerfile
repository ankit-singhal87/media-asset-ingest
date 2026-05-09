FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG PROJECT_PATH
COPY . .
RUN dotnet publish "$PROJECT_PATH" \
    --configuration Release \
    --output /app/publish \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

ARG DLL_NAME
ENV DOTNET_WORKER_DLL=$DLL_NAME
COPY --from=build /app/publish .

ENTRYPOINT ["/bin/sh", "-c", "dotnet \"$DOTNET_WORKER_DLL\""]
