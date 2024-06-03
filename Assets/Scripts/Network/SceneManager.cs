using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : NetworkBehaviourCustom
{

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void LoadANewScene(string sceneName)
    {
        foreach(var item in ConnectedClientsInfo.Values){
            item.sync = false;
        }
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
