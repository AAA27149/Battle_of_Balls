using UnityEngine;

/**
 * Title: 房间主机启动
 * Description: 对应原来的 GameApp。点创建房间的客户端在本进程里听端口
 */

public class HostApp : MonoBehaviour
{
    private NetServer _server;

    public bool IsHost
    {
        get { return _server != null; }
    }

    /// <summary>
    /// 点创建房间后再听端口。没有中心服可连，所以这里不传 NetClient
    /// </summary>
    public void StartHost()
    {
        if (_server != null)
        {
            return;
        }

        Host_RoomCtrl roomCtrl = new Host_RoomCtrl();
        roomCtrl.OnInit();

        _server = new NetServer(null);
        _server.StartServer(NetDefine.IPHost, NetDefine.RoomPort);

        //注册了指令集才能正常接收消息
        _server.RegistCommand(NetDefine.CMD_JoinRoomCode, roomCtrl);
        _server.RegistCommand(NetDefine.CMD_PlayerInputCode, roomCtrl);
    }

    private void OnDestroy()
    {
        if (_server != null)
        {
            _server.Close();
            _server = null;
        }
    }
}
