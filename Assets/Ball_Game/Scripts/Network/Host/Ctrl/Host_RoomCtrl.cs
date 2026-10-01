using System.Collections.Generic;
using Google.Protobuf;

/**
 * Title: 房间主机指令处理
 * Description: 对应原来的 Game_RoleCtrl。处理加入房间
 */

public class Host_RoomCtrl : IContainer
{
    //主机自己是 1 号，后面进来的从 2 开始
    private int _playerId = 1;

    public void OnInit()
    {
    }

    /// <summary>
    /// 房间主机作为服务端，接收其它客户端的数据
    /// </summary>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_JoinRoomCode: //还是注册了对应的指令集才能接收到消息
                OnJoinRoomHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerInputCode:
                OnPlayerInputHandle(serverBase, basePackage);
                break;
        }
    }

    /// <summary>
    /// 本进程不去连接别的服务器，这个接口留空
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
    }

    /// <summary>
    /// 加入房间请求
    /// </summary>
    private void OnJoinRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        JoinRoomReq req = JoinRoomReq.Parser.ParseFrom(basePackage.Data);
        LogMsg.Info("获取加入房间请求 OnJoinRoomHandle::" + req.ToString());

        //1. 算上主机最多 4 人。当前这条连接已经在 Session 里
        if (SessionMgr.Instance.GetSessionCount() > 3)
        {
            serverBase.SendError(basePackage, CmdCode.RoomFull);
            return;
        }

        //2. 分配玩家编号，并记到房间名单里
        _playerId++;
        Session session = (Session)serverBase;
        session._roleId = _playerId;
        BallRoomPlayerMgr.Instance.AddPlayer(_playerId);

        JoinRoomRet ret = new JoinRoomRet()
        {
            PlayerId = _playerId,
        };
        List<int> playerIds = BallRoomPlayerMgr.Instance.PlayerIds;
        for (int i = 0; i < playerIds.Count; i++)
        {
            ret.PlayerIds.Add(playerIds[i]);
        }
        serverBase.SendData(basePackage, NetDefine.CMD_JoinRoomCode, ret.ToByteString());

        //3. 把这个玩家同步给房间里其它客户端
        PlayerEnterRet enterRet = new PlayerEnterRet()
        {
            PlayerId = _playerId,
        };
        Host_WorldBC.Instance.PlayerEnterBC(session, enterRet);
    }

    /// <summary>
    /// 玩家输入请求
    /// </summary>
    private void OnPlayerInputHandle(ServerBase serverBase, BasePackage basePackage)
    {
        PlayerInputReq req = PlayerInputReq.Parser.ParseFrom(basePackage.Data);
        Host_InputBuf.Instance.Set(req.PlayerId, req.MoveX, req.MoveZ, req.Yaw);
    }
}
