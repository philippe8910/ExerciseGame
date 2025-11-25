using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class SimpleKeypadInput : MonoBehaviour
{
    [TitleGroup("UI 元件")]
    [Required, SceneObjectsOnly]
    [LabelText("數字按鈕 (0-9)")]
    public List<Button> numberButtons;
    
    [Required, SceneObjectsOnly]
    [LabelText("確認按鈕")]
    public Button confirmButton;
    
    [Required, SceneObjectsOnly]
    [LabelText("顯示文字")]
    public TMP_Text displayText;
    
    [SceneObjectsOnly]
    [LabelText("刪除按鈕（選填）")]
    [InfoBox("可選：用於刪除最後一個輸入的字元")]
    public Button deleteButton;
    
    [SceneObjectsOnly]
    [LabelText("清空按鈕（選填）")]
    [InfoBox("可選：用於清空所有輸入")]
    public Button clearButton;

    [TitleGroup("設定")]
    [LabelText("目標場景名稱")]
    public string targetSceneName = "Main";
    
    [LabelText("最大輸入長度")]
    [MinValue(1)]
    public int maxInputLength = 10;
    
    [LabelText("初始顯示文字")]
    public string initialText = "";

    [TitleGroup("遊戲狀態")]
    [ReadOnly, ShowInInspector]
    private string currentInput = "";

    private void Start()
    {
        Debug.Log("🎮 KeypadInput 系統啟動");
        
        if (!ValidateComponents()) return;

        InitializeKeypad();
    }

    bool ValidateComponents()
    {
        bool isValid = true;

        if (numberButtons == null || numberButtons.Count == 0)
        {
            Debug.LogError("❌ numberButtons 未設定或為空！");
            isValid = false;
        }
        else if (numberButtons.Count != 10)
        {
            Debug.LogWarning($"⚠️ numberButtons 數量為 {numberButtons.Count}，建議設定 10 個按鈕（0-9）");
        }

        if (confirmButton == null)
        {
            Debug.LogError("❌ confirmButton 未設定！");
            isValid = false;
        }

        if (displayText == null)
        {
            Debug.LogError("❌ displayText 未設定！");
            isValid = false;
        }

        if (isValid)
        {
            Debug.Log("✅ 所有必要組件檢查通過");
        }

        return isValid;
    }

    void InitializeKeypad()
    {
        // 設定初始顯示文字
        currentInput = initialText;
        UpdateDisplay();

        // 註冊每個數字按鈕的點擊事件
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] == null)
            {
                Debug.LogWarning($"⚠️ numberButtons[{i}] 為 null，跳過");
                continue;
            }

            int capturedIndex = i; // 避免閉包問題
            numberButtons[i].onClick.AddListener(() => OnNumberClick(capturedIndex));
            
            Debug.Log($"✓ 註冊數字按鈕 {i}");
        }

        // 註冊確認按鈕事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
            Debug.Log("✓ 註冊確認按鈕");
        }

        // 註冊刪除按鈕事件（如果有）
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDelete);
            Debug.Log("✓ 註冊刪除按鈕");
        }

        // 註冊清空按鈕事件（如果有）
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClear);
            Debug.Log("✓ 註冊清空按鈕");
        }

        Debug.Log($"🎹 Keypad 初始化完成，最大長度: {maxInputLength}");
    }

    void OnNumberClick(int number)
    {
        // 檢查是否超過最大長度
        if (currentInput.Length >= maxInputLength)
        {
            Debug.LogWarning($"⚠️ 已達到最大輸入長度 ({maxInputLength})");
            return;
        }

        currentInput += number.ToString();
        UpdateDisplay();
        Debug.Log($"🔢 輸入數字: {number}, 當前輸入: {currentInput}");
    }

    void OnDelete()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
            Debug.Log($"🔙 刪除字元, 當前輸入: {currentInput}");
        }
    }

    void OnClear()
    {
        currentInput = "";
        UpdateDisplay();
        Debug.Log("🗑️ 清空輸入");
    }

    void UpdateDisplay()
    {
        if (displayText != null)
        {
            displayText.text = currentInput;
        }
    }

    [Button("測試確認", ButtonSizes.Large), GUIColor(0.5f, 1, 0.5f)]
    [HideInEditorMode]
    void OnConfirm()
    {
        if (string.IsNullOrEmpty(currentInput))
        {
            Debug.LogWarning("⚠️ 輸入為空，無法確認");
            return;
        }

        // 儲存 ID 到 PlayerPrefs
        PlayerPrefs.SetString("ID", currentInput);
        PlayerPrefs.Save();
        Debug.Log($"💾 已儲存 ID: {currentInput}");

        // 載入目標場景
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"🚀 載入場景: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("❌ targetSceneName 未設定！");
        }
    }

    [Button("清除已儲存的 ID", ButtonSizes.Medium), GUIColor(1, 0.5f, 0.5f)]
    void ClearSavedID()
    {
        if (PlayerPrefs.HasKey("ID"))
        {
            string savedID = PlayerPrefs.GetString("ID");
            PlayerPrefs.DeleteKey("ID");
            PlayerPrefs.Save();
            Debug.Log($"🗑️ 已清除儲存的 ID: {savedID}");
        }
        else
        {
            Debug.Log("ℹ️ 沒有已儲存的 ID");
        }
    }

    [Button("顯示已儲存的 ID", ButtonSizes.Medium), GUIColor(0.5f, 0.5f, 1)]
    void ShowSavedID()
    {
        if (PlayerPrefs.HasKey("ID"))
        {
            string savedID = PlayerPrefs.GetString("ID");
            Debug.Log($"📋 已儲存的 ID: {savedID}");
        }
        else
        {
            Debug.Log("ℹ️ 沒有已儲存的 ID");
        }
    }

    private void OnDestroy()
    {
        // 清理事件監聽
        if (numberButtons != null)
        {
            foreach (var button in numberButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();
        
        if (deleteButton != null)
            deleteButton.onClick.RemoveAllListeners();
        
        if (clearButton != null)
            clearButton.onClick.RemoveAllListeners();

        Debug.Log("🧹 KeypadInput 清理完成");
    }
}