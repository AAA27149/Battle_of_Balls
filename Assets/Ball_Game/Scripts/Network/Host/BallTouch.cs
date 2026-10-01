using UnityEngine;

/**
 * Title: 玩家身上的编号
 * Description: 球碰到胶囊时，用来知道是几号玩家
 */

public class PlayerMark : MonoBehaviour
{
    public int PlayerId;
}

/**
 * Title: 球的编号
 */
public class BallMark : MonoBehaviour
{
    public int BallId;
}

/**
 * Title: 碰到球
 * Description: 只在主机上有意义。碰撞由主机判定
 */

public class BallTouch : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        PlayerMark mark = collision.collider.GetComponent<PlayerMark>();
        if (mark == null)
        {
            return;
        }

        Host_World world = FindAnyObjectByType<Host_World>();
        if (world != null)
        {
            world.OnBallTapped(mark.PlayerId);
        }
    }
}
