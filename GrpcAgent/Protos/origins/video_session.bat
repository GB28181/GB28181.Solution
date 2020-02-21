@rem Copyright 2018 husplus authors.
@rem
@rem Licensed under the Apache License, Version 2.0 (the "License");
@rem you may not use this file except in compliance with the License.
@rem You may obtain a copy of the License at
@rem
@rem     http://www.apache.org/licenses/LICENSE-2.0
@rem
@rem Unless required by applicable law or agreed to in writing, software
@rem distributed under the License is distributed on an "AS IS" BASIS,
@rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
@rem See the License for the specific language governing permissions and
@rem limitations under the License.
@rem Generate the C# code for .proto files


@echo off &TITLE Generation Protobuf Code For C#

mode con cols=100 lines=30
color 0D
cls

setlocal


@rem enter this directory of bat
cd /d %~dp0

@rem set path for rpc compiler tool
set PROTOC=C:\Users\h302201\.nuget\packages\grpc.tools\1.13.1\tools\windows_x64\protoc.exe
set PLUGIN=C:\Users\h302201\.nuget\packages\grpc.tools\1.13.1\tools\windows_x64\grpc_csharp_plugin.exe

@rem use csharp plugin compile *.proto to csharp code 
%PROTOC%   --csharp_out ../ --grpc_out ../ ./video_session.proto  --plugin=protoc-gen-grpc=%PLUGIN%

endlocal

echo.
echo.
echo          work had been done.
echo.
echo.
echo          code had been generated to  "../"
echo.
echo          press any key to exit
pause >nul