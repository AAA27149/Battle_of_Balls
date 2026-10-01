using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Title: 游戏房间
 * Description: 按玩家编号显示胶囊，并提示自己是什么颜色。加入方按主机快照显示
 */

public class GameRoom : MonoBehaviour
{
    /// <summary>
    /// 1号，红色，主机
    /// </summary>
    [SerializeField, Header("1号红色")]
    private GameObject _player1;

    /// <summary>
    /// 2号，蓝色
    /// </summary>
    [SerializeField, Header("2号蓝色")]
    private GameObject _player2;

    /// <summary>
    /// 3号，绿色
    /// </summary>
    [SerializeField, Header("3号绿色")]
    private GameObject _player3;

    /// <summary>
    /// 4号，紫色
    /// </summary>
    [SerializeField, Header("4号紫色")]
    private GameObject _player4;

    /// <summary>
    /// 颜色提示
    /// </summary>
    [SerializeField, Header("颜色提示")]
    private Text _txtColor;

    /// <summary>
    /// 主机断开提示，默认关掉
    /// </summary>
    [SerializeField, Header("主机断开提示")]
    private GameObject _hostDownPanel;

    public static GameRoom Instance;

    private GameObject[] _players;
    private bool _isHost;
    private PlayerInputCtrl _input;
    private Rigidbody _ball;
    private Camera _camera;
    private bool _hasNet;
    private Dictionary<int, BallView> _ballViews = new Dictionary<int, BallView>();
    private string _colorText;
    private float _tappedUntil;
    private Vector3[] _playerTargetPos;
    private float[] _playerTargetYaw;
    private const float PositionLerpSpeed = 12f;
    private const float RotationLerpSpeed = 15f;

    private void Awake()
    {
        _players = new GameObject[] { null, _player1, _player2, _player3, _player4 };
        Instance = this;
        if (_hostDownPanel != null)
        {
            _hostDownPanel.SetActive(false);
        }
        _playerTargetPos = new Vector3[5];
        _playerTargetYaw = new float[5];
        _input = gameObject.AddComponent<PlayerInputCtrl>();
        _camera = Camera.main;
        _ball = GameObject.Find("Ball").GetComponent<Rigidbody>();
        BallView firstBall = new BallView();
        firstBall.Transform = _ball.transform;
        firstBall.TargetPos = _ball.transform.position;
        firstBall.TargetRot = _ball.transform.rotation;
        firstBall.Placed = true;
        _ballViews[1] = firstBall;

        HideAll();

        //1. 把已经在房间里的人显示出来
        List<int> playerIds = BallRoomPlayerMgr.Instance.PlayerIds;
        for (int i = 0; i < playerIds.Count; i++)
        {
            ShowPlayer(playerIds[i]);
        }

        //2. 告诉玩家自己的编号和颜色。1号是主机
        int selfId = BallRoomPlayerMgr.Instance.LocalPlayerId;
        if (selfId == 1)
        {
            _txtColor.text = "你是1号红色主机玩家";
        }
        else
        {
            _txtColor.text = "你是" + selfId + "号" + BallRoomPlayerMgr.ColorName(selfId) + "玩家";
        }
        _colorText = _txtColor.text;

        //3. 之后再有人进来，再显示对应胶囊
        BallRoomPlayerMgr.Instance.OnPlayerEnter += ShowPlayer;
        BallRoomPlayerMgr.Instance.OnPlayerExit += HidePlayer;

        HostApp hostApp = FindAnyObjectByType<HostApp>();
        _isHost = hostApp != null && hostApp.IsHost;
        if (_isHost)
        {
            Host_World world = gameObject.AddComponent<Host_World>();
            world.Init(_player1, _player2, _player3, _player4, _ball, _input);
        }
        else
        {
            //球的物理只在主机上跑。加入方只改显示，关掉刚体插值，避免和快照打架
            _ball.isKinematic = true;
            _ball.useGravity = false;
            _ball.interpolation = RigidbodyInterpolation.None;
            SocketDispatch.Instance.AddEventHandle(NetDefine.CMD_WorldSnapshotCode, OnSnapshot);
        }
    }

    private void OnDestroy()
    {
        BallRoomPlayerMgr.Instance.OnPlayerEnter -= ShowPlayer;
        BallRoomPlayerMgr.Instance.OnPlayerExit -= HidePlayer;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void FixedUpdate()
    {
        //加入方只发输入。人的位置和球都等主机快照
        if (_isHost || _input == null || NetSocketMgr.Client == null)
        {
            return;
        }

        PlayerInputReq req = new PlayerInputReq()
        {
            PlayerId = BallRoomPlayerMgr.Instance.LocalPlayerId,
            MoveX = _input.MoveX,
            MoveZ = _input.MoveZ,
            Yaw = _input.Yaw,
        };
        NetSocketMgr.Client.SendData(NetDefine.CMD_PlayerInputCode, req.ToByteString());
    }

    private void Update()
    {
        if (_tappedUntil > 0f && Time.time >= _tappedUntil)
        {
            _tappedUntil = 0f;
            if (_txtColor != null)
            {
                _txtColor.text = _colorText;
            }
        }

        if (_isHost)
        {
            return;
        }

        FollowSnapshot();
    }

    private void LateUpdate()
    {
        LookFromSelf();
    }

    /// <summary>
    /// 收到主机结果，只记下目标位置和旋转。写法对齐参考项目的 SetNetworkState
    /// </summary>
    private void OnSnapshot(ByteString data)
    {
        WorldSnapshot snapshot = WorldSnapshot.Parser.ParseFrom(data);
        if (snapshot.Balls.Count > 0)
        {
            for (int i = 0; i < snapshot.Balls.Count; i++)
            {
                SetBallTarget(snapshot.Balls[i]);
            }
        }
        else if (snapshot.Ball != null)
        {
            SetBallTarget(snapshot.Ball);
        }

        for (int i = 0; i < snapshot.Players.Count; i++)
        {
            PlayerState state = snapshot.Players[i];
            if (state.PlayerId <= 0 || state.PlayerId > 4)
            {
                continue;
            }

            _playerTargetPos[state.PlayerId] = new Vector3(state.PosX, state.PosY, state.PosZ);
            _playerTargetYaw[state.PlayerId] = state.Yaw;
            if (!_hasNet)
            {
                GameObject player = GetPlayer(state.PlayerId);
                if (player != null)
                {
                    player.transform.position = _playerTargetPos[state.PlayerId];
                    player.transform.rotation = Quaternion.Euler(0f, state.Yaw, 0f);
                }
            }
        }

        _hasNet = true;
    }

    /// <summary>
    /// 加入方每帧往目标靠一点。系数和参考项目一样，12 和 15
    /// </summary>
    private void FollowSnapshot()
    {
        if (!_hasNet || _ballViews.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<int, BallView> item in _ballViews)
        {
            BallView view = item.Value;
            if (view.Transform == null)
            {
                continue;
            }

            view.Transform.position = Vector3.Lerp(
                view.Transform.position,
                view.TargetPos,
                PositionLerpSpeed * Time.deltaTime);
            view.Transform.rotation = Quaternion.Slerp(
                view.Transform.rotation,
                view.TargetRot,
                RotationLerpSpeed * Time.deltaTime);
        }

        List<int> playerIds = BallRoomPlayerMgr.Instance.PlayerIds;
        for (int i = 0; i < playerIds.Count; i++)
        {
            int playerId = playerIds[i];
            GameObject player = GetPlayer(playerId);
            if (player == null || !player.activeInHierarchy)
            {
                continue;
            }

            player.transform.position = Vector3.Lerp(
                player.transform.position,
                _playerTargetPos[playerId],
                PositionLerpSpeed * Time.deltaTime);
            player.transform.rotation = Quaternion.Slerp(
                player.transform.rotation,
                Quaternion.Euler(0f, _playerTargetYaw[playerId], 0f),
                RotationLerpSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 界面显示 Tapped，大约 1 秒后恢复颜色提示
    /// </summary>
    public void ShowTapped()
    {
        if (_txtColor != null)
        {
            _txtColor.text = "Tapped";
        }

        _tappedUntil = Time.time + 1f;
    }

    private void SetBallTarget(BallState state)
    {
        int ballId = state.BallId <= 0 ? 1 : state.BallId;
        BallView view = GetOrCreateBall(ballId);
        view.TargetPos = new Vector3(state.PosX, state.PosY, state.PosZ);
        view.TargetRot = new Quaternion(state.RotX, state.RotY, state.RotZ, state.RotW);
        //新球第一次出现就放在主机给的位置，不要从房间中心滑过去
        if (!view.Placed && view.Transform != null)
        {
            view.Transform.position = view.TargetPos;
            view.Transform.rotation = view.TargetRot;
            view.Placed = true;
        }
    }

    private BallView GetOrCreateBall(int ballId)
    {
        BallView view;
        if (_ballViews.TryGetValue(ballId, out view))
        {
            return view;
        }

        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "Ball_" + ballId;
        if (_ball != null)
        {
            ballObject.transform.localScale = _ball.transform.localScale;
            Renderer sourceRenderer = _ball.GetComponent<Renderer>();
            Renderer ballRenderer = ballObject.GetComponent<Renderer>();
            if (sourceRenderer != null && ballRenderer != null)
            {
                ballRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }
        }

        Rigidbody body = ballObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        view = new BallView();
        view.Transform = ballObject.transform;
        view.TargetPos = ballObject.transform.position;
        view.TargetRot = ballObject.transform.rotation;
        _ballViews[ballId] = view;
        return view;
    }

    private class BallView
    {
        public Transform Transform;
        public Vector3 TargetPos;
        public Quaternion TargetRot;
        public bool Placed;
    }

    /// <summary>
    /// 第一人称。朝向用本地鼠标，不等快照
    /// </summary>
    private void LookFromSelf()
    {
        if (_camera == null || _input == null)
        {
            return;
        }

        GameObject self = GetPlayer(BallRoomPlayerMgr.Instance.LocalPlayerId);
        if (self == null || !self.activeInHierarchy)
        {
            return;
        }

        Quaternion look = Quaternion.Euler(_input.Pitch, _input.Yaw, 0f);
        Vector3 flatForward = Quaternion.Euler(0f, _input.Yaw, 0f) * Vector3.forward;
        //眼睛在胶囊前方一点，避免镜头卡在身体里面。身体还在，别人看得到
        Vector3 eye = self.transform.position + Vector3.up * 0.75f + flatForward * 0.45f;
        _camera.transform.position = eye;
        _camera.transform.rotation = look;
    }

    private void HideAll()
    {
        _player1.SetActive(false);
        _player2.SetActive(false);
        _player3.SetActive(false);
        _player4.SetActive(false);
    }

    /// <summary>
    /// 显示这个编号的胶囊
    /// </summary>
    private void ShowPlayer(int playerId)
    {
        GameObject player = GetPlayer(playerId);
        if (player != null)
        {
            player.SetActive(true);
        }
    }

    /// <summary>
    /// 关掉退出玩家的胶囊
    /// </summary>
    private void HidePlayer(int playerId)
    {
        GameObject player = GetPlayer(playerId);
        if (player != null)
        {
            player.SetActive(false);
        }
    }

    /// <summary>
    /// 主机连接断了。加入方弹出提示
    /// </summary>
    public static void NotifyHostDisconnected()
    {
        if (Instance != null)
        {
            Instance.ShowHostDown();
        }
    }

    public void ShowHostDown()
    {
        if (_txtColor != null)
        {
            _txtColor.text = "主机已断开连接";
        }

        if (_hostDownPanel != null)
        {
            _hostDownPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (_input != null)
        {
            _input.enabled = false;
        }
    }

    /// <summary>
    /// 点确定，回到主界面
    /// </summary>
    public void OnHostDownConfirm()
    {
        NetSocketMgr.Instance.Disconnect();
        BallRoomPlayerMgr.Instance.Clear();
        HostApp hostApp = FindAnyObjectByType<HostApp>();
        if (hostApp != null)
        {
            Destroy(hostApp.gameObject);
        }

        SceneManager.LoadScene(NetDefine.MainScene);
    }

    private GameObject GetPlayer(int playerId)
    {
        if (playerId <= 0 || playerId > 4)
        {
            return null;
        }

        return _players[playerId];
    }
}
