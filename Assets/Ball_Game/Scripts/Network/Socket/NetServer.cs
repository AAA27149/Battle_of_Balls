
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class NetServer //shift+tab 左对齐
{
    private Socket _socket;
    private bool _isClose;
    private Dictionary<int,IContainer> _cmdDic =  new Dictionary<int,IContainer>();
    private NetClient _client;
    public NetServer(NetClient client)
    {
        _client = client;
    }
    
    public void StartServer(string ip, int port)
    {
        //创建Socket                  Ipv4           确保数据准确无误的流传输            采用TCP协议通信
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //设置ip和端口号 
        EndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        //绑定ip和端口
        _socket.Bind(endPoint);
        
        //以上就是把服务端开起来了。这里没有中心服、登录服、网关，只有房间主机
        LogMsg.Info("房间主机开启成功: " + endPoint.ToString());
        
        //设置最大的Socket最大连接数，这些应该在配置文件里配置
        _socket.Listen(100);  //使这个socket 为 监听套接字
        //先挂上接收，再返回。避免客户端在这一瞬间连接被拒绝
        ListenConnectSocket();
    }

    /// <summary>
    /// 关掉监听，并断开已经连上来的客户端，对方才能立刻发现主机没了
    /// </summary>
    public void Close()
    {
        _isClose = true;
        List<Session> sessions = new List<Session>();
        foreach (KeyValuePair<int, Session> item in SessionMgr.Instance.GetSessionDic())
        {
            sessions.Add(item.Value);
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            sessions[i].CloseWithoutNotice();
        }

        if (_socket != null)
        {
            _socket.Close();
            _socket = null;
        }
    }

    /// <summary>
    /// 监听客户端连接
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void ListenConnectSocket()
    {
        if (_isClose || _socket == null)
        {
            return;
        }
        _socket.BeginAccept(ClientConnectB, null);
    }

    /// <summary>
    /// 有客户端连接，处理连接后的逻辑
    /// </summary>
    /// <param name="ar"></param>
    private void ClientConnectB(IAsyncResult ar)
    {
        //服务端的未知错误特别多，所以要用try-catch
        try
        {
            Socket clientSocket=  _socket.EndAccept(ar);
            LogMsg.Info($"客户端::{clientSocket.RemoteEndPoint} 连接成功...");
            //开始接收客户端数据 这个Session就拿到了NetServer的指令集，以及对应的客户端client
            //这一步操作就是拿到login服务端的 _cmdDIc,_clident 和 SessionId
            Session session = new Session(_cmdDic,_client); //用一个Session 去记录一个客户端的Socket
            session.ReceiveData(clientSocket); //每一个客户端socket 配一个Session去处理
            //clientSocket.Receive();
        
            //当处理完成当前客户端连接后，继续处理下一个客户端的连接请求
            ListenConnectSocket(); //形成循环监听客户端 连接请求的闭环，并且是异步的
        }
        catch (Exception e)
        {
            //主动 Close 时，正在 Accept 的线程会进来，这里直接结束
            if (_isClose)
            {
                return;
            }
            Console.WriteLine(e.Message);
        }
        
        
        
        

    }

    public void RegistCommand(int cmd,IContainer container)
    {
        _cmdDic.Add(cmd,container);
    }
}
