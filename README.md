# AsyncSocketer
AsyncSocketer �ǶԱ�׼�׽��ֵ����¼������ķ�װ���޸���CodeProject�й��ڸ������첽Socket�����£�[C# SocketAsyncEventArgs High Performance Socket Code](http://www.codeproject.com/Articles/83102/C-SocketAsyncEventArgs-High-Performance-Socket-Cod) �������µ���ҪĿ��������MSDN��[�����첽Socket���](http://msdn.microsoft.com/zh-tw/library/vstudio/system.net.sockets.socketasynceventargs(v=vs.90).aspx)�е�ʾ����Ŀ�����ĵ�˼·���ṩһ����������� BufferManager ���ڽ��պͷ����������ݣ��ṩһ�������׽��������ĵ�UserToken���Լ��ⶨ�շ����ݵ�Э�鶨�弰������

����Ŀ�Ǽ̳������л��������׽��������ģ������׽��ֵ����ӡ����ա����͡��Ͽ�ʱ�����¼�֪ͨ����ʽ�����׽����й��ڽ��պͷ��͵�ֱ�ӵ��á�

����Ŀ�ṩ��������ʵ����ĿĿ�ģ�

###`�¼�ί��`
* `SocketEventHandler`
�׽����¼�ί��
* `ServerSocketEventHandler`
������׽����¼�ί��
* `SocketErrorHandler`
�׽��ִ����¼�ί��
* `SocketPerformanceHandler`
�׽������ܼ����¼�ί��

###`�ӿ�`
* `IDentity`
��ʶ����

###`��`
* `SocketConfigure`
AsyncSocketer �������ö���
* `ISocketer`
��׼�׽��ַ�װ
* `TcpSocketer : ISocketer`
ʹ�� TCP Э����׽���
* `UdpSocketer : ISocketer`
ʹ�� UDP Э����׽���
* `PerformanceBase : Component`
���ܼ�������������
* `EventSocketer : PerformanceBase`
�첽�׽��֣���׼�׽��ֵ��¼�������װ
* `PartialSocket : EventSocketer`
ʹ�ö�� SocketAsyncEventArgs �غͻ���طֿ������¼����첽�׽���
* `PartialSocketServer : PartialSocket`
�ֿ������¼��ķ�����첽�׽���
* `MixedSocket : EventSocketer`
ʹ��ͬһ�� SocketAsyncEventArgs �غͻ���طֿ������¼����첽�׽���
* `BufferManager`
������
* `SocketEventArgs : EventArgs`
�׽����¼�����
* `ServerSocketEventArgs : SocketEventArgs`
������׽����¼�����
* `SocketErrorArgs : EventArgs`
�׽��ִ����¼�����
* `PerformanceCountArgs : EventArgs`
�׽������ܼ����¼�����
* `ServerPerformanceCountArgs : PerformanceCountArgs`
������׽������ܼ����¼�����
* `Pooler<TEArtType> where TEArtType : IDentity`
��������
* `EventArgObject : IDentity`
SocketAsyncEventArgs �� IDentity ��װ����
* `EventPool`
SocketAsyncEventArgs ��
* `MessageFragment : IDentity`
�׽��ֵ��շ������ݷ�Ƭ
* `EventToken`
SocketAsyncEventArgs ���׽���������
* `MessagePool`
�������ݳ�

###`��������`

ͨ��ʹ��`EventSocketer`���������࣬������`CreateClientSocket`�����һ��������`ISocketer`���׽���ʵ����ʵ�־�������������ͨ����������`Get***AsyncEvents`����غ���������첽����ʱ��Ҫʹ�õ���`SocketAsyncEventArgs`��ͨ����������`Get***Buffer`����غ�������û������`BufferManager`���ʵ����ͨ��������`OnSended`���Ƶĺ������¼�������������`SocketAsyncEventArgs`����ͨ������`Receive`��`Send`�������Զ�����պͷ������ݵķ�ʽ��ͨ������`Reset***AsyncEvents`���Ƶĺ����ڶϿ�����ʱ������ SocketAsyncEventArgs �ء�
* `����`

`EventSocketer`�Ĺ��캯��`EventSocketer(SocketConfigure sc)`���������޲ι��캯������ʼ���¼�ע�������շ������̡߳�`EventSocketer`��������ʹ��`Connect`���ӵ�ָ����Զ��������������`Connecting`�¼�����`Connect`������ʹ��`CreateClientSocket()`�õ���`ISocketer`ʵ��`ClientSocket`��ʹ��`GetConnectEventsPooler`�����õ�һ��`EventPool`ʵ����ʹ��`ClientSocket`��`Connect`����ʹ��ʵ��`Socket`�׽��ֵ��첽���ӷ������ӵ������������ӳɹ�֮����`OnConnected`����������`AfterConnected`�¼���������`Receive`��`Send`���շ��̣߳���ʼ�շ��������ݡ�
* `����`

�����ӳɹ�֮��`EventSocketer`��˽�г�Ա`mbrWaitForDisconnect`�ᱻ��Ϊ����״̬��һ�������Ϊ`true`����`Receive`�߳��У���ʹ��`while`ѭ������״ֵ̬��Ϊ`true`ʱ���`ClientSocket`��`Available`ֵ����ֵ���� 0 ʱ������`lostCount`����Ϊ1024��������`GetReceiveEventsPooler`�����õ�`EventPool`�ص�ʵ������ʹ��`Pop(SocketConfigure config)`�����л�ȡ���ڽ����첽���յ�`SocketAsyncEventArgs`ʵ������`OnReceived`�����н���ʹ��`SocketEventArgs`�����յ����������ݣ�ʹ��`MessagePool`��ʵ��`IncommeMessage`��`PushMessage`���������ݴ���������ݻ��棬�����ܼ���������ʵ�����֣�������`Recevied`�¼����첽�������֮�󣬼��`ClientSocket.SocketUnAvailable`���Լ������״̬����Ϊ`true`ʱ��������ʱ�����ã���`lostCount`���������0ʱ�����Ӷ�ʧ������`Disconnect`�����Ͽ����ӡ�
* `����`

�����ӳɹ�֮��`EventSocketer`��˽�г�Ա`mbrWaitForDisconnect`�ᱻ��Ϊ����״̬��һ�������Ϊ`true`����`Send`�߳��У���ʹ��`while`ѭ������״ֵ̬��Ϊ`true`ʱ����`GetSendEventsPooler`�����õ�`EventPool`�ص�ʵ������ʹ��`Pop(SocketConfigure config)`�����л�ȡ�����첽���͵�`SocketAsyncEventArgs`ʵ����ʹ��`MessagePool`��ʵ��`OutMessage`��`GetMessage`������ȡ��Ҫ���͵����ݣ�ʹ��`ClientSocket`��`Send`�첽���ͣ��ڷ��ͳɹ�֮�󣬵���`OnSended`���������з��ͺ���������`Sended`�¼��������ܼ���������ʵ�����֡�
* `�Ͽ�`

���շ����ݵ��߳��У�������Ӷ�ʧ��ͨ������`Disconnect`��������ֱ�ӵ��øú�����������`ClientSocket`��`Disconnect`���������첽�Ͽ����ӣ�������`MessagePool`��ʵ����`ForceClose`������ȡ�����еȴ��źţ�����`ResetConnectAsyncEvents`��`ResetDisconnectAsyncEvents`��`ResetReceiveAsyncEvents`��`ResetSendAsyncEvents`ȡ��`EventPool`�صĵȴ��źš�
* `������`

�����е���`OnSended`���Ƶĺ��������У�����`SocketAsyncEventArgs`��`SocketError`ֵ����Ϊ`SocketError.Success`ʱ���Ը�`SocketAsyncEventArgs`Ϊ��������`SocketErrorArgs`ʵ����������`Error`�¼������`SocketConfigure`��ʵ����`OnErrorContinue`����Ϊ`false`�������������������

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
