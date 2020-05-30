# 说明

情况说明，希望有空有时间的朋友一起参与完善，提出好的想法、间见解，我们一起完善。

## GRPC Server

+ 迁移到apsnetocre3.1+ 之后GRPC这部分单独启动Server的代码九不再需要了，aspnetcore自己已经支持了。
+ 需要移除掉这部分功能。
+ 服务运行起来之后，会按照Proto接口定义的功能提供服务，允许其他服务通过GRPC调用。
+ gb28181.server  iis express 跑起来后接口post地址:http://localhost:60500/v1/PtzControl/PtzDirect  参数json如:{"xyz":{"X":0,"Y":4,"Z":0},"speed":3,"deviceid":"78978201001320000025"}

## MediaEventSource

+ 这个接口是需要的，
+ 项目写的比较急，所有有些地方结构性很差
+ 中间一段时间有其他伙伴参与开发，设计思路上又没有沟通的很好，所以就显得更乱
+ 有好的想法和建议，大家一起完善

