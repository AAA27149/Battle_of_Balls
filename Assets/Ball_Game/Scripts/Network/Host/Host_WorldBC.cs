using System.Collections.Generic;
using Google.Protobuf;

/**
 * Title: 房间广播
 * Description: 对应原来的 Game_WorldBC。把进房结果发给房间里其它客户端
 */

public class Host_WorldBC : Singleton<Host_WorldBC>
{
    /// <summary>
    /// 有玩家进入后，同步给其它客户端
    /// </summary>
    public void PlayerEnterBC(Session currentSession, PlayerEnterRet ret)
    {
        //1. 获取所有连接
        Dictionary<int, Session> sessionDic = SessionMgr.Instance.GetSessionDic();

        //2. 循环所有连接，进行同步
        foreach (var item in sessionDic)
        {
            //3. 排除自己，自己走加入房间的回包
            if (item.Value.SessionId == currentSession.SessionId)
            {
                continue;
            }

            item.Value.SendData(NetDefine.CMD_PlayerEnterCode, ret.ToByteString());
        }
    }

    /// <summary>
    /// 把这一拍的人和球发给所有客户端
    /// </summary>
    public void SnapshotBC(WorldSnapshot snapshot)
    {
        Dictionary<int, Session> sessionDic = SessionMgr.Instance.GetSessionDic();
        foreach (var item in sessionDic)
        {
            item.Value.SendData(NetDefine.CMD_WorldSnapshotCode, snapshot.ToByteString());
        }
    }
}
