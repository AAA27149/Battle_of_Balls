using System.Net;
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

    /// <summary>
    /// 主机地址
    /// </summary>
    [SerializeField, Header("IP输入框")]
    private InputField _iptIp;

    /// <summary>
    /// 主机端口
    /// </summary>
    [SerializeField, Header("端口输入框")]
    private InputField _iptPort;

    /// <summary>
    /// 主机不存在提示，默认关掉
    /// </summary>
    [SerializeField, Header("主机不存在提示")]
    private GameObject _hostMissingPanel;

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
        if (_hostMissingPanel != null)
        {
            _hostMissingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void OnCreateRoomBtnClick()
    {
        string ip;
        int port;
        if (!TryReadAddress(out ip, out port))
        {
            return;
        }

        if (!_hostApp.StartHost(ip, port))
        {
            SetStatus("创建房间失败，请检查地址和端口是否可用");
            return;
        }

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

        //2. 按输入的地址和端口连接房间主机
        string ip;
        int port;
        if (!TryReadAddress(out ip, out port))
        {
            return;
        }

        SetStatus("正在连接 " + ip + ":" + port);
        NetSocketMgr.Instance.ConnectServer(ip, port, OnConnSucceed, OnConnFailed);
    }

    /// <summary>
    /// 读取界面上的地址和端口。不合法就提示，不继续
    /// </summary>
    private bool TryReadAddress(out string ip, out int port)
    {
        ip = _iptIp != null ? _iptIp.text.Trim() : NetDefine.IPHost;
        string portText = _iptPort != null ? _iptPort.text.Trim() : NetDefine.RoomPort.ToString();
        port = 0;
        if (ip.Length == 0 || !IPAddress.TryParse(ip, out _))
        {
            SetStatus("IP地址不正确");
            return false;
        }

        if (!int.TryParse(portText, out port) || port < 1 || port > 65535)
        {
            SetStatus("端口不正确");
            return false;
        }

        return true;
    }

    private void OnConnSucceed()
    {
        //连接回调在收包线程。和登录一样，连上就发请求
        _roomCtrl.JoinRoom();
    }

    private void OnConnFailed()
    {
        //已经在游戏里的加入方，连上的主机突然断了
        bool hostDown = _entered && _hostApp != null && !_hostApp.IsHost;
        if (NetSocketMgr.Client != null)
        {
            NetSocketMgr.Client._isNeedReconn = false;
        }

        _syncContext.Post(_ =>
        {
            if (hostDown)
            {
                GameRoom.NotifyHostDisconnected();
                return;
            }

            SetStatus("主机不存在");
            if (_hostMissingPanel != null)
            {
                _hostMissingPanel.SetActive(true);
            }
        }, null);
    }

    /// <summary>
    /// 关掉主机不存在提示
    /// </summary>
    public void OnHostMissingConfirm()
    {
        if (_hostMissingPanel != null)
        {
            _hostMissingPanel.SetActive(false);
        }
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
