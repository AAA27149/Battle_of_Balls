using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/**
 * Title: 房间入口
 * Description: 创建房间、加入房间。界面在场景里摆好，这里只控制按钮和文字
 */

public class RoomEntry : MonoBehaviour
{
    /// <summary>
    /// 状态文本
    /// </summary>
    [SerializeField, Header("状态文本")]
    private Text _txtStatus;

    private HostApp _hostApp;
    private SynchronizationContext _syncContext;

    private void Awake()
    {
        _syncContext = SynchronizationContext.Current;
        NetSocketMgr.Instance.Init();
        _hostApp = GetComponent<HostApp>();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void OnCreateRoomBtnClick()
    {
        _hostApp.StartHost();
        _txtStatus.text = "已创建房间，等待其它客户端加入  " + NetDefine.IPHost + ":" + NetDefine.RoomPort;
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public void OnJoinRoomBtnClick()
    {
        //1. 本窗口已经是主机，就不要再连自己
        if (_hostApp.IsHost)
        {
            _txtStatus.text = "本窗口已经是主机，请另开一个客户端再加入";
            return;
        }

        //2. 连接房间主机
        _txtStatus.text = "正在连接 " + NetDefine.IPHost + ":" + NetDefine.RoomPort;
        NetSocketMgr.Instance.ConnectServer(NetDefine.IPHost, NetDefine.RoomPort, OnConnSucceed, OnConnFailed);
    }

    private void OnConnSucceed()
    {
        //连接回调在收包线程，文字要丢回主线程
        _syncContext.Post(_ => { _txtStatus.text = "已连接房间主机"; }, null);
    }

    private void OnConnFailed()
    {
        _syncContext.Post(_ => { _txtStatus.text = "连接房间主机失败"; }, null);
    }
}
