
FROM microsoft/dotnet:latest

WORKDIR /opt/bin

COPY Build/netcoreapp2.1/publish /opt/bin 

EXPOSE 5061

EXPOSE 50051

ENTRYPOINT ["dotnet", "GB28181.Service.dll"]
