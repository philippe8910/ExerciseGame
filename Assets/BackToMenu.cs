using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public InputActionReference aButtonAction; // 對應 "Activate" action，也就是 A 鍵
    public float holdDuration = 3f;

    private float holdTime = 0f;
    private bool isHolding = false;

    private void OnEnable()
    {
        aButtonAction.action.started += OnButtonPressed;
        aButtonAction.action.canceled += OnButtonReleased;
        aButtonAction.action.Enable();
    }

    private void OnDisable()
    {
        aButtonAction.action.started -= OnButtonPressed;
        aButtonAction.action.canceled -= OnButtonReleased;
        aButtonAction.action.Disable();
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        isHolding = true;
        holdTime = 0f;
    }

    private void OnButtonReleased(InputAction.CallbackContext context)
    {
        isHolding = false;
        holdTime = 0f;
    }

    private void Update()
    {
        if (isHolding)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= holdDuration)
            {
                Debug.Log("已按下 A 鍵超過三秒，返回主選單！");
                SceneManager.LoadScene("Main");
                isHolding = false; // 防止重複呼叫
            }
        }
    }
}
