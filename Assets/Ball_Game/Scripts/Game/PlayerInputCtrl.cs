using UnityEngine;
using UnityEngine.InputSystem;

/**
 * Title: 玩家输入
 * Description: 第一人称朝向和移动。鼠标在 Update 里读，主机在 FixedUpdate 里用
 */

public class PlayerInputCtrl : MonoBehaviour
{
    public float MoveX;
    public float MoveZ;
    public float Yaw;
    public float Pitch;

    private float _yaw;
    private float _pitch;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null)
        {
            return;
        }

        //1. 松开鼠标后，点一下再锁回去
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        //2. 朝向
        _yaw += mouse.delta.x.ReadValue() * 0.08f;
        _pitch -= mouse.delta.y.ReadValue() * 0.08f;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);
        Yaw = _yaw;
        Pitch = _pitch;

        //3. 移动，相对自己的朝向
        float x = 0f;
        float z = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            x -= 1f;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            x += 1f;
        }
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            z += 1f;
        }
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            z -= 1f;
        }
        MoveX = x;
        MoveZ = z;
    }
}
