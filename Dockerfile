FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base

WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5060
EXPOSE 8080


FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["GB28181.Server/GB28181.Server.csproj", "GB28181.Server/"]
RUN dotnet restore "GB28181.Server/GB28181.Server.csproj"
COPY . .
WORKDIR "/src/GB28181.Server"
RUN dotnet build "GB28181.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GB28181.Server.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GB28181.Server.dll"]