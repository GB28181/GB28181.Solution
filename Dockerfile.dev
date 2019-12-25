FROM microsoft/dotnet:2.1-runtime-alpine3.7 AS build-env-GB28181-platform2016-service

WORKDIR /go/src/honeywell.com/husplus/GB28181.platform2016.service
COPY . .

RUN dotnet build GB28181.Platform.Service.sln -o gb28181service && \
     rm -rf ./vendor/*  && \
     rm -rf  ./*.md ./*.go

from microsoft/dotnet:2.1-runtime-alpine3.7

WORKDIR /app

COPY --from=build-env-GB28181-platform2016-service /go/src/honeywell.com/husplus/GB28181.platform2016.service/gb28181service /app

ENV PORT 8080
EXPOSE 8080

ENTRYPOINT ["./gb28181service"]