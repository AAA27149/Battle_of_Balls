using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

/**
 * Title: 主机物理帧
 * Description: 一个 FixedUpdate 里移动所有人并让球碰撞。物理算完再广播快照
 */

public class Host_World : MonoBehaviour
{
    private const float MoveSpeed = 8f;
    private const float SpawnInterval = 15f;

    private GameObject[] _players;
    private Rigidbody[] _playerBodies;
    private List<Rigidbody> _balls = new List<Rigidbody>();
    private PlayerInputCtrl _input;
    private int _tick;
    private int _nextBallId = 2;
    private float _spawnTimer;
    private PhysicsMaterial _bounceMaterial;

    public void Init(GameObject player1, GameObject player2, GameObject player3, GameObject player4, Rigidbody ball, PlayerInputCtrl input)
    {
        _players = new GameObject[] { null, player1, player2, player3, player4 };
        _playerBodies = new Rigidbody[5];
        _input = input;
        _balls.Add(ball);

        //原来的那颗球，编号 1
        BallMark firstMark = ball.gameObject.GetComponent<BallMark>();
        if (firstMark == null)
        {
            firstMark = ball.gameObject.AddComponent<BallMark>();
        }
        firstMark.BallId = 1;
        if (ball.gameObject.GetComponent<BallTouch>() == null)
        {
            ball.gameObject.AddComponent<BallTouch>();
        }

        //球只在主机上模拟
        ball.isKinematic = false;
        ball.useGravity = true;
        ball.mass = 0.55f;
        ball.linearDamping = 0.01f;
        ball.angularDamping = 0.02f;
        ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ball.interpolation = RigidbodyInterpolation.Interpolate;
        SetBallBounce(ball);

        for (int i = 1; i <= 4; i++)
        {
            _playerBodies[i] = EnsureDynamic(_players[i]);
            if (_players[i] == null)
            {
                continue;
            }

            PlayerMark mark = _players[i].GetComponent<PlayerMark>();
            if (mark != null)
            {
                mark.PlayerId = i;
            }
        }

        StartCoroutine(SnapshotAfterPhysics());
    }

    /// <summary>
    /// 球的弹性。默认弹性是 0，撞墙只会停住
    /// </summary>
    private void SetBallBounce(Rigidbody ball)
    {
        Collider collider = ball.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        PhysicsMaterial material = new PhysicsMaterial("BallBounce");
        material.bounciness = 0.95f;
        material.dynamicFriction = 0.1f;
        material.staticFriction = 0.1f;
        material.bounceCombine = PhysicsMaterialCombine.Maximum;
        material.frictionCombine = PhysicsMaterialCombine.Average;
        _bounceMaterial = material;
        collider.material = material;
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < SpawnInterval)
        {
            return;
        }

        _spawnTimer = 0f;
        SpawnBall();
    }

    /// <summary>
    /// 主机每 15 秒在房间里再生成一颗球
    /// </summary>
    private void SpawnBall()
    {
        if (_balls.Count == 0)
        {
            return;
        }

        GameObject source = _balls[0].gameObject;
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "Ball_" + _nextBallId;
        ballObject.transform.localScale = source.transform.localScale;
        ballObject.transform.position = new Vector3(Random.Range(-11f, 11f), 0.6f, Random.Range(-11f, 11f));
        Renderer sourceRenderer = source.GetComponent<Renderer>();
        Renderer ballRenderer = ballObject.GetComponent<Renderer>();
        if (sourceRenderer != null && ballRenderer != null)
        {
            ballRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        Rigidbody body = ballObject.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.mass = 0.55f;
        body.linearDamping = 0.01f;
        body.angularDamping = 0.02f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        Collider collider = ballObject.GetComponent<Collider>();
        if (collider != null && _bounceMaterial != null)
        {
            collider.material = _bounceMaterial;
        }

        BallMark mark = ballObject.AddComponent<BallMark>();
        mark.BallId = _nextBallId;
        ballObject.AddComponent<BallTouch>();
        _nextBallId++;
        _balls.Add(body);
        LogMsg.Info("生成球::" + ballObject.name + "  " + ballObject.transform.position);
    }

    /// <summary>
    /// 玩家碰到球。主机自己直接显示，其它人只通知对应客户端
    /// </summary>
    public void OnBallTapped(int playerId)
    {
        if (playerId == BallRoomPlayerMgr.Instance.LocalPlayerId)
        {
            if (GameRoom.Instance != null)
            {
                GameRoom.Instance.ShowTapped();
            }
            return;
        }

        TappedRet ret = new TappedRet()
        {
            PlayerId = playerId,
        };
        Dictionary<int, Session> sessionDic = SessionMgr.Instance.GetSessionDic();
        foreach (KeyValuePair<int, Session> item in sessionDic)
        {
            if (item.Value._roleId != playerId)
            {
                continue;
            }

            item.Value.SendData(NetDefine.CMD_TappedCode, ret.ToByteString());
        }
    }

    private void FixedUpdate()
    {
        if (_balls.Count == 0)
        {
            return;
        }

        //1. 主机自己的输入直接写入，不绕网络
        int selfId = BallRoomPlayerMgr.Instance.LocalPlayerId;
        if (_input != null && selfId > 0)
        {
            Host_InputBuf.Instance.Set(selfId, _input.MoveX, _input.MoveZ, _input.Yaw);
        }

        //2. 这一拍的输入一起移动
        List<int> playerIds = BallRoomPlayerMgr.Instance.PlayerIds;
        for (int i = 0; i < playerIds.Count; i++)
        {
            MovePlayer(playerIds[i]);
        }
    }

    /// <summary>
    /// 物理步进之后再取样，球的反弹已经算进这一拍
    /// </summary>
    private IEnumerator SnapshotAfterPhysics()
    {
        WaitForFixedUpdate wait = new WaitForFixedUpdate();
        float timer = 0f;
        while (true)
        {
            yield return wait;
            timer += Time.fixedDeltaTime;
            //和参考项目一样，大约 0.05 秒发一次，不要每拍都发
            if (timer < 0.05f)
            {
                continue;
            }

            timer -= 0.05f;
            BroadcastSnapshot();
        }
    }

    private void MovePlayer(int playerId)
    {
        if (playerId <= 0 || playerId > 4)
        {
            return;
        }

        GameObject player = _players[playerId];
        Rigidbody body = _playerBodies[playerId];
        if (player == null || body == null || !player.activeInHierarchy)
        {
            return;
        }

        Host_InputBuf.Input input;
        if (!Host_InputBuf.Instance.TryGet(playerId, out input))
        {
            input = new Host_InputBuf.Input();
        }

        Quaternion yawRot = Quaternion.Euler(0f, input.Yaw, 0f);
        Vector3 wish = yawRot * new Vector3(input.MoveX, 0f, input.MoveZ);
        if (wish.sqrMagnitude > 1f)
        {
            wish.Normalize();
        }

        //用速度推，刚体才会被墙和别的胶囊挡住，也能把球撞出去
        body.linearVelocity = wish * MoveSpeed;
        body.angularVelocity = Vector3.zero;
        body.MoveRotation(yawRot);
    }

    private void BroadcastSnapshot()
    {
        _tick++;
        WorldSnapshot snapshot = new WorldSnapshot()
        {
            Tick = _tick,
            Ball = MakeBallState(_balls[0], 1),
        };
        for (int i = 0; i < _balls.Count; i++)
        {
            Rigidbody body = _balls[i];
            if (body == null)
            {
                continue;
            }

            BallMark mark = body.GetComponent<BallMark>();
            int ballId = mark != null ? mark.BallId : i + 1;
            snapshot.Balls.Add(MakeBallState(body, ballId));
        }

        List<int> playerIds = BallRoomPlayerMgr.Instance.PlayerIds;
        for (int i = 0; i < playerIds.Count; i++)
        {
            int playerId = playerIds[i];
            if (playerId <= 0 || playerId > 4 || _playerBodies[playerId] == null)
            {
                continue;
            }

            Rigidbody body = _playerBodies[playerId];
            snapshot.Players.Add(new PlayerState()
            {
                PlayerId = playerId,
                PosX = body.position.x,
                PosY = body.position.y,
                PosZ = body.position.z,
                Yaw = body.rotation.eulerAngles.y,
            });
        }

        Host_WorldBC.Instance.SnapshotBC(snapshot);
    }

    private BallState MakeBallState(Rigidbody body, int ballId)
    {
        return new BallState()
        {
            BallId = ballId,
            PosX = body.position.x,
            PosY = body.position.y,
            PosZ = body.position.z,
            VelX = body.linearVelocity.x,
            VelY = body.linearVelocity.y,
            VelZ = body.linearVelocity.z,
            AngVelX = body.angularVelocity.x,
            AngVelY = body.angularVelocity.y,
            AngVelZ = body.angularVelocity.z,
            RotX = body.rotation.x,
            RotY = body.rotation.y,
            RotZ = body.rotation.z,
            RotW = body.rotation.w,
        };
    }

    /// <summary>
    /// 动态刚体。会和墙、其它玩家、球发生碰撞
    /// </summary>
    private Rigidbody EnsureDynamic(GameObject player)
    {
        if (player == null)
        {
            return null;
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody>();
        }

        body.mass = 20f;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.isKinematic = false;

        PlayerMark mark = player.GetComponent<PlayerMark>();
        if (mark == null)
        {
            mark = player.AddComponent<PlayerMark>();
        }

        return body;
    }
}
