
FROM microsoft/aspnetcore:2
LABEL Name=gb28181_platform2016_test Version=0.0.1
ARG source=.

ADD ./ /usr/local/src

WORKDIR /usr/local/src/app

RUN cd /usr/local/src/

RUN dotnet restore

RUN dotnet build

EXPOSE 3000
COPY $source .

ENTRYPOINT dotnet gb28181_platform2016_test.dll
