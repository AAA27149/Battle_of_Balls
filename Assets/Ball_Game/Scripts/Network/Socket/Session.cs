
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.Protobuf;

public class Session : ServerBase //一个session 代表一个连接的客户端，只有作为服务端才会去记录session
{
    public int SessionId
    {
        get;
        set;
    }

    public int _roleId;
  
    
    public Session(Dictionary<int, IContainer> cmdDic,NetClient client)
    {
        _cmdDic=cmdDic;
        _client=client;
        SessionMgr.Instance.AddSession(this);
    }

   


    /// <summary>
    /// 开始接收客户端发来的数据
    /// </summary>
    /// <param name="socket"></param>
    public void ReceiveData(Socket socket)
    {
        _socket=socket;
        BeginReceive();
    }

    protected override void HandleCommand(BasePackage basePackage)
    {
        IContainer container = _cmdDic[basePackage.ProtoCode];
        if (container == null)
        {
            LogMsg.Info("Command not regist...");
            return;
        }

        //客户端直接连房间主机，没有网关转发，不再改写 UnitySessionId / GateSessionId
        container.OnServerCommand(this,basePackage);
        
    }

    public override void Disconnect()
    {
        if (_socket != null)
        {
            LogMsg.Info("Disconnect::"+_socket.RemoteEndPoint+" 断开了连接...");
        }
        //没有网关和数据库。断开后把这条 Session 从管理器里拿掉
        SessionMgr.Instance.RemoveSession(SessionId);
        
        base.Disconnect();
        
    }
}
