using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static NetworkPlayer;

public class NetworkBehaviourCustom : NetworkBehaviour
{
    public static Dictionary<ulong, NetworkPlayer> ConnectedClientsInfo = new Dictionary<ulong, NetworkPlayer>();
    public static CircuitMap actualMap;

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerToClientsListServerRpc(string myName, ulong clientId)
    {
        if (!IsHost)
        {
            return;
        }
        ConnectedClientsInfo.Add(clientId, new NetworkPlayer(clientId, myName));
        UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeMyKartServerRpc(Kart kart, ulong clientId)
    {
        ConnectedClientsInfo[clientId].kart = kart;
        UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
    }

    [ClientRpc(RequireOwnership = false)]
    public void UpdateEveryoneClientListClientRpc(string ConnectedClientsUsernamesServerJSON)
    {
        ConnectedClientsInfo = JsonConvert.DeserializeObject<Dictionary<ulong, NetworkPlayer>>(ConnectedClientsUsernamesServerJSON);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateMyAudioServerRpc(ulong clientId, string playerAudio)
    {

        ConnectedClientsInfo[clientId].networkPlayerAudio = JsonConvert.DeserializeObject<NetworkPlayerAudio>(playerAudio);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AskGameStateSyncServerRpc()
    {
        AckGameStateSyncClientRpc(GameState.gameMode);
        ChangeGameStateMapSyncClientRpc(GameState.selectedCircuit);
    }

    [ClientRpc(RequireOwnership = false)]
    public void AckGameStateSyncClientRpc(GameMode gm)
    {
        GameState.gameMode = gm;
    }

    [ClientRpc(RequireOwnership = false)]
    public void ChangeGameStateMapSyncClientRpc(Circuit cc)
    {
        GameState.selectedCircuit = cc;
    }

    [ServerRpc(RequireOwnership = false)]
    public void IAmSyncServerRpc(ulong clientId)
    {
        ConnectedClientsInfo[clientId].sync = true;
        UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
    }

    public static bool IsEveryoneInSync(){
        foreach(var item in ConnectedClientsInfo.Values){
            if(!item.sync) return false;
        }
        return true;
    }

    public Kart GetClientKart(ulong clientId)
    {
        return ConnectedClientsInfo[clientId].kart;
    }

    public static NetworkPlayer GetNetworkPlayer(ulong clientId)
    {
        return ConnectedClientsInfo[clientId];
    }

    public NetworkObject FindNetworkObjectByTag(string tag)
    {
        foreach (NetworkObject obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (obj.tag != tag) continue;
            return obj;
        }

        throw new Exception("NETWORK OBJECT NOT FOUND!");
    }

    protected void InitiateHost(bool local)
    {
        ConnectedClientsInfo = new();
        NetworkManager.Singleton.StartHost();
        AddPlayerToClientsListServerRpc(PersistenceManager.gameSave.username, NetworkManager.LocalClientId);
        GameState.online = !local;
    }

    protected void safeShutdown(){
         for(int i = 1; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++){
            try{NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.ConnectedClientsIds[i]);}catch{}
            i--;
        }
        try{
            Destroy(GetComponent<NetworkObject>());
            NetworkManager.Singleton.Shutdown();
        }
        catch{}
    }
}
