using UnityEngine;

/**
 * Title: 客户端网络启动
 * Description: 进游戏时拿到主线程的同步上下文，后面收包才能派发回主线程
 */

public class NetSocketBehaviour : MonoBehaviour
{
    private void Awake()
    {
        NetSocketMgr.Instance.Init();
    }

    private void OnDestroy()
    {
        NetSocketMgr.Instance.Disconnect();
    }
}
