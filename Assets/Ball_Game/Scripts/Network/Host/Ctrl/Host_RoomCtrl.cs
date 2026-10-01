/**
 * Title: 房间主机指令处理
 * Description: 对应原来的 Game_RoleCtrl。进房和输入的具体逻辑后面再补
 */

public class Host_RoomCtrl : IContainer
{
    public void OnInit()
    {
    }

    /// <summary>
    /// 房间主机作为服务端，接收其它客户端的数据
    /// </summary>
    public void OnServerCommand(ServerBase serverBase, BasePackage basePackage)
    {
        switch (basePackage.ProtoCode)
        {
            case NetDefine.CMD_JoinRoomCode: //还是注册了对应的指令集才能接收到消息
                OnJoinRoomHandle(serverBase, basePackage);
                break;
            case NetDefine.CMD_PlayerInputCode:
                OnPlayerInputHandle(serverBase, basePackage);
                break;
        }
    }

    /// <summary>
    /// 本进程不去连接别的服务器，这个接口留空
    /// </summary>
    public void OnClientCommand(ServerBase serverBase, BasePackage basePackage)
    {
    }

    /// <summary>
    /// 加入房间请求
    /// </summary>
    private void OnJoinRoomHandle(ServerBase serverBase, BasePackage basePackage)
    {
        LogMsg.Info("收到进房请求，逻辑后续再写");
    }

    /// <summary>
    /// 玩家输入请求
    /// </summary>
    private void OnPlayerInputHandle(ServerBase serverBase, BasePackage basePackage)
    {
    }
}
