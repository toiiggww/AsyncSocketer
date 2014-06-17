# AsyncSocketer
AsyncSocketer 是对标准套接字的以事件驱动的封装，修改自CodeProject中关于高性能异步Socket的文章：[C# SocketAsyncEventArgs High Performance Socket Code](http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod) 。该文章的主要目的是完善MSDN中[关于异步Socket简介](http://msdn.microsoft.com/zh-tw/library/vstudio/system.net.sockets.socketasynceventargs(v=vs.90).aspx)中的示例项目。该文的思路是提供一个缓冲管理类 BufferManager 用于接收和发送网络数据，提供一个保存套接字上下文的UserToken，以及拟定收发数据的协议定义及解析。

本项目是继承上文中缓存管理和套接字上下文，并于套接字的连接、接收、发送、断开时，以事件通知的形式，简化套接字中关于接收和发送的直接调用。

本项目提供以下类以实现项目目的：

###`事件委托`
* `SocketEventHandler`
套接字事件委托
* `ServerSocketEventHandler`
服务端套接字事件委托
* `SocketErrorHandler`
套接字错误事件委托
* `SocketPerformanceHandler`
套接字性能计数事件委托

###`接口`
* `IDentity`
标识定义

###`类`
* `SocketConfigure`
AsyncSocketer 参数设置定义
* `ISocketer`
标准套接字封装
* `TcpSocketer : ISocketer`
使用 TCP 协议的套接字
* `UdpSocketer : ISocketer`
使用 UDP 协议的套接字
* `PerformanceBase : Component`
性能计数基础对象定义
* `EventSocketer : PerformanceBase`
异步套接字，标准套接字的事件驱动封装
* `PartialSocket : EventSocketer`
使用多个 SocketAsyncEventArgs 池和缓存池分开管理事件的异步套接字
* `PartialSocketServer : PartialSocket`
分开管理事件的服务端异步套接字
* `MixedSocket : EventSocketer`
使用同一个 SocketAsyncEventArgs 池和缓存池分开管理事件的异步套接字
* `BufferManager`
管理缓存
* `SocketEventArgs : EventArgs`
套接字事件参数
* `ServerSocketEventArgs : SocketEventArgs`
服务端套接字事件参数
* `SocketErrorArgs : EventArgs`
套接字错误事件参数
* `PerformanceCountArgs : EventArgs`
套接字性能计数事件参数
* `ServerPerformanceCountArgs : PerformanceCountArgs`
服务端套接字性能计数事件参数
* `Pooler<TEArtType> where TEArtType : IDentity`
抽象对象池
* `EventArgObject : IDentity`
SocketAsyncEventArgs 的 IDentity 封装定义
* `EventPool`
SocketAsyncEventArgs 池
* `MessageFragment : IDentity`
套接字的收发包数据分片
* `EventToken`
SocketAsyncEventArgs 的套接字上下文
* `MessagePool`
网络数据池

###`处理流程`

通过使用`EventSocketer`派生的子类，并重载`CreateClientSocket`来获得一个重载自`ISocketer`的套接字实例以实现具体的网络操作，通过重载类似`Get***AsyncEvents`的相关函数来获得异步操作时需要使用到的`SocketAsyncEventArgs`，通过重载形如`Get***Buffer`的相关函数来获得缓存管理`BufferManager`类的实例，通过重载与`OnSended`相似的函数在事件发生后来处理`SocketAsyncEventArgs`对象，通过重载`Receive`和`Send`函数来自定义接收和发送数据的方式，通过重载`Reset***AsyncEvents`相似的函数在断开连接时来重置 SocketAsyncEventArgs 池。
* `连接`

`EventSocketer`的构造函数`EventSocketer(SocketConfigure sc)`调用它的无参构造函数来初始化事件注册对象和收发数据线程。`EventSocketer`及其子类使用`Connect`连接到指定的远程主机，并引发`Connecting`事件，在`Connect`函数中使用`CreateClientSocket()`得到的`ISocketer`实例`ClientSocket`，使用`GetConnectEventsPooler`函数得到一个`EventPool`实例，使用`ClientSocket`的`Connect`函数使用实际`Socket`套接字的异步连接方法连接到服务器。连接成功之后，在`OnConnected`函数中引发`AfterConnected`事件，并启动`Receive`和`Send`的收发线程，开始收发网线数据。
* `接收`

在连接成功之后，`EventSocketer`的私有成员`mbrWaitForDisconnect`会被置为网络状态，一般情况下为`true`。在`Receive`线程中，会使用`while`循环检查该状态值，为`true`时检查`ClientSocket`的`Available`值，该值大于 0 时，重置`lostCount`计数为1024，并调用`GetReceiveEventsPooler`函数得到`EventPool`池的实例，并使用`Pop(SocketConfigure config)`函数中获取用于进行异步接收的`SocketAsyncEventArgs`实例，在`OnReceived`函数中将，使用`SocketEventArgs`复制收到的网络数据，使用`MessagePool`的实例`IncommeMessage`的`PushMessage`将网络数据存入接收数据缓存，将性能计数项增加实际数字，并引发`Recevied`事件。异步接收完成之后，检查`ClientSocket.SocketUnAvailable`属性检查网络状态，在为`true`时，连接暂时不可用，对`lostCount`逐减，减到0时，连接丢失，调用`Disconnect`函数断开连接。
* `发送`

在连接成功之后，`EventSocketer`的私有成员`mbrWaitForDisconnect`会被置为网络状态，一般情况下为`true`。在`Send`线程中，会使用`while`循环检查该状态值，为`true`时调用`GetSendEventsPooler`函数得到`EventPool`池的实例，并使用`Pop(SocketConfigure config)`函数中获取用以异步发送的`SocketAsyncEventArgs`实例，使用`MessagePool`的实例`OutMessage`的`GetMessage`函数获取需要发送的数据，使用`ClientSocket`的`Send`异步发送，在发送成功之后，调用`OnSended`函数，进行发送后处理，并引发`Sended`事件，将性能计数项增加实际数字。
* `断开`

在收发数据的线程中，如果连接丢失，通过调用`Disconnect`函数，或直接调用该函数，将调用`ClientSocket`的`Disconnect`函数进行异步断开连接，并调用`MessagePool`的实例的`ForceClose`函数，取消队列等待信号，调用`ResetConnectAsyncEvents`、`ResetDisconnectAsyncEvents`、`ResetReceiveAsyncEvents`、`ResetSendAsyncEvents`取消`EventPool`池的等待信号。
* `错误处理`

在所有的与`OnSended`相似的函数处理中，会检查`SocketAsyncEventArgs`的`SocketError`值，不为`SocketError.Success`时，以该`SocketAsyncEventArgs`为参数构造`SocketErrorArgs`实例，并引发`Error`事件。如果`SocketConfigure`的实例的`OnErrorContinue`属性为`false`，后续处理将不会继续。

***
A .Net Async Eventabled **Server** or **Client** Socketer compmont.
##Event List:
###`Connectting`:
For begin connect to a remote host
###`Connected`:
For end of connect
###`Recevied`:
For recevie some network bytes
###`Sended`:
For send some bytes
###`Disconnected`:
For disconnectd
###`Error`:
For a network error
***
EOF
