using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
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

    private GameObject[] _players;
    private bool _isHost;
    private PlayerInputCtrl _input;
    private Rigidbody _ball;
    private Camera _camera;
    private bool _hasNet;
    private Vector3 _ballTargetPos;
    private Quaternion _ballTargetRot;
    private Vector3[] _playerTargetPos;
    private float[] _playerTargetYaw;
    private const float PositionLerpSpeed = 12f;
    private const float RotationLerpSpeed = 15f;

    private void Awake()
    {
        _players = new GameObject[] { null, _player1, _player2, _player3, _player4 };
        _playerTargetPos = new Vector3[5];
        _playerTargetYaw = new float[5];
        _input = gameObject.AddComponent<PlayerInputCtrl>();
        _camera = Camera.main;
        _ball = GameObject.Find("Ball").GetComponent<Rigidbody>();

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

        //3. 之后再有人进来，再显示对应胶囊
        BallRoomPlayerMgr.Instance.OnPlayerEnter += ShowPlayer;

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
        if (snapshot.Ball != null)
        {
            _ballTargetPos = new Vector3(snapshot.Ball.PosX, snapshot.Ball.PosY, snapshot.Ball.PosZ);
            _ballTargetRot = new Quaternion(snapshot.Ball.RotX, snapshot.Ball.RotY, snapshot.Ball.RotZ, snapshot.Ball.RotW);
            if (!_hasNet)
            {
                _ball.transform.position = _ballTargetPos;
                _ball.transform.rotation = _ballTargetRot;
            }
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
        if (!_hasNet || _ball == null)
        {
            return;
        }

        _ball.transform.position = Vector3.Lerp(
            _ball.transform.position,
            _ballTargetPos,
            PositionLerpSpeed * Time.deltaTime);
        _ball.transform.rotation = Quaternion.Slerp(
            _ball.transform.rotation,
            _ballTargetRot,
            RotationLerpSpeed * Time.deltaTime);

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

    private GameObject GetPlayer(int playerId)
    {
        if (playerId <= 0 || playerId > 4)
        {
            return null;
        }

        return _players[playerId];
    }
}
