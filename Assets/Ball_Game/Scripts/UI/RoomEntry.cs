using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private RoomCtrl _roomCtrl;
    private bool _entered;

    private void Awake()
    {
        _syncContext = SynchronizationContext.Current;
        //先注册进房回包，再 Init。错误码只能注册一次，这里先占上
        _roomCtrl = new RoomCtrl(this);
        NetSocketMgr.Instance.Init();
        _hostApp = GetComponent<HostApp>();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void OnCreateRoomBtnClick()
    {
        _hostApp.StartHost();
        //主机是 1 号红色，马上进入游戏场景
        BallRoomPlayerMgr.Instance.SetLocalHost();
        EnterGame();
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public void OnJoinRoomBtnClick()
    {
        //1. 本窗口已经是主机，就不要再连自己
        if (_hostApp.IsHost)
        {
            SetStatus("本窗口已经是主机，请另开一个客户端再加入");
            return;
        }

        //2. 连接房间主机
        SetStatus("正在连接 " + NetDefine.IPHost + ":" + NetDefine.RoomPort);
        NetSocketMgr.Instance.ConnectServer(NetDefine.IPHost, NetDefine.RoomPort, OnConnSucceed, OnConnFailed);
    }

    private void OnConnSucceed()
    {
        //连接回调在收包线程。和登录一样，连上就发请求
        _roomCtrl.JoinRoom();
    }

    private void OnConnFailed()
    {
        _syncContext.Post(_ => { SetStatus("连接房间主机失败"); }, null);
    }

    public void SetStatus(string text)
    {
        if (_txtStatus != null)
        {
            _txtStatus.text = text;
        }
    }

    /// <summary>
    /// 进入游戏场景。网络物体留下来，否则主机端口会断
    /// </summary>
    public void EnterGame()
    {
        if (_entered)
        {
            return;
        }

        _entered = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(NetDefine.GameScene);
    }
}
