# grpc to webapi 改造方法

1.  添加引用
1.  nuget 包管理器中增加程序包源 。（不添加搜索不到）
  - 名称：DotNet5 dev repository
  源：https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json
  - 搜索并添加 Microsoft.AspNetCore.Grpc.HttpApi  我的版本是：0.1.0-alpha.20254.1
1. 添加2个google  api包引用。可以从项目中copy
annotations.proto、http.proto
**注意：** 根据添加文件的路径修改annotations.proto中的import引用http.proto的路径。descriptor.proto引用路径不变
1. 改造grpc服务的proto
我们选择PtzControl.proto做试验
 + 引入 import "Protos/google/api/annotations.proto";
 + 改造rpc 添加处理
 原始：
 `rpc PtzDirect (PtzDirectRequest) returns (PtzDirectReply) {}`
 改造后：
 
    rpc PtzDirect (PtzDirectRequest) returns (PtzDirectReply) {
            option (google.api.http) = {
              post: "/v1/PtzControl/PtzDirect"
              body: "*"
            };+
          }`
**说明：** 谓词：post，url可以自定义，因为输入参数是复杂类型，选择post方法，body选用通配符

1. 添加启动时服务引用
在startup.cs文件ConfigureServices中添加如下引用：
`services.AddGrpcHttpApi();`
1. 最后编辑launchSettings
路径：Properties/launchSettings.json
目的是增加iis启动配置
源文件如下：


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
