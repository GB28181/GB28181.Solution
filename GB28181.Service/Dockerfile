
FROM microsoft/dotnet:2.1-aspnetcore-runtime

WORKDIR /app

COPY . .

EXPOSE 50051
EXPOSE 5061

ENTRYPOINT ["dotnet", "GB28181.Service.dll"]
