using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class TrialTypeInfo
{
    // 每筆資料包含一個 Sprite 與該題型的正確答案數量
    public Sprite sprite;      
    public int correctCount;   
}

public class EmotionalStroopCore : MonoBehaviour
{
    [Header("情緒圖片列表")]
    public List<Sprite> negativeImageList;  // 負面圖片列表
    public List<Sprite> neutralImageList;     // 中性圖片列表
    
    [Tooltip("設定總試次中負面圖片出現的次數")]
    public int totalNegativeAppearances = 5;  // 負面圖片總出現次數

    [Header("圖示生成區")]
    public MeshRenderer iconContainer;           // 用於生成圖示的容器

    [Header("題型資料設定")]
    // 三個 List 分別存放不同題型的 TrialTypeInfo
    public List<TrialTypeInfo> type1List;
    public List<TrialTypeInfo> type2List;
    public List<TrialTypeInfo> type3List;

    [Header("生成次數設定")]
    public int type1TrialCount = 5;       
    public int type2TrialCount = 5;       
    public int type3TrialCount = 5;       

    [Header("其他參數")]
    public float timeInterval = 2.0f;  // 顯示情緒圖片的間隔時間
    

}
