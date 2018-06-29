
FROM microsoft/aspnetcore:2.1
LABEL Name=GB28181.Service Version=6.0.0
ARG source=.

ADD ./ /usr/local/src

WORKDIR /usr/local/src/app

RUN cd /usr/local/src/

RUN dotnet restore

RUN dotnet build

EXPOSE 3000
COPY $source .

ENTRYPOINT dotnet GB28181.Service.dll
