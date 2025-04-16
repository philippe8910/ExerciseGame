using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomStarBackground : MonoBehaviour , IData
{
    public List<GameObject> starList;
    public int enabledCount; // 👉 這邊記錄打開的數量

    private void Start()
    {
        RandomStarEnable(Random.Range(1, 5));
    }

    [Button]
    public void RandomStarEnable(int count)
    {
        // 全部關閉
        starList.ForEach(star => star.SetActive(false));

        // 隨機打開 count 個
        var selectedStars = starList.OrderBy(_ => Random.value).Take(count).ToList();
        selectedStars.ForEach(star => star.SetActive(true));

        // 記錄實際打開的數量
        enabledCount = selectedStars.Count;
    }

}
