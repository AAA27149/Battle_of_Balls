
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Google.Protobuf;

public class ServerBase
{
    //服务于Unity 
    public ClientType _clientType;
    public Action<int,ByteString> OnReceiveMsg;
    //指令集
    protected Dictionary<int,IContainer> _cmdDic =  new Dictionary<int, IContainer>();
    
    protected Socket _socket;
    protected byte[] _buffer=new byte[1024*4]; //子类一些共有的确实可以提取到父类中
    //连接状态
    protected ConnState _connState;
    //作为服务端时，需要拿到客户端的引用
    public NetClient _client;
    
    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data"></param>

    public void SendData(BasePackage basePackage,int protoCode=-1,ByteString data =null,bool isLog=false) //要先解析客户端发来的数据包，要先知道它是怎么发送的
    {
        try
        {
            if (protoCode != -1)
            {
                basePackage.ProtoCode=protoCode;
            }

            if (data != null)
            {
                basePackage.Data=data;
            }

            if (isLog)
            {
                LogMsg.Info($"{_socket.LocalEndPoint} 发送了数据::{basePackage.ToString()} => {_socket.RemoteEndPoint}");
            }
            
            //这玩意的socket 其实的连接这个服务器的客户端的socket
            _socket.Send(NetUtils.Instance.MakeData(basePackage.ToByteArray()));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        
       



    }
    
    /// <summary>
    /// 发送数据，不打印重载
    /// </summary>
    /// <param name="data"></param>

    public void SendDataNotLog(BasePackage basePackage,int protoCode=-1,ByteString data =null,bool isLog=false) //要先解析客户端发来的数据包，要先知道它是怎么发送的
    {
        try
        {
            if (protoCode != -1)
            {
                basePackage.ProtoCode=protoCode;
            }

            if (data != null)
            {
                basePackage.Data=data;
            }

            if (isLog)
            {
                LogMsg.Info($"{_socket.LocalEndPoint} 发送了数据::{basePackage.ToString()} => {_socket.RemoteEndPoint}");
            }
            
            //这玩意的socket 其实的连接这个服务器的客户端的socket
            _socket.Send(NetUtils.Instance.MakeData(basePackage.ToByteArray()));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
        
       



    }
    


    public void SendData(int protoCode=-1,ByteString data =null)
    {
        BasePackage basePackage = new BasePackage();
        SendData(basePackage,protoCode,data);
    }

    public void SendData(int unitySessionId, int protoCode=-1,ByteString data =null,bool isLog=true)
    {
        BasePackage basePackage = new BasePackage();
        basePackage.UnitySessionId=unitySessionId;
        SendData(basePackage,protoCode,data,isLog);
    }
    
    public void SendError(BasePackage basePackage, CmdCode cmdCode)
    {
        ErrMsg errMsg = new ErrMsg()
        {
            CmdCode = cmdCode,
        };
        SendData(basePackage,NetDefine.CMD_ErrCode, errMsg.ToByteString());
    }
    
        
    
    
    /// <summary>
    ///  //开始接收服务端发来的数据
    /// </summary>
    protected void BeginReceive()
    {
        _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceiveCB, null);
    }
    
    /// <summary>
    /// 处理服务端发来的数据
    /// </summary>
    /// <param name="ar"></param>
    private void OnReceiveCB(IAsyncResult ar)
    {
        try
        {
            //返回的字节数
            int len =  _socket.EndReceive(ar);

            if (len > 0)
            {
                while (true) //消息头（消息体的长度）+消息体（判断是否压缩，crc值，数据）
                {
                
                    //消息头记录的消息体长度
                    ushort msgLen = BitConverter.ToUInt16(_buffer, 0);  
                    if (len>=msgLen+2) //其实也是硬拼的，写代码没那么难
                    {
                        //拿到了客户端发来的 经过解析后的数据
                        byte[] data= NetUtils.Instance.ParseData(_buffer, msgLen); //就是封装方便调用，简化代码
                        if (data != null)
                        {
                            //拿到数据反序列化数据
                            BasePackage basePackage= BasePackage.Parser.ParseFrom(data); //数据的解包
                            //Console.WriteLine("basePackage::"+ basePackage.ToString());
                            //输入和快照每帧都有，不打印，避免刷屏
                            if (basePackage.ProtoCode != NetDefine.CMD_PlayerInputCode &&
                                basePackage.ProtoCode != NetDefine.CMD_WorldSnapshotCode)
                            {
                                LogMsg.Info($"{_socket.LocalEndPoint} 接收  basePackage::{basePackage.ToString()} <= {_socket.RemoteEndPoint}");
                                
                            }
                            
                            HandleCommand(basePackage);
                            
                        }
                        len-=(msgLen+2);
                        //如果-完，len还是大于0，说明发生了粘包
                        if (len > 0)
                        {
                            Buffer.BlockCopy(_buffer, msgLen+2, _buffer, 0, len);
                        }

                    }
                    else
                    {
                        break;
                    }
                }
                BeginReceive();
            }
            else
            {
                Disconnect();
            }
            
        }
        catch (Exception e)
        {
            Disconnect();
            LogMsg.Info(e.Message);
            
        }
    }

    protected virtual void HandleCommand(BasePackage basePackage)
    {
        
    }

    public virtual void Disconnect()
    {
        _connState = ConnState.Disconnected;
        if (_socket != null)
        {
            _socket.Close();
            _socket=null;
        }
    }
    
    
    
}
