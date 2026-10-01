using System;
using System.Collections.Generic;
using System.Threading;

/**
 * Title: 房间里有哪些玩家
 * Description: 切场景后还要留着。主机是 1 号，后面按加入顺序递增
 */

public class BallRoomPlayerMgr : Singleton<BallRoomPlayerMgr>
{
    private List<int> _playerIds = new List<int>();
    private SynchronizationContext _syncContext;

    public int LocalPlayerId
    {
        get;
        private set;
    }

    public List<int> PlayerIds
    {
        get { return _playerIds; }
    }

    /// <summary>
    /// 有新玩家进入。游戏场景用来把对应胶囊显示出来
    /// </summary>
    public Action<int> OnPlayerEnter;

    /// <summary>
    /// 主机创建房间，自己是 1 号
    /// </summary>
    public void SetLocalHost()
    {
        _syncContext = SynchronizationContext.Current;
        LocalPlayerId = 1;
        _playerIds.Clear();
        _playerIds.Add(1);
    }

    /// <summary>
    /// 加入房间成功，记下自己和房间里已有的人
    /// </summary>
    public void SetPlayers(int selfId, IList<int> playerIds)
    {
        _syncContext = SynchronizationContext.Current;
        LocalPlayerId = selfId;
        _playerIds.Clear();
        for (int i = 0; i < playerIds.Count; i++)
        {
            _playerIds.Add(playerIds[i]);
        }
    }

    /// <summary>
    /// 房间里多了一个人
    /// </summary>
    public void AddPlayer(int playerId)
    {
        if (_playerIds.Contains(playerId))
        {
            return;
        }

        _playerIds.Add(playerId);

        //主机收包在子线程，显示胶囊要回到主线程
        if (OnPlayerEnter == null)
        {
            return;
        }

        if (_syncContext == null || SynchronizationContext.Current == _syncContext)
        {
            OnPlayerEnter(playerId);
            return;
        }

        _syncContext.Post(_ =>
        {
            OnPlayerEnter?.Invoke(playerId);
        }, null);
    }

    /// <summary>
    /// 编号对应的颜色
    /// </summary>
    public static string ColorName(int playerId)
    {
        switch (playerId)
        {
            case 1:
                return "红色";
            case 2:
                return "蓝色";
            case 3:
                return "绿色";
            case 4:
                return "紫色";
        }

        return "";
    }
}
