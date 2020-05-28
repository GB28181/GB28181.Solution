# 模块关系说明

+ **GB28181.Server** GB28181服务启动和服务承载(HOST)模块,是服务的入口，服务从这里启动.
+ **GB28181.SIPSorcery** GB28181的业务模块，依赖并在逻辑关系上继承sipsorcery，其中有一部分代码与sipsorcery是重合的，正在除去重合的部分.
+ **Logger4Net** 兼容Log4net日志接口的日志类库，可以在这里扩展log逻辑将日志输入/输出到需要目标
+ **Common** 通用模块，主要提供泛型，网络，流相关的一些操作，以及一些帮助类
+ **sipsorcery** SIP媒体协议栈以及相关的实现，支持WebRTC/WebSocket.
+ **sipsorcery.Unitests** 就是名字的字面意思，sipsorcery的单元测试,目前是直接搬过来了，方便参考代码.
+ **GB28181.WinTool** 一个只能运行windows OS上的工具，辅助开发和测试，主要依赖StreamingKit处理流.
+ **StreamingKit** 一个用于多媒体流处理的库，主要用于处理TS/PS流和mp4包.
+ **Testing** 面向GB28181业务开发装测试case.
+ **Microsoft.AspNetCore.Grpc.HttpApi**  与  **Microsoft.AspNetCore.Grpc.Swagger**  是来自微软AspLabs的REST to GRPC的扩展模块.