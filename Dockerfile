FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project files from their folders
COPY ["SocialMedia.Api/SocialMedia.Api.csproj", "SocialMedia.Api/"]
COPY ["SocialMedia.Application/SocialMedia.Application.csproj", "SocialMedia.Application/"]
COPY ["SocialMedia.Domain/SocialMedia.Domain.csproj", "SocialMedia.Domain/"]
COPY ["SocialMedia.Infrastructure/SocialMedia.Infrastructure.csproj", "SocialMedia.Infrastructure/"]

# Restore using the path to the API project
RUN dotnet restore "SocialMedia.Api/SocialMedia.Api.csproj"

# Copy the rest of the source code
COPY . .


WORKDIR "/src/SocialMedia.Api"
RUN dotnet build "SocialMedia.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SocialMedia.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SocialMedia.Api.dll"]
