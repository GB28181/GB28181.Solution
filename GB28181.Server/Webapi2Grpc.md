# webapi to grpc 改造方法

该特性属于试验特性,目的是使得服务在支持GRPC的同时也能支持REST API调用,目前以包引用的方式存在。

1.  添加nuget包引用，在nuget管理器中增加包源:（不添加搜索不到）
    - 名称：DotNet5 dev repository 源：https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json
    - 搜索并添加 Microsoft.AspNetCore.Grpc.HttpApi  我的版本是：0.1.0-alpha.20254.1

2. 添加2个google  api包引用  `annotations.proto、http.proto`

**注意：** 根据添加文件的路径修改annotations.proto中的import引用http.proto的路径。descriptor.proto引用路径不变

## 1. 改造grpc服务的proto（我们选择PtzControl.proto做试验）

 + 引入 import "Protos/google/api/annotations.proto";
 + 改造rpc 添加处理

 原始： `rpc PtzDirect (PtzDirectRequest) returns (PtzDirectReply) {}`
 改造后：

 ```js
    rpc PtzDirect (PtzDirectRequest) returns (PtzDirectReply) {
            option (google.api.http) = {
              post: "/v1/PtzControl/PtzDirect"
              body: "*"
            };+
          }
 ```

**说明** 谓词：post，url可以自定义，因为输入参数是复杂类型，选择post方法，body选用通配符

1. 添加启动时服务引用
在startup.cs文件ConfigureServices中添加引用：`services.AddGrpcHttpApi();`
2. 最后编辑launchSettings(Properties/launchSettings.json)
目的是增加iis启动配置（WIndows Only),源文件如下：
```json
    {
      "iisSettings": {
        "windowsAuthentication": false,
        "anonymousAuthentication": true,
        "iisExpress": {
          "applicationUrl": "http://localhost:60500/",
          "sslPort": 44316
        }
      },
      "profiles": {
        "IIS Express": {
          "commandName": "IISExpress",
          "launchBrowser": true,
          "environmentVariables": {
            "ASPNETCORE_ENVIRONMENT": "Development"
          }
        },
        "GB28181.Service": {
          "commandName": "Project",
          "launchBrowser": true,
          "applicationUrl": "https://localhost:5001;http://localhost:5000",
          "environmentVariables": {
            "ASPNETCORE_ENVIRONMENT": "Development"
          }
        }
      }
    }
```