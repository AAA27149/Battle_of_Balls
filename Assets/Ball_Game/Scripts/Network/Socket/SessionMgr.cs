
using System.Collections.Generic;
using System.Threading;

public class SessionMgr : Singleton<SessionMgr> //管理所有用户的连接
{
    private int _instanceInten;
    
    private Dictionary<int,Session> _sessionDic=new Dictionary<int, Session>();
    
    
    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="session"></param>
    /// <param name="sessionId"></param>
    public void AddSession( Session session ,int sessionId =-1)
    {
        if (sessionId <= 0)
        {
            sessionId = GetInstanceInter();
        }
        
        if (!_sessionDic.ContainsKey(sessionId))
        {
            session.SessionId = sessionId;
            _sessionDic.Add(sessionId, session);
        }
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="sessionId"></param>
    public void RemoveSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            _sessionDic.Remove(sessionId);
        }
    }

    /// <summary>
    /// 根据id获取session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    public Session GetSession(int sessionId)
    {
        if (_sessionDic.ContainsKey(sessionId))
        {
            return _sessionDic[sessionId];
        }

        return null;
    }

    /// <summary>
    /// 封装好的api ，返回所有的Session连接
    /// </summary>
    /// <returns></returns>
    public int GetSessionCount()
    {
        return _sessionDic.Count;
    }

    /// <summary>
    /// 返回所有连接，广播时用
    /// </summary>
    /// <returns></returns>
    public Dictionary<int, Session> GetSessionDic()
    {
        return _sessionDic;
    }
    
    public int GetInstanceInter()
    {
        return Interlocked.Increment(ref _instanceInten);
    }
}
