
FROM microsoft/aspnetcore

WORKDIR /usr/local/src/app

COPY . /usr/local/src/app

EXPOSE 50051

ENTRYPOINT ["dotnet", "GB28181.Service.dll"]
