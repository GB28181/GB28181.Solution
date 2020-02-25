# GB28181 Standard

+ 当前版本以GB2014为基础逐渐增加GB2016的国标要求，最新国标是：【[GB28181-2016](docs/GBT%2028181-2016%20公共安全视频监控联网系统信息传输、交换、控制技术要求-目录版.pdf)】
+ 项目结构、代码结仍存在不少问题，待完善,因为时间问题，也是希望大家能一起完善
+ 希望每一个对本项目感兴趣的朋友，都能成为本项目的共同作者或者贡献者

## 运行环境(environment)

~~~ bash
running in docker
running on Linux
running on aspnetcore 3.1+
~~~

## 说明(instruction)

+ Mainly on `develop` branch ,in order to support .net core3.1+.
  + 以develop分支为主.
+ The windows function part was not maintained .
  + Form Client Project Need to be fixed.
+ you can reffer to other branch in this repo , `PRs` are always welcome.
+ provide grpc interface for other microservice

## 一些要做的事情(TODO List)

项目希望达到的目标功能,如下:

打勾的是已完成的，没打勾的是正在做的，需要大家一起完成的。

+ Architecture架构
  + [x] 设计与流媒体服务交互的GRPC接口
  + [x] 设计与系统配置服务(或数据服务)交互的GRPC接口
  + [x] 精简服务模块，调整代码结构关系
  + [ ] 为配置接口和流媒体服务接口提供mock数据,使得服务可以独立运行
  + [ ] 以GRPC方式对接流媒体服务[EasydarwinGo](https://www.github.com/gb28181/easydawingo)
  + [ ] 以GRPC方式从系统配置服务(或者数据服务)中获取GB信令服务的配置信息，包括名称、ID、端口、协议等
  + [ ] 使服务注册组件变成可配置的，(当前是consul，并且k8s环境中也不需要)
  + [x] 将GRPC服务的实现改为apsnetcore3.1+的内置实现方式.
  + [ ] 从GB28181.Sipsorcery项目中将原始的Sipsorcery项目分离出来

+ SIP信令服务
  + [x] 对接GB28181设备,实现基本的设备控制(暂不含双向语音和巡航等功能)
    + [x] Device Registering And managemment
    + [x] Device Controlling Service such as :PTZ
    + [x] Device Catalog Query
    + [x] Device Info Query
    + [x] Device Live Video
    + [x] Device History Video Query
  + [x] 对接GB28181平台，实现完整的平台级联控制。
  + [x] 注册到服务的设备信息缓存
  + [ ] 注册到服务的平台信息缓存，待进一步测试

+ Streaming Media(流媒体，以EasydarwinGo为基础)
  + [ ] 实现SIP信令服务与流媒体服务交互的GRPC接口。
  + [ ] 实现完整的实时视频播放功能, Video Live Play
  + [ ] 实现完整的历史视频搜索功能，History Video Record Search
  + [ ] 实现完整的历史视频播放功能, History Video PlayBack

## License

BSD v2

## Inspired By

+ [GB28181_Platform2016_Test](https://github.com/qinpengit/Gb28181_Platform2016_Test)

+ [sipsorcery](https://github.com/sipsorcery/sipsorcery)

+ [GB28181-2016(C/C++)](https://github.com/usecpp/gb28181-2016)


## 讨论、成为共同作者、近距离贡献

微信扫描二维码，添加好友，进入微信讨论群(注明：GB28181+公司+姓名)：

![qrcode](./docs/crazybber.jpg)

