using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Title: 主机物理帧
 * Description: 一个 FixedUpdate 里移动所有人并让球碰撞。物理算完再广播快照
 */

public class Host_World : MonoBehaviour
{
    private const float MoveSpeed = 6.5f;

    private GameObject[] _players;
    private Rigidbody[] _playerBodies;
    private Rigidbody _ball;
    private PlayerInputCtrl _input;
    private int _tick;

    public void Init(GameObject player1, GameObject player2, GameObject player3, GameObject player4, Rigidbody ball, PlayerInputCtrl input)
    {
        _players = new GameObject[] { null, player1, player2, player3, player4 };
        _playerBodies = new Rigidbody[5];
        _ball = ball;
        _input = input;

        //球只在主机上模拟
        _ball.isKinematic = false;
        _ball.useGravity = true;
        _ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _ball.interpolation = RigidbodyInterpolation.Interpolate;

        for (int i = 1; i <= 4; i++)
        {
            _playerBodies[i] = EnsureDynamic(_players[i]);
        }

        StartCoroutine(SnapshotAfterPhysics());
    }

    private void FixedUpdate()
    {
        if (_ball == null)
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
            Ball = new BallState()
            {
                PosX = _ball.position.x,
                PosY = _ball.position.y,
                PosZ = _ball.position.z,
                VelX = _ball.linearVelocity.x,
                VelY = _ball.linearVelocity.y,
                VelZ = _ball.linearVelocity.z,
                AngVelX = _ball.angularVelocity.x,
                AngVelY = _ball.angularVelocity.y,
                AngVelZ = _ball.angularVelocity.z,
                RotX = _ball.rotation.x,
                RotY = _ball.rotation.y,
                RotZ = _ball.rotation.z,
                RotW = _ball.rotation.w,
            },
        };

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
        return body;
    }
}
