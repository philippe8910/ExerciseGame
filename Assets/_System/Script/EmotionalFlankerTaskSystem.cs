using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    [SerializeField]
    public EmotionalFlankerTaskDataHolder currentTaskData;
    
    public List<FlankerTaskData> currentData = new List<FlankerTaskData>();

    public TMP_Text middleLetter;
    public TMP_Text upperLetter, bottomLetter;

    public Color redColor = Color.red, greenColor = Color.green;

    // 試次間隔時間 (清空畫面後的等待)
    public float timeBetweenTrials = 1.0f;
    // 每個試次允許作答的時間 (反應時間上限)
    public float[] responseTimeLimit;
    

    private void Start()
    {
        Init();
        StartCoroutine(StartTask());
    }
    
    private IEnumerator StartTask()
    {
        foreach (var data in currentData)
        {
            middleLetter.text = "+";
            upperLetter.text = "";
            bottomLetter.text = "";

            yield return new WaitForSeconds(timeBetweenTrials);

            middleLetter.color = data.midColor;
            upperLetter.color = data.OtherColor;
            bottomLetter.color = data.OtherColor;
            
            middleLetter.text = data.currentLetter;
            upperLetter.text = data.currentLetter;
            bottomLetter.text = data.currentLetter;
            
            // 設定反應時間上限
            float responseTime = responseTimeLimit[Random.Range(0, responseTimeLimit.Length)];
            
            // 等待玩家回應
            float startTime = Time.time;
            
            while (Time.time - startTime < responseTime)
            {
                // 檢查玩家是否有回應
                if (Input.GetKeyDown(KeyCode.RightArrow) && middleLetter.color == Color.green) // 假設空白鍵為回應鍵
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    break;
                }
                
                if (Input.GetKeyDown(KeyCode.LeftArrow) && middleLetter.color == Color.red) // 假設空白鍵為回應鍵
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    break;
                }
                
                yield return null; // 等待下一幀
            }
            
            // 清空畫面
            middleLetter.text = "";
            upperLetter.text = "";
            bottomLetter.text = "";
            
            middleLetter.color = Color.white;
        }
        
        int totalCount = currentData.Count;
        int correctCount = currentData.Count(d => d.isCorrect);

// 防止除以零錯誤
        float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        float averageResponseTime = currentData.Where(d => d.isCorrect).Any()
            ? currentData.Where(d => d.isCorrect).Average(d => d.responseTime)
            : 0f;

        Debug.Log($"✅ 正確率: {correctCount}/{totalCount} ({accuracy:F2}%)");
        Debug.Log($"⏱️ 平均反應時間（正確題）: {averageResponseTime:F2} 秒");

    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            // 在區間 [i, list.Count) 中取得隨機索引
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    public void Init()
    {
        currentData.Clear();

        var negativeLatter = currentTaskData.negativeLatter;
        var neutralLatter = currentTaskData.neutralLatter;

        negativeLatter = negativeLatter.Take(30).ToList();
        neutralLatter = neutralLatter.Take(30).ToList();
        
        Debug.Log("負向詞彙數量: " + currentTaskData.negativeLatter.Count);
        Debug.Log("中性詞彙數量: " + currentTaskData.neutralLatter.Count);

        
        Debug.Log("負向詞彙數量: " + negativeLatter.Count);
        Debug.Log("中性詞彙數量: " + neutralLatter.Count);

        // 定義所有要組合的配色組合
        (Color mid, Color other)[] colorCombos = new (Color, Color)[]
        {
            (Color.red, Color.red),
            (Color.green, Color.red),
            (Color.red, Color.green),
            (Color.green, Color.green)
        };

        // 負向詞處理
        foreach (var word in negativeLatter)
        {
            foreach (var (midColor, otherColor) in colorCombos)
            {
                currentData.Add(new FlankerTaskData
                {
                    currentLetter = word,
                    midColor = midColor,
                    OtherColor = otherColor,
                    isNegative = true
                });
            }
        }

        // 中性詞處理
        foreach (var word in neutralLatter)
        {
            foreach (var (midColor, otherColor) in colorCombos)
            {
                currentData.Add(new FlankerTaskData
                {
                    currentLetter = word,
                    midColor = midColor,
                    OtherColor = otherColor,
                    isNegative = false
                });
            }
        }

        // 隨機打亂資料
        ShuffleList(currentData);
    }

}

[System.Serializable]
[CreateAssetMenu(fileName = "EmotionalFlankerTaskDataHolder", menuName = "EmotionalFlankerTaskDataHolder", order = 1)]
public class EmotionalFlankerTaskDataHolder : ScriptableObject
{
    public List<string> neutralLatter;
    public List<string> negativeLatter;
}

[System.Serializable]
public class FlankerTaskData
{
    public string currentLetter;
    public Color midColor , OtherColor;
    public bool isNegative;
    public bool isCorrect;
    public float responseTime;
}
