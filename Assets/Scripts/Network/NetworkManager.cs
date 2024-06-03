using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NetworkManagerCustom;

public class NetworkManagerCustom : NetworkManager
{

    private static bool callbacksSet = false;
    public static bool matchStarted = false;

    private void Start()
    {
        if(!callbacksSet){
            Singleton.ConnectionApprovalCallback += Singleton_ApprovalCheck;
            Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
            Singleton.OnTransportFailure += Singleton_OnTransportFailure;
            callbacksSet = true;
        }
    }

    private void Singleton_OnTransportFailure()
    {
        MenuManager.alertaText = "OOPS... CONNECTION ERROR";
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "menu"){
            try{
                NetworkManager.Singleton.Shutdown();
            }
            catch{}
            MenuManager.initWithScreen = MenuManager.ScreenIdEnum.MAIN_MENU;
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
        }
    }

    private void Singleton_ApprovalCheck(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
    {
        if(ConnectedClientsIds.Count >= 4)
        {
            response.Approved = false;
            response.Reason = "LOBBY IS ALREADY FULL (MAX CAPACITY IS 4 PLAYERS)";
        }
        else if(matchStarted){
            response.Approved = false;
            response.Reason = "ALREADY IN-GAME";
        }
        response.Approved = true;
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj)
    {
        if(!IsHost)
        {
            GameState.online = false;
            if(DisconnectReason != string.Empty){
                MenuManager.alertaText = $"CONNECTION REFUSED REASON: {DisconnectReason}";
            }
            else{
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "menu"){
                    UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
                    try{
                        Singleton.Shutdown();
                    }
                    catch{}

                    MenuManager.alertaText = "OOPS... CONNECTION ERROR";
                    MenuManager.initWithScreen = MenuManager.ScreenIdEnum.MAIN_MENU;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
                }
            }
        }
        else{
            NetworkBehaviourCustom.ConnectedClientsInfo.Remove(obj);
            CircuitManager.playerPositionDataDictonary.Remove(obj);
        }
    }
}
