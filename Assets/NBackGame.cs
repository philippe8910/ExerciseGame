using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBackSeparateRandomWithExactDistribution : MonoBehaviour
{
    [Header("n-back 設定")]
    [Range(1, 3), Tooltip("設定 n-back 數字，範圍 1~3")]
    public int n = 2;

    [Tooltip("總試次數")]
    public int totalTrials = 20;

    [Tooltip("每題顯示/播放的刺激時間（秒）")]
    public float stimulusInterval = 2f;
    
    [Tooltip("總視覺題目")]
    public int visualTrials = 10;
    
    [Tooltip("總聽覺題目")]
    public int audioTrials = 10;

    [Header("九宮格(視覺)設定")]
    [Tooltip("指定場上九宮格的 Plane 物件")]
    public GameObject[] gridPlanes;

    [Tooltip("刺激時要變換的材質")]
    public Material stimulusColor;

    [Tooltip("預設材質(刺激結束後恢復)")]
    public Material defaultColor;

    [Header("聲音(聽覺)設定")]
    [Tooltip("用來播放音效的 AudioSource")]
    public AudioSource audioSource;

    [Tooltip("可用的 AudioClip 清單")]
    public AudioClip[] audioClips;

    [Header("回應次數分配")]
    [Tooltip("指定必須回應圖片的試次數")]
    public int imageResponseCount = 5;

    [Tooltip("指定必須回應音效的試次數")]
    public int audioResponseCount = 5;

    [Tooltip("指定圖片與音效都必須回應的試次數")]
    public int bothResponseCount = 10;

    [Header("玩家按鍵設定")]
    [Tooltip("玩家按鍵 - 視覺刺激")]
    public KeyCode visualKey = KeyCode.Space;

    [Tooltip("玩家按鍵 - 聽覺刺激")]
    public KeyCode audioKey = KeyCode.Z;

    // ---- 內部記錄 ----
    private List<int> visualIDList = new List<int>();  // 視覺 n-back ID
    private List<int> audioIDList  = new List<int>();  // 聲音 n-back ID
    
    private List<bool> visualResponseList = new List<bool>();  // 視覺回應紀錄
    private List<bool> audioResponseList = new List<bool>();     // 聲音回應紀錄
    

    // 統計：視覺
    private int visualHit = 0;
    private int visualMiss = 0;
    private int visualFalseAlarm = 0;
    private int visualCorrectRejection = 0;

    // 統計：聽覺
    private int audioHit = 0;
    private int audioMiss = 0;
    private int audioFalseAlarm = 0;
    private int audioCorrectRejection = 0;

    void Start()
    {
        // 檢查：兩陣列必須有元素
        if (gridPlanes == null || gridPlanes.Length == 0)
        {
            Debug.LogError("請在 Inspector 指定九宮格的 Plane 物件！");
            return;
        }
        if (audioSource == null || audioClips == null || audioClips.Length == 0)
        {
            Debug.LogError("請在 Inspector 指定 audioSource 與 audioClips！");
            return;
        }
    }

    public void Init()
    {
        for (int i = 0 ; i < totalTrials - visualTrials; i++)
        {
            visualResponseList.Add(false);
        }
        
        for (int i = 0; i < visualTrials; i++)
        {
            visualResponseList.Add(true);
        }
        
        for (int i = 0 ; i < totalTrials - audioTrials; i++)
        {
            audioResponseList.Add(false);
        }
        
        for (int i = 0; i < audioTrials; i++)
        {
            visualResponseList.Add(true);
        }
        

        
    }
    
    

    
}
