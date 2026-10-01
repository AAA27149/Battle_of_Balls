using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/**
 * Title:
 * Description:
 */

public delegate void OnActionHandler(ByteString data);

public class SocketDispatch : Singleton<SocketDispatch> //把从服务端收到的数据派发到注册或者需要的地方去
{
    private Dictionary<int, OnActionHandler> _actionDic = new Dictionary<int, OnActionHandler>();

    /// <summary>
    /// 注册事件
    /// </summary>
    /// <param name="protoCode"></param>
    /// <param name="handle"></param>
    public void AddEventHandle(int protoCode, OnActionHandler handle)
    {
        if (!_actionDic.ContainsKey(protoCode) && handle != null)
        {
            _actionDic.Add(protoCode, handle);
        }
    }

    /// <summary>
    /// 删除事件
    /// </summary>
    /// <param name="protoCode"></param>
    public void RemoveEventHandle(int protoCode)
    {
        if (_actionDic.ContainsKey(protoCode))
        {
            _actionDic.Remove(protoCode);
        }
    }

    /// <summary>
    /// 广播，事件系统三套，注册，取消注册，广播
    /// </summary>
    /// <param name="protoCode"></param>
    /// <param name="data"></param>
    public void DispatcherEvent(int protoCode, ByteString data)
    {
        if (_actionDic.ContainsKey(protoCode))
        {
            _actionDic[protoCode]?.Invoke(data);
        }
    }
}
