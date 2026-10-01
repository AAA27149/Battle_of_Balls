using System;
using Google.Protobuf;
using UnityEngine;

/**
 * Title:
 * Description:
 */

public class NetErrorMsgMgr : Singleton<NetErrorMsgMgr>
{
    public void Init()
    {
        //注册错误消息码
        SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_ErrCode, OnErrorMsgHandle);
    }

    /// <summary>
    /// 处理错误消息
    /// </summary>
    /// <param name="data"></param>
    private void OnErrorMsgHandle(ByteString data)
    {
        ErrMsg msg = ErrMsg.Parser.ParseFrom(data);
        switch (msg.CmdCode)
        {
            case CmdCode.ServerError:
                Debug.Log("服务端错误");
                break;
            case CmdCode.RoomFull:
                Debug.Log("房间已满");
                break;
            default:
                Debug.Log("错误码::" + msg.CmdCode);
                break;
        }
    }
}
