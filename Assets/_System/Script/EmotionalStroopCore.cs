using System;
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

    public GameObject congruentPrefab, incongruentPrefab, starsArrayPrefab;

    [Header("生成次數設定")]
    public int type1TrialCount = 5;       
    public int type2TrialCount = 5;       
    public int type3TrialCount = 5;       

    [Header("其他參數")]
    public float timeInterval = 2.0f;  // 顯示情緒圖片的間隔時間
    
    [Header("題目List")]
    public List<StroopData> currentTrialList = new List<StroopData>(); // 當前試次的題目列表

    private void Start()
    {
        Init();
        StartCoroutine(GameStart());
    }

    public void Init()
    {
        for (int i = 0; i < type1TrialCount; i++)
        {
            StroopData data = new StroopData();
            data.type = StroopType.Congruent;
            currentTrialList.Add(data);
        }
        
        for (int i = 0; i < type2TrialCount; i++)
        {
            StroopData data = new StroopData();
            data.type = StroopType.Incongruent;
            currentTrialList.Add(data);
        }
        
        for (int i = 0; i < type3TrialCount; i++)
        {
            StroopData data = new StroopData();
            data.type = StroopType.StarsArray;
            currentTrialList.Add(data);
        }
    }

    public IEnumerator GameStart()
    {
        foreach (var data in currentTrialList)
        {
            yield return new WaitForSeconds(timeInterval);

            GameObject g = null;

            switch (data.type)
            {
                case StroopType.Congruent:
                    g = Instantiate(congruentPrefab, iconContainer.transform);
                    break;
                case StroopType.Incongruent:
                    g = Instantiate(incongruentPrefab, iconContainer.transform);
                    break;
                case StroopType.StarsArray:
                    g = Instantiate(starsArrayPrefab, iconContainer.transform);
                    break;
            }

            g.transform.localPosition = new Vector3(0, 0.25f, 0);
            g.transform.localScale = new Vector3(1, 1, 1);
            g.transform.localRotation = Quaternion.Euler(0,0,0);

            float startTime = Time.time;
            bool responded = false;

            // 非阻塞式等待
            while (Time.time - startTime < timeInterval)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }
                
                int correctCount = -1;

                switch (data.type)
                {
                    case StroopType.Congruent:
                        correctCount = g.GetComponent<NumBackground>().enableNumber;
                        break;
                    case StroopType.Incongruent:
                        correctCount = g.GetComponent<RandomNumBackground>().enableNumber;
                        break;
                    case StroopType.StarsArray:
                        correctCount = g.GetComponent<RandomStarBackground>().enabledCount;
                        break;
                }
                yield return null; // 👉 讓 Unity 可以繼續執行下一幀
            }

            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = timeInterval; // 或者設為 -1 表示沒反應
            }

            Destroy(g);
        }

        Debug.Log("✅ 全部題目完成！");
    }

}

public enum StroopType
{
    Congruent, // 題型1
    Incongruent, // 題型2
    StarsArray  // 題型3
}

[System.Serializable]
public class StroopData
{
    public StroopType type; // 題型
    public bool isCorrect; // 是否正確
    public float responseTime; // 回應時間
}
