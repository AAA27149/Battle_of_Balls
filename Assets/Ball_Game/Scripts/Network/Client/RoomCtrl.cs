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
        _view.SetStatus("进入房间，玩家编号 " + ret.PlayerId);
    }

    /// <summary>
    /// 其它玩家进入
    /// </summary>
    private void OnPlayerEnterHandle(ByteString data)
    {
        PlayerEnterRet ret = PlayerEnterRet.Parser.ParseFrom(data);
        _view.SetStatus("玩家 " + ret.PlayerId + " 进入房间");
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
