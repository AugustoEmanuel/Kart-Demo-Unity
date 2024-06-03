using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using static NetworkPlayer;

public class PlayerPositionData
{
    public ulong clientId;
    public int lap;
    public int nextWaypoint;
    public float distanceToNextWaypoint;

    public int points;

    public int lifes = 3;

    public int rank;

    public bool finishedRace = false;

    public PlayerPositionData(ulong clientId)
    {
        this.clientId = clientId;
    }
}

public class PlayerComparer : IComparer<PlayerPositionData>
{
    public int Compare(PlayerPositionData x, PlayerPositionData y)
    {
        if (x.lap != y.lap)
        {
            return y.lap.CompareTo(x.lap);
        }

        if (y.nextWaypoint != x.nextWaypoint)
        {
            return y.nextWaypoint.CompareTo(x.nextWaypoint);
        }

        if (x.distanceToNextWaypoint != y.distanceToNextWaypoint)
        {
            return x.distanceToNextWaypoint.CompareTo(y.distanceToNextWaypoint);
        }

        return y.points.CompareTo(x.points);
    }
}

public class CircuitManager : NetworkBehaviourCustom
{
    [Serializable]
    private struct KartNetworkPrefab
    {
        public Kart kart;
        public NetworkObject networkPrefab;
    };

    public static Dictionary<ulong, PlayerPositionData> playerPositionDataDictonary = new();

    public List<Transform> playersStartPosition = new List<Transform>();
    public int lapCount = 3;
    public NetworkObject playerPreFab;
    public NetworkObject misselPreFab;
    public NetworkObject minaPreFab;


    public Circuit circuit;

    [SerializeField] private List<KartNetworkPrefab> carPreFab;
    private List<NetworkObject> playersCars = new();
    private static readonly object _lock = new object();
    private float tickTimer = 0;

    public bool isArena = false;

    public static float contadorInicio = 3;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        contadorInicio = 3;
        IAmSyncServerRpc(NetworkManager.Singleton.LocalClientId);
        if (!IsHost) return;

        NetworkManagerCustom.matchStarted = true;
        playerPositionDataDictonary.Clear();
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
        {
            ulong clientId = NetworkManager.Singleton.ConnectedClientsIds[i];
            NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(playerPreFab, clientId, true, true, false, playersStartPosition[i].position, playersStartPosition[i].rotation);

            playerPositionDataDictonary.Add(clientId, new(clientId));
        }

        UpdateEveryonePositionsClientRpc(JsonConvert.SerializeObject(playerPositionDataDictonary));
    }

    [ClientRpc]
    public void UpdateEveryonePositionsClientRpc(string playerPositionListJSON) {
        if(IsHost) return;
        lock(_lock){
            playerPositionDataDictonary = JsonConvert.DeserializeObject<Dictionary<ulong, PlayerPositionData>>(playerPositionListJSON);
        }
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        tickTimer += Time.deltaTime;

        if(!IsEveryoneInSync()) return;
        
        if(contadorInicio > 0){
            contadorInicio -= Time.deltaTime;
            if (tickTimer > 0.05f || contadorInicio <= 0){
                UpdateContadorInicioClientRpc(contadorInicio);
                tickTimer = 0f;
            }
            return;
        }
        UpdateRaceStates();
        DeterminePlayerRankings();

        if (tickTimer > 0.05f)
        {
            UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
            UpdateEveryonePositionsClientRpc(JsonConvert.SerializeObject(playerPositionDataDictonary));
            tickTimer = 0f;
        }
        foreach(PlayerPositionData playerPositionData in playerPositionDataDictonary.Values)
        {
            Debug.Log("CLIENT " + playerPositionData.clientId + " | LAP: " + playerPositionData.lap + " | NEXTCHK: " + playerPositionData.nextWaypoint + " | DNEXTCHK: " + playerPositionData.distanceToNextWaypoint);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void UpdateContadorInicioClientRpc(float contador)
    {
        contadorInicio = contador;
    }

    public static void AddPointsToPlayer(ulong clientId){
        lock(_lock){
            Debug.Log("ADDING POINT TO PLAYER "+ clientId);
            playerPositionDataDictonary[clientId].points += 1;
            Debug.Log(playerPositionDataDictonary[clientId].points);
        }
    }

    public static void TakeDamage(ulong clientId){
        lock(_lock){
            playerPositionDataDictonary[clientId].lifes -= 1;
            if(playerPositionDataDictonary[clientId].lifes < 0) playerPositionDataDictonary[clientId].lifes = 0;
        }
    }

    public static bool HasEveryoneFinishedRace(){
        foreach(var item in playerPositionDataDictonary.Values){
            if(!item.finishedRace) return false;
        }

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void FinishCircuitServerRpc(ulong clientId)
    {
        lock(_lock){
            playerPositionDataDictonary[clientId].finishedRace = true;
        }
        UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
    }

    private void UpdateRaceStates()
    {
        foreach (var player in ConnectedClientsInfo.Values)
        {
            if(player.itemTimer > 3f)
            {
                player.item = GetRandomEnumValue<Item>(0);
                //player.item = Item.MINA;
            }

            if(player.item == Item.NO_ITEM)
            {
                player.itemTimer += Time.deltaTime;
            }
            else
            {
                player.itemTimer = 0;
            }

            if (player.appliedBoost)
            {
                player.boostTimer += Time.deltaTime;
                if (player.boostTimer > 1f)
                {
                    player.appliedBoost = false;
                }
            }

            if(player.shieldActive){
                player.shieldTimer += Time.deltaTime;
                if (player.shieldTimer > 4f)
                {
                    player.shieldActive = false;
                }
            }

            //Debug.Log("CLIENT: " + player.clientId + " | ITEM_TIME: " + player.itemTimer + " | ITEM:" + player.item);
        }
    }

    public static T GetRandomEnumValue<T>(int removeAt = -1) where T : Enum
    {
        var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();

        if (removeAt != -1)
        {
            enumValues.RemoveAt(removeAt);
        }
        return enumValues[new System.Random().Next(0, enumValues.Count)];
    }

    private NetworkObject getKartPrefab(Kart kart)
    {
        foreach (KartNetworkPrefab item in carPreFab)
        {
            if (item.kart == kart) return item.networkPrefab;
        }
        throw new Exception("KART NOT PRESENT ON CIRCUIT MANAGER PREFAB LIST");
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateMyPositionDataServerRpc(ulong playerId, string playerPositionJSON)
    {
        PlayerPositionData ppd = JsonConvert.DeserializeObject<PlayerPositionData>(playerPositionJSON);

        lock (_lock)
        {
            ppd.points = playerPositionDataDictonary[playerId].points;
            ppd.rank = playerPositionDataDictonary[playerId].rank;
            ppd.finishedRace = playerPositionDataDictonary[playerId].finishedRace;
            ppd.lifes = playerPositionDataDictonary[playerId].lifes;
            playerPositionDataDictonary[playerId] = ppd;
        }
    }

    public void DeterminePlayerRankings()
{
    List<PlayerPositionData> players = new(CircuitManager.playerPositionDataDictonary.Values);
    players.Sort(new PlayerComparer());

    // Create a dictionary to store the ranks
    Dictionary<ulong, int> playerRanks = new();

    lock(_lock){

        if(GameState.gameMode == GameMode.BATTLE){
            int rank = 1;
            for (int i = 0; i < players.Count; i++)
            {
                playerPositionDataDictonary[players[i].clientId].rank = rank;
                
                // Increment rank for the next player (unless there's a tie)
                if (i < players.Count - 1 && players[i].points != players[i + 1].points)
                {
                    rank = i + 2;
                }
            }
        }
        else{
            for (int i = 0; i < players.Count; i++){
                playerPositionDataDictonary[players[i].clientId].rank = i + 1;
            }
        }
    }
}

    [ServerRpc(RequireOwnership = false)]
    public void UseItemServerRpc(ulong clientId, float px, float py, float pz, float rx, float ry, float rz, ulong target)
    {

        Debug.Log("RUNNING");

        Item item = ConnectedClientsInfo[clientId].item;

        ConnectedClientsInfo[clientId].item = Item.NO_ITEM;

        Vector3 playerPosition = new Vector3(px, py, pz);
        Quaternion playerRotation = Quaternion.Euler(rx, ry, rz);


        switch (item)
        {
            case Item.BOOST:
                ConnectedClientsInfo[clientId].boostTimer = 0;
                ConnectedClientsInfo[clientId].appliedBoost = true;
                break;
            case Item.MISSILE:
                SpawnMissel(clientId, playerPosition, playerRotation, target, false);
                break;
            case Item.MISSILE_NO_LOCK:
                SpawnMissel(clientId, playerPosition, playerRotation, 999, true);
                break;
            case Item.SHIELD:
                ConnectedClientsInfo[clientId].shieldTimer = 0;
                ConnectedClientsInfo[clientId].shieldActive = true;
                break;
            case Item.MINA:
                SpawnMina(clientId, playerPosition);
                break;
        }
        UpdateEveryoneClientListClientRpc(JsonConvert.SerializeObject(ConnectedClientsInfo));
    }

    private void SpawnMissel(ulong playerId, Vector3 playerPosition, Quaternion playerRotation, ulong target, bool alternateModel)
    {
        Vector3 misselPos = new(playerPosition.x, playerPosition.y, playerPosition.z);
        NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(misselPreFab, 0, true, true, false, misselPos, Quaternion.Euler(0, playerRotation.eulerAngles.y, 0));
        networkObject.GetComponent<Missel>().nextCheckpointSingleIndex = playerPositionDataDictonary[playerId].nextWaypoint;
        networkObject.GetComponent<Missel>().targetId = target;
        networkObject.GetComponent<Missel>().ownerId = playerId;
        networkObject.GetComponent<Missel>().alternateModel.Value = alternateModel;
    }

    private void SpawnMina(ulong playerId, Vector3 playerPosition)
    {
        Vector3 minaPos = new(playerPosition.x, playerPosition.y, playerPosition.z);
        Debug.Log(minaPreFab);
        NetworkObject networkObject = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(minaPreFab, 0, true, true, false, minaPos, Quaternion.Euler(Vector3.zero));
        networkObject.GetComponent<Mina>().ownerId = playerId;
    }
}
