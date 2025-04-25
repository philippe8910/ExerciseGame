using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneManagerSystem : MonoBehaviour
{
    public  void LoadScene(string sceneName)
    {
        // Load the specified scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
