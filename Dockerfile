
FROM microsoft/dotnet:3.1

WORKDIR /opt/bin

COPY Build/netcoreapp3.1/publish /opt/bin

EXPOSE 5061

EXPOSE 50051

ENTRYPOINT ["dotnet", "GB28181.Service.dll"]
