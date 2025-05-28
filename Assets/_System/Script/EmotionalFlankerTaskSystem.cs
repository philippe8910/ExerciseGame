using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    [Header("Flanker 設定")]
    public List<FlankerTaskData> currentData = new List<FlankerTaskData>();

    public GameObject endPanel;
    public TMP_Text systemText;
    public TMP_Text middleLetter;
    public TMP_Text upperLetter, bottomLetter;

    public Color redColor = Color.red, greenColor = Color.green;
    public float timeBetweenTrials = 1.0f;
    public float[] responseTimeLimit;

    [Header("旗子參考")]
    public FlagComponent leftFlag;
    public FlagComponent rightFlag;

    public bool isTest = false;

    private void Start()
    {
        if (systemText == null)
        {
            Debug.LogError("❌ systemText 未綁定，錯誤無法顯示！");
            return;
        }

        if (middleLetter == null || upperLetter == null || bottomLetter == null)
            systemText.text += "⚠️ 有 TMP_Text 元件未綁定\n";

        if (leftFlag == null || rightFlag == null)
            systemText.text += "⚠️ 有 FlagComponent 未綁定\n";

        if (endPanel == null)
            systemText.text += "⚠️ endPanel 未綁定\n";

        try
        {
            Init();
        }
        catch (Exception e)
        {
            systemText.text += "❌ Init 發生錯誤：\n" + e + "\n";
            return;
        }

        StartCoroutine(StartTask());
    }
    
    private IEnumerator waitForGameStart()
    {
        yield return new WaitForSeconds(5);
        yield return null;
    }

    private IEnumerator StartTask()
    {
        yield return StartCoroutine(waitForGameStart());
        
        foreach (var data in currentData)
        {
            if (middleLetter == null || upperLetter == null || bottomLetter == null)
                yield break;

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

            yield return new WaitForSeconds(0.5f);

            middleLetter.text = "";
            upperLetter.text = "";
            bottomLetter.text = "";

            float responseTime = responseTimeLimit[Random.Range(0, responseTimeLimit.Length)];
            float startTime = Time.time;

            bool responded = false;

            while (Time.time - startTime < responseTime)
            {
                bool leftUp = leftFlag != null && leftFlag.isAbove;
                bool rightUp = rightFlag != null && rightFlag.isAbove;

                if (leftUp && rightUp)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false;
                    responded = true;
                    break;
                }

                if (rightUp && !leftUp && middleLetter.color == Color.green)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }

                if (leftUp && !rightUp && middleLetter.color == Color.red)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }

                yield return null;
            }

            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = responseTime;
            }

            if (data.midColor == data.OtherColor)
            {
                data.colorIsSame = true;
            }
            else
            {
                data.colorIsSame = false;
            }

            middleLetter.color = Color.white;
        }

        int totalCount = currentData.Count;
        int correctCount = currentData.Count(d => d.isCorrect);
        float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        float averageResponseTime = currentData.Where(d => d.isCorrect).Any()
            ? currentData.Where(d => d.isCorrect).Average(d => d.responseTime)
            : 0f;

        string resultText = $"✅ 正確率: {correctCount}/{totalCount} ({accuracy:F2}%)\n" +
                            $"⏱️ 平均反應時間（正確題）: {averageResponseTime:F2} 秒\n";
        systemText.text += resultText;
        Debug.Log(resultText);

        if (endPanel != null)
            endPanel.SetActive(true);

        ExportFlankerResultsToCSV();
    }

    public void Init()
    {
        currentData.Clear();
        
        List<string> negativeLatter = new List<string>
        {
            "分屍", "強姦", "屠殺", "凌虐", "自焚", "崩潰", "暴躁", "上吊", "欺騙", "變態",
            "憤怒", "亂倫", "血腥", "暴虐", "溺斃", "狠毒", "砍頭", "詛咒", "發怒", "猥褻",
            "畜生", "怒罵", "殘忍", "驚慄", "咆哮", "悲慟", "喪命", "哭泣", "激怒", "挑釁",
            "傷心", "憎惡", "恐怖", "破產", "悲憤", "憎恨", "悲痛", "焦躁", "淫蕩", "焦慮",
            "雜交", "瘟疫", "陰險", "悲傷", "野蠻", "恥辱", "悽慘", "瘋癲", "反感", "骯髒",
            "敗類", "厭煩", "焦急", "喪事", "心煩", "卑鄙", "出殯", "噁心", "罪孽", "惡劣",
            "下蠱", "災禍", "偏見", "笨蛋", "騙子", "邪惡", "夭折", "虛偽", "厭世", "刻薄",
            "狂傲", "沮喪", "絕望", "貪婪", "淒涼", "悲哀", "卑劣", "陪葬", "苦惱", "嫌惡",
            "錯亂", "畸形", "自卑", "斷氣", "殘廢", "諂媚", "白痴", "罪惡", "短命", "無能",
            "憂傷", "窮困", "輕蔑", "墮落", "憂慮", "蔑視", "醜陋", "膽小", "病態", "腐敗",
            "去勢", "膽怯", "哀悼", "頹廢", "貧乏", "軟弱", "意圖", "懶惰",
        };

        List<string> neutralLatter = new List<string>
        {
            "空地", "默想", "冥想", "段落", "概要", "底下", "前言", "取向", "選取", "字形",
            "厚度", "句子", "配套", "用語", "檢閱", "思量", "屬性", "歸類", "由來", "摘要",
            "主義", "沿途", "額外", "比喻", "時程", "循環", "通往", "預先", "要件", "收取",
            "調節", "隨身", "見解", "演繹", "抽象", "心智", "傾向", "抽取", "考察", "起點",
            "緣故", "提取", "交替", "回顧", "聲稱", "伸直", "換取", "擺設", "調頻", "假定",
            "慰藉", "抽樣", "清高", "備用", "推測", "知覺", "虛擬", "伴隨", "注重", "頭腦",
            "體積", "推論", "商議", "乾燥", "轉速", "隨機", "察覺", "散佈", "評價", "轉彎",
            "務必", "時髦", "斷定", "揣測", "華麗", "上流",
        };

        if (!isTest)
        {
            neutralLatter = neutralLatter.Take(30).ToList();
            negativeLatter = negativeLatter.Take(30).ToList();
        }
        else
        {
            neutralLatter = neutralLatter.Take(3).ToList();
            negativeLatter = negativeLatter.Take(3).ToList();
        }

        
            
        (Color mid, Color other)[] colorCombos = new (Color, Color)[]
        {
            (Color.red, Color.red),
            (Color.green, Color.red),
            (Color.red, Color.green),
            (Color.green, Color.green)
        };

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

        ShuffleList(currentData);
    }

    public void ExportFlankerResultsToCSV()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string path = "/storage/emulated/0/Download/FlankerResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
        string path = Application.dataPath + "/FlankerResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "-" + PlayerPrefs.GetString("ID") +".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Index,Letter,MidColor,OtherColor,IsNegative,IsCorrect,ResponseTime,ColorIsSame");

        int correctCount = 0;
        float totalResponseTime = 0f;
        int totalCount = currentData.Count;

        for (int i = 0; i < currentData.Count; i++)
        {
            var data = currentData[i];
            string midColorStr = ColorToString(data.midColor);
            string otherColorStr = ColorToString(data.OtherColor);

            if (data.isCorrect)
            {
                correctCount++;
                totalResponseTime += data.responseTime;
            }

            csv.AppendLine($"{i},{data.currentLetter},{midColorStr},{otherColorStr},{data.isNegative},{data.isCorrect},{data.responseTime:F3},{data.colorIsSame}");
        }

        float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        float averageRT = correctCount > 0 ? totalResponseTime / correctCount : 0f;

        csv.AppendLine();
        csv.AppendLine($"總題數,{totalCount}");
        csv.AppendLine($"正確題數,{correctCount}");
        csv.AppendLine($"正確率,{accuracy:F2}%");
        csv.AppendLine($"平均反應時間（僅計算正確題）, {averageRT:F3} 秒");

        try
        {
            File.WriteAllText(path, csv.ToString());
            string msg = "✅ Flanker CSV 已儲存至: " + path;
            Debug.Log(msg);
            systemText.text += msg + "\n";
        }
        catch (Exception e)
        {
            string err = "❌ 無法寫入Flanker CSV: " + e.Message;
            Debug.LogError(err);
            systemText.text += err + "\n";
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private string ColorToString(Color c)
    {
        if (c == Color.red) return "Red";
        if (c == Color.green) return "Green";
        if (c == Color.blue) return "Blue";
        if (c == Color.white) return "White";
        if (c == Color.black) return "Black";
        return $"RGBA({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
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
    public Color midColor, OtherColor;
    public bool isNegative;
    public bool isCorrect;
    public bool colorIsSame;
    public float responseTime;
}
