FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80

# Install ICU for globalization support
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LANG=en_US.UTF-8
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["OpenAutomate.API/OpenAutomate.API.csproj", "OpenAutomate.API/"]
COPY ["OpenAutomate.Core/OpenAutomate.Core.csproj", "OpenAutomate.Core/"]
COPY ["OpenAutomate.Infrastructure/OpenAutomate.Infrastructure.csproj", "OpenAutomate.Infrastructure/"]
RUN dotnet restore "OpenAutomate.API/OpenAutomate.API.csproj"
COPY . .
WORKDIR "/src/OpenAutomate.API"
RUN dotnet build "OpenAutomate.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenAutomate.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenAutomate.API.dll"]