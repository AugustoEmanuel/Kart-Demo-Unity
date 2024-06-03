using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NetworkPlayer;

public class PlayerLobby : MonoBehaviour
{
    [Serializable]
    struct KartLobby
    {
        public Kart kart;
        public GameObject obj;
    }

    [SerializeField] private List<KartLobby> lobbyKarts = new();
    [SerializeField] private GameObject kartsGameObject;
    [SerializeField] private TextMeshProUGUI playerNameTxt;


    void Start()
    {

    }

    void Update()
    {
        kartsGameObject.transform.rotation = Quaternion.Euler(kartsGameObject.transform.rotation.eulerAngles + (23 * Time.deltaTime * Vector3.up));
    }

    public void SetPlayerAndActive(string name, string points, Kart kart)
    {
        playerNameTxt.text = name;

        if(points != null){
            playerNameTxt.text += "\nPOINTS: " + points;
        }

        this.gameObject.SetActive(true);
        foreach (var k in lobbyKarts)
        {
            k.obj.SetActive(k.kart == kart);
        }
    }
}
