using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBackColorChange : MonoBehaviour
{
    [Header("n-back 設定")]
    [Tooltip("設定 n-back 數字，範圍 1~3")]
    [Range(1, 3)]
    public int n = 2;
    [Tooltip("總試次數")]
    public int totalTrials = 20;
    [Tooltip("每個試次的刺激顯示時間 (秒)")]
    public float stimulusInterval = 2.0f;

    [Header("九宮格設定")]
    [Tooltip("請指定場上九宮格的 Plane 物件")]
    public GameObject[] gridPlanes;
    [Tooltip("刺激時要變換的顏色")]
    public Color stimulusColor = Color.yellow;
    [Tooltip("預設顏色 (刺激結束後恢復)")]
    public Color defaultColor = Color.white;

    [Header("挑戰設定")]
    [Tooltip("指定必定出現的匹配試次數 (強制挑戰)，僅適用於試次編號 >= n")]
    public int forcedMatchCount = 5;

    // 內部變數
    private List<int> stimulusHistory = new List<int>();
    private int currentTrial = 0;
    private List<int> forcedMatchIndices = new List<int>();

    // 回應狀態計數
    private int hitCount = 0;               // 正確匹配
    private int missCount = 0;              // 錯過匹配
    private int falseAlarmCount = 0;        // 誤按
    private int correctRejectionCount = 0;  // 正確拒絕

    // 玩家回應旗標
    private bool waitingForResponse = false;
    private bool userResponse = false;

    void Start()
    {
        if (gridPlanes == null || gridPlanes.Length == 0)
        {
            Debug.LogError("請在 Inspector 中指定九宮格的 Plane 物件！");
            return;
        }
        // 開始前先清除所有 Plane 顏色
        ResetAllPlanes();

        // 生成強制匹配的試次索引，範圍在 [n, totalTrials-1]
        int availableTrials = totalTrials - n;
        if (forcedMatchCount > availableTrials)
        {
            forcedMatchCount = availableTrials;
        }
        forcedMatchIndices = GenerateUniqueRandomIndices(n, totalTrials - 1, forcedMatchCount);
        Debug.Log("強制挑戰試次編號: " + string.Join(", ", forcedMatchIndices));

        Debug.Log("開始 n-back 遊戲，當當前刺激與 n 個試次之前刺激相同時請按空白鍵 (Space)。按任意鍵開始...");
        StartCoroutine(GameStart());
    }

    IEnumerator GameStart()
    {
        yield return new WaitUntil(() => Input.anyKeyDown);
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (currentTrial < totalTrials)
        {
            int stimulus;
            if (currentTrial < n)
            {
                // 前 n 個試次無法構成匹配
                stimulus = Random.Range(0, gridPlanes.Length);
            }
            else if (forcedMatchIndices.Contains(currentTrial))
            {
                // 強制匹配：採用 n 個試次前的刺激
                stimulus = stimulusHistory[currentTrial - n];
                Debug.Log("試次 " + (currentTrial + 1) + " 為強制挑戰 (匹配) 試次");
            }
            else
            {
                // 非強制試次：確保不匹配 (重新選擇直到不等於 n 個試次前的刺激)
                int candidate;
                do {
                    candidate = Random.Range(0, gridPlanes.Length);
                } while (candidate == stimulusHistory[currentTrial - n]);
                stimulus = candidate;
            }
            stimulusHistory.Add(stimulus);

            // 顯示前先重置所有 Plane 的顏色，避免上題影響
            ResetAllPlanes();
            SetPlaneColor(stimulus, stimulusColor);
            Debug.Log("試次 " + (currentTrial + 1) + " : 變色的 Plane 為 " + stimulus);

            // 判斷是否為 n-back 的匹配試次
            bool isMatch = false;
            if (currentTrial >= n)
            {
                if (stimulus == stimulusHistory[currentTrial - n])
                {
                    isMatch = true;
                }
            }

            // 重設玩家回應旗標，等待玩家在限定時間內按下空白鍵
            waitingForResponse = true;
            userResponse = false;
            float timer = 0f;
            while (timer < stimulusInterval)
            {
                if (Input.GetKeyDown(KeyCode.Space) && waitingForResponse)
                {
                    userResponse = true;
                    waitingForResponse = false;
                    Debug.Log("玩家回應：按下空白鍵");
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // 根據回應情況分類
            if (isMatch && userResponse)
            {
                Debug.Log("正確匹配！");
                hitCount++;
            }
            else if (isMatch && !userResponse)
            {
                Debug.Log("錯過匹配！");
                missCount++;
            }
            else if (!isMatch && userResponse)
            {
                Debug.Log("誤按！");
                falseAlarmCount++;
            }
            else
            {
                Debug.Log("正確拒絕。");
                correctRejectionCount++;
            }

            // 試次結束後清除顏色，確保下一題畫面清晰
            ResetAllPlanes();

            currentTrial++;
            yield return new WaitForSeconds(0.5f);
        }

        // 遊戲結束，計算並輸出統計數據
        Debug.Log("遊戲結束！");
        Debug.Log("總試次數: " + totalTrials);
        Debug.Log("Hit (正確匹配): " + hitCount);
        Debug.Log("Miss (錯過匹配): " + missCount);
        Debug.Log("False Alarm (誤按): " + falseAlarmCount);
        Debug.Log("Correct Rejection (正確拒絕): " + correctRejectionCount);

        float accuracy = ((float)(hitCount + correctRejectionCount)) / totalTrials;
        float errorRate = ((float)(missCount + falseAlarmCount)) / totalTrials;
        Debug.Log("正確率: " + (accuracy * 100).ToString("F2") + "%");
        Debug.Log("錯誤率: " + (errorRate * 100).ToString("F2") + "%");
    }

    // 將所有 Plane 顏色重設為預設顏色
    void ResetAllPlanes()
    {
        foreach (GameObject plane in gridPlanes)
        {
            if (plane.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.material.color = defaultColor;
            }
        }
    }

    // 設定指定 Plane 的顏色
    void SetPlaneColor(int index, Color color)
    {
        if (index < 0 || index >= gridPlanes.Length)
            return;
        if (gridPlanes[index].TryGetComponent<Renderer>(out Renderer renderer))
        {
            renderer.material.color = color;
        }
    }

    // 產生獨特的隨機索引，範圍包含 lower 到 upper，數量為 count
    List<int> GenerateUniqueRandomIndices(int lower, int upper, int count)
    {
        List<int> indices = new List<int>();
        for (int i = lower; i <= upper; i++)
        {
            indices.Add(i);
        }
        // 洗牌
        for (int i = 0; i < indices.Count; i++)
        {
            int randomIndex = Random.Range(i, indices.Count);
            (indices[i], indices[randomIndex]) = (indices[randomIndex], indices[i]);
        }
        // 取前 count 個
        return indices.GetRange(0, count);
    }
}
