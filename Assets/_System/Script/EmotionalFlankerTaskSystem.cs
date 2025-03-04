using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    [SerializeField]
    public EmotionalFlankerTaskDataHolder currentTaskData;

    public List<string> currentTrialData = new List<string>();

    public TMP_Text middleLetterText;
    public TMP_Text flankerLetterText_0, flankerLetterText_1;

    public Color redColor = Color.red, greenColor = Color.green;

    // 試次間隔時間 (清空畫面後的等待)
    public float timeBetweenTrials = 1.0f;
    // 每個試次允許作答的時間 (反應時間上限)
    public float responseTimeLimit = 2.0f;

    public int negativeLatterCount = 10;
    public int totalTrialCount = 20;
    public int currentIndex = 0;

    private void Start()
    {
        // 產生試次資料：前 negativeLatterCount 筆為負向 (isNegative = true)，其餘為中性
        for (int i = 0; i < totalTrialCount; i++)
        {
            currentTrialData.Add(LatterGenerator(i < negativeLatterCount));
        }
        
        // 將試次洗牌
        ShuffleList(currentTrialData);
        
        // 開始試驗流程
        StartCoroutine(StartTask());
    }

    /// <summary>
    /// 主流程：依序顯示試次，隨機設定紅或綠，並檢測玩家是否在限定時間內作答。
    /// 紅色試次正確答案為 O 鍵，綠色則為 P 鍵。
    /// </summary>
    private IEnumerator StartTask()
    {
        // 逐一執行每一筆試次
        foreach (string trialLetter in currentTrialData)
        {
            // 隨機決定顏色：這裡以 50% 機率決定紅或綠
            bool isRed = Random.value < 0.5f;
            Color chosenColor = isRed ? redColor : greenColor;
            
            // 設定 UI 文字與顏色（中間字及左右兩側的干擾字）
            middleLetterText.text = trialLetter;
            middleLetterText.color = chosenColor;
            flankerLetterText_0.text = trialLetter;
            flankerLetterText_0.color = chosenColor;
            flankerLetterText_1.text = trialLetter;
            flankerLetterText_1.color = chosenColor;

            // 設定正確按鍵：紅色正確為 O 鍵，綠色正確為 P 鍵
            KeyCode correctKey = isRed ? KeyCode.O : KeyCode.P;

            bool responded = false;
            bool correctResponse = false;
            float reactionTime = 0f;
            float startTime = Time.time;

            // 在 responseTimeLimit 時間內等待玩家作答
            while (Time.time - startTime < responseTimeLimit)
            {
                // 先偵測是否有按 O 或 P 鍵
                if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.P))
                {
                    reactionTime = Time.time - startTime;
                    
                    // 判斷玩家是否按下正確的按鍵
                    // 注意：因為 GetKeyDown 只能在該 frame 回傳 true，
                    // 因此這裡用兩個 if 判斷，分別檢查是否按下正確或錯誤的鍵
                    if (Input.GetKeyDown(correctKey))
                    {
                        correctResponse = true;
                    }
                    else
                    {
                        correctResponse = false;
                    }
                    responded = true;
                    break;
                }
                yield return null;
            }

            if (!responded)
            {
                Debug.Log($"Trial: {trialLetter} | Color: {(isRed ? "Red" : "Green")} | No Response");
            }
            else
            {
                Debug.Log($"Trial: {trialLetter} | Color: {(isRed ? "Red" : "Green")} | Response: {(correctResponse ? "Correct" : "Incorrect")} | RT: {reactionTime:F3}s");
            }

            // 清空畫面（也可在此處加入反饋提示）
            middleLetterText.text = "";
            flankerLetterText_0.text = "";
            flankerLetterText_1.text = "";

            // 試次間隔
            yield return new WaitForSeconds(timeBetweenTrials);
        }

        Debug.Log("Task finished!");
    }

    /// <summary>
    /// 根據是否為負向，從相應的字串清單中隨機挑選一個字串
    /// </summary>
    private string LatterGenerator(bool isNegative)
    {
        int count = isNegative ? currentTaskData.negativeLatter.Count : currentTaskData.neutralLatter.Count;
        int randomIndex = Random.Range(0, count);

        return isNegative ? currentTaskData.negativeLatter[randomIndex] : currentTaskData.neutralLatter[randomIndex];
    }

    /// <summary>
    /// 使用 Fisher–Yates 洗牌法對 List 就地洗牌
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            // 在區間 [i, list.Count) 中取得隨機索引
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "EmotionalFlankerTaskDataHolder", menuName = "EmotionalFlankerTaskDataHolder", order = 1)]
public class EmotionalFlankerTaskDataHolder : ScriptableObject
{
    public List<string> neutralLatter;
    public List<string> negativeLatter;
}
