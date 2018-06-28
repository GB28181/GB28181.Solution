@rem Copyright 2016 gRPC authors.
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

..\..\packages\Grpc.Tools.1.8.0\tools\windows_x64\protoc.exe -I../protos --csharp_out ../ --grpc_out ../ --plugin=protoc-gen-grpc=..\..\packages\Grpc.Tools.1.8.0\tools\windows_x64\grpc_csharp_plugin.exe ../protos/ptz_control.proto