
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

    private bool _hasExit;

    public override void Disconnect()
    {
        if (_hasExit)
        {
            base.Disconnect();
            return;
        }

        _hasExit = true;
        if (_socket != null)
        {
            LogMsg.Info("Disconnect::"+_socket.RemoteEndPoint+" 断开了连接...");
        }

        //1. 已经进房的玩家退出，先通知其它人，再从名单里去掉
        if (_roleId > 1)
        {
            PlayerExitRet ret = new PlayerExitRet()
            {
                PlayerId = _roleId,
            };
            Host_WorldBC.Instance.PlayerExitBC(this, ret);
            BallRoomPlayerMgr.Instance.RemovePlayer(_roleId);
        }

        //2. 没有网关和数据库。断开后把这条 Session 从管理器里拿掉
        SessionMgr.Instance.RemoveSession(SessionId);
        base.Disconnect();
    }

    /// <summary>
    /// 主机关闭时只断开这条连接，不按普通玩家退出通知
    /// </summary>
    public void CloseWithoutNotice()
    {
        _hasExit = true;
        SessionMgr.Instance.RemoveSession(SessionId);
        base.Disconnect();
    }
}
