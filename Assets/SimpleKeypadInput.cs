using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SimpleKeypadInput : MonoBehaviour
{
    [Header("UI 元件")]
    public List<Button> numberButtons;       // 數字按鈕清單
    public Button confirmButton;             // 完成按鈕
    public TMP_Text displayText;             // 顯示輸入文字

    private void Start()
    {
        // 註冊每個數字按鈕的點擊事件
        for (int i = 0; i < numberButtons.Count; i++)
        {
            int capturedIndex = i; // 避免閉包問題
            numberButtons[i].onClick.AddListener(() =>
            {
                displayText.text += capturedIndex.ToString();
            });
        }

        // 註冊完成按鈕事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
        }
    }

    void OnConfirm()
    {
        PlayerPrefs.SetString("ID" , displayText.text);
        SceneManager.LoadScene("Main");
    }
}