using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class RandomNumBackground : MonoBehaviour , IData
{
    public List<GameObject> numList;
    public int enableNumber; // 👉 這邊記錄打開的數量
    
    private void Start()
    {
        RandomStarEnable(Random.Range(1, 5));
    }

    [Button]
    public void RandomStarEnable(int count)
    {
        // 全部關閉
        numList.ForEach(num => num.SetActive(false));
        string randomNum = Random.Range(1, 5).ToString();

        count = Mathf.Clamp(count, 1, 5);

        // 隨機打開 count 個
        var selectedStars = numList.OrderBy(_ => Random.value).Take(count).ToList();
        selectedStars.ForEach(star => star.SetActive(true));
        selectedStars.ForEach(text => text.GetComponent<TMP_Text>().text = randomNum);

        // 記錄實際打開的數量
        enableNumber = selectedStars.Count;
    }
}
