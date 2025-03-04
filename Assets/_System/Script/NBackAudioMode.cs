using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NBackAudioMode : MonoBehaviour
{
    [Header("n-back 設定")]
    [Tooltip("設定 n-back 數字，範圍 1~3")]
    [Range(1, 3)]
    public int n = 2;
    [Tooltip("總試次數")]
    public int totalTrials = 20;
    [Tooltip("每個試次的刺激播放間隔 (秒)")]
    public float stimulusInterval = 2.0f;
    
    [Header("語音設定")]
    [Tooltip("用來播放刺激的 AudioSource")]
    public AudioSource audioSource;
    [Tooltip("語音刺激素材集合")]
    public AudioClip[] audioClips;
    
    [Header("挑戰設定")]
    [Tooltip("指定必定出現的匹配試次數 (強制挑戰)，僅適用於試次編號 >= n")]
    public int forcedMatchCount = 5;

    // 內部變數
    private List<int> stimulusHistory = new List<int>();
    private int currentTrial = 0;
    private List<int> forcedMatchIndices = new List<int>();

    // 回應狀態計數
    private int hitCount = 0;               // 正確匹配：應該匹配且有回應
    private int missCount = 0;              // 錯過匹配：應該匹配但未回應
    private int falseAlarmCount = 0;        // 誤按：不應匹配卻回應了
    private int correctRejectionCount = 0;  // 正確拒絕：不應匹配且未回應

    // 玩家回應旗標
    private bool waitingForResponse = false;
    private bool userResponse = false;

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("請在 Inspector 中指定 AudioSource！");
            return;
        }
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogError("請在 Inspector 中指定 AudioClips！");
            return;
        }

        // 生成強制匹配的試次索引，範圍在 [n, totalTrials-1]
        int availableTrials = totalTrials - n;
        if (forcedMatchCount > availableTrials)
        {
            forcedMatchCount = availableTrials;
        }
        forcedMatchIndices = GenerateUniqueRandomIndices(n, totalTrials - 1, forcedMatchCount);
        Debug.Log("強制挑戰試次編號: " + string.Join(", ", forcedMatchIndices));

        Debug.Log("開始 n-back 語音遊戲，當當前刺激與 n 個試次之前刺激相同時請按空白鍵 (Space)。按任意鍵開始...");
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
                stimulus = Random.Range(0, audioClips.Length);
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
                    candidate = Random.Range(0, audioClips.Length);
                } while (candidate == stimulusHistory[currentTrial - n]);
                stimulus = candidate;
            }
            stimulusHistory.Add(stimulus);

            // 播放語音刺激
            if (audioClips[stimulus] != null)
            {
                audioSource.PlayOneShot(audioClips[stimulus]);
                Debug.Log("試次 " + (currentTrial + 1) + " 播放的音訊: " + audioClips[stimulus].name);
            }

            // 判斷是否為 n-back 的匹配試次
            bool isMatch = false;
            if (currentTrial >= n)
            {
                if (stimulus == stimulusHistory[currentTrial - n])
                {
                    isMatch = true;
                }
            }

            // 等待玩家回應
            waitingForResponse = true;
            userResponse = false;
            float timer = 0f;
            while (timer < stimulusInterval)
            {
                if (Input.GetKeyDown(KeyCode.Z) && waitingForResponse)
                {
                    userResponse = true;
                    waitingForResponse = false;
                    Debug.Log("玩家回應：按下空白鍵");
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // 根據回應狀態進行分類
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

            currentTrial++;
            yield return new WaitForSeconds(0.5f);
        }

        // 遊戲結束，計算並輸出統計結果
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
