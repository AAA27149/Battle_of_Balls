public class NetDefine
{
    public const string IPHost = "127.0.0.1"; //本机ip

    public const int RoomPort = 13010; //房间主机端口。一个人做主机，其他人连这个端口


    public const ushort CMD_ErrCode = 10001; //错误码

    //这些都是 BasePackage 里的 protocode  协议码
    public const ushort CMD_JoinRoomCode = 12010; //加入房间请求码

    public const ushort CMD_PlayerInputCode = 12020; //玩家输入请求码

    public const ushort CMD_WorldSnapshotCode = 12030; //世界快照返回码

    public const ushort CMD_PlayerEnterCode = 12040; //有玩家进入返回码

    public const ushort CMD_PlayerExitCode = 12050; //有玩家离开返回码
}

/// <summary>
/// 连接状态
/// </summary>
public enum ConnState
{
    Connected,
    Disconnected,
}

/// <summary>
/// 客户端类型
/// </summary>
public enum ClientType
{
    Unity,
}
