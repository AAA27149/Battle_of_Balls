using Google.Protobuf;
using UnityEngine;

/**
 * Title: 房间控制器
 * Description: 对应原来的 LoginCtrl。连接成功后发进房请求，处理进房回包
 */

public class RoomCtrl
{
    private RoomEntry _view;

    public RoomCtrl(RoomEntry view)
    {
        _view = view;
        //在哪里需要数据的时候去监听 请求结果
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_JoinRoomCode, OnJoinRoomHandle);
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_PlayerEnterCode, OnPlayerEnterHandle);
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_PlayerExitCode, OnPlayerExitHandle);
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_TappedCode, OnTappedHandle);
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_ErrCode, OnErrHandle);
    }

    /// <summary>
    /// 连接成功后请求加入房间
    /// </summary>
    public void JoinRoom()
    {
        JoinRoomReq req = new JoinRoomReq();
        NetSocketMgr.Client.SendData(NetDefine.CMD_JoinRoomCode, req.ToByteString());
    }

    /// <summary>
    /// 加入房间返回
    /// </summary>
    private void OnJoinRoomHandle(ByteString data)
    {
        JoinRoomRet ret = JoinRoomRet.Parser.ParseFrom(data);
        //记下自己和已经在房间里的人，再进入游戏场景
        BallRoomPlayerMgr.Instance.SetPlayers(ret.PlayerId, ret.PlayerIds);
        _view.EnterGame();
    }

    /// <summary>
    /// 其它玩家进入
    /// </summary>
    private void OnPlayerEnterHandle(ByteString data)
    {
        PlayerEnterRet ret = PlayerEnterRet.Parser.ParseFrom(data);
        BallRoomPlayerMgr.Instance.AddPlayer(ret.PlayerId);
    }

    /// <summary>
    /// 其它玩家退出
    /// </summary>
    private void OnPlayerExitHandle(ByteString data)
    {
        PlayerExitRet ret = PlayerExitRet.Parser.ParseFrom(data);
        BallRoomPlayerMgr.Instance.RemovePlayer(ret.PlayerId);
    }

    /// <summary>
    /// 碰到球
    /// </summary>
    private void OnTappedHandle(ByteString data)
    {
        if (GameRoom.Instance != null)
        {
            GameRoom.Instance.ShowTapped();
        }
    }

    /// <summary>
    /// 错误码
    /// </summary>
    private void OnErrHandle(ByteString data)
    {
        ErrMsg msg = ErrMsg.Parser.ParseFrom(data);
        if (msg.CmdCode == CmdCode.RoomFull)
        {
            _view.SetStatus("房间已满");
            return;
        }

        Debug.Log("错误码::" + msg.CmdCode);
    }
}
