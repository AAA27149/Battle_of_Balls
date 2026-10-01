using System.Collections.Generic;

/**
 * Title: 本帧输入
 * Description: 每个玩家只留最后一条。主机在同一个 FixedUpdate 里一起用
 */

public class Host_InputBuf : Singleton<Host_InputBuf>
{
    public struct Input
    {
        public float MoveX;
        public float MoveZ;
        public float Yaw;
    }

    private Dictionary<int, Input> _inputDic = new Dictionary<int, Input>();
    private object _lock = new object();

    /// <summary>
    /// 记下这个玩家最新的操作
    /// </summary>
    public void Set(int playerId, float moveX, float moveZ, float yaw)
    {
        lock (_lock)
        {
            Input input = new Input()
            {
                MoveX = moveX,
                MoveZ = moveZ,
                Yaw = yaw,
            };
            _inputDic[playerId] = input;
        }
    }

    /// <summary>
    /// 取出最新操作
    /// </summary>
    public bool TryGet(int playerId, out Input input)
    {
        lock (_lock)
        {
            return _inputDic.TryGetValue(playerId, out input);
        }
    }
}
