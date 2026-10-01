using System;
using System.Threading;
using Google.Protobuf;
using UnityEngine;

/**
 * Title: 网络模块管理类
 * Description:
 */

public class NetSocketMgr : Singleton<NetSocketMgr>
{
    private SynchronizationContext syncContext;
    private static NetClient _client;

    public static NetClient Client
    {
        get
        {
            return _client;
        }
    }

    public void Init()
    {
        syncContext = SynchronizationContext.Current; //把子线程切换回主线程
        //加入房间时再 ConnectServer，这里不主动连
        NetErrorMsgMgr.Instance.Init();
    }

    /// <summary>
    /// 开始连接服务端
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    public void ConnectServer(string ip, int port, Action connSucceed = null, Action connFailed = null)
    {
        Disconnect(); //确保client 没有连接服务端
        _client = new NetClient(ip, port, ClientType.Unity);
        _client.OnReceiveMsg += OnReceiveMsgHandle;
        if (connSucceed != null)
        {
            _client.OnConnSucceed = connSucceed;
        }

        if (connFailed != null)
        {
            _client.OnConnFailed = connFailed;
        }
        _client.StartConnect();
    }

    /// <summary>
    /// 收到服务端发来的数据
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="arg2"></param>
    private void OnReceiveMsgHandle(int protoCode, ByteString data)
    {
        syncContext.Post(_ =>
        {
            //哪里需要该数据，就去哪里注册
            //这里面的内容会丢到主线程去跑
            SocketDispatch.Instance.DispatcherEvent(protoCode, data);
        }, null);
    }

    public void Disconnect()
    {
        if (_client != null)
        {
            _client._isNeedReconn = false;
            _client.Disconnect();
            _client = null;
        }
    }
}
