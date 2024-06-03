using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : NetworkBehaviourCustom
{
    public int nextCheckpointSingleIndex = 0;
    public int actualLap;
    public float currentTime;
    public int position;
    public bool timerActive;
    private PlayerUI playerUI;
    public List<float> recordedTimes = new List<float>();
    public CameraCar camera;
    private CircuitManager circuitManager;
    private TrackCheckpoints trackCheckpoints;
    public bool finishedRace;
    public CarController carController;
    private PlayerInputActions inputActions;

    private float damageTimer = 0;
    public bool doDamage = false;
    public bool menuOpen = false;
    private Quaternion rotationBeforeDamage;
    public Vector3 rotationSpeed = new Vector3(0, 90, 0);

    public AudioListener audioListener;

    ulong closestKartId = 999;

    private void Start()
    {

        carController.player = this;
        timerActive = true;
        actualLap = 1;
        currentTime = 0f;
        playerUI = GetComponent<PlayerUI>();

        if(OwnerClientId != NetworkManager.Singleton.LocalClientId){
            playerUI.canvas.gameObject.SetActive(false);
            Destroy(audioListener);
            camera.gameObject.SetActive(false);
            return;
        }

        inputActions = new();
        inputActions.Race.Enable();
        inputActions.Menu.Enable();

        inputActions.Race.useItem.performed += UseItem_performed;
        inputActions.Race.Debug.performed += Debug_performed;
        inputActions.Menu.Back.performed += back_performed;
        inputActions.Menu.Submit.performed += Submit_performed;

        NetworkObject circuitManagerNObj = FindNetworkObjectByTag("CIRCUIT_MANAGER");
        circuitManager = circuitManagerNObj.GetComponent<CircuitManager>();
        trackCheckpoints = circuitManagerNObj.GetComponent<TrackCheckpoints>();

        playerUI.canvas.gameObject.SetActive(true);
    }

    private void back_performed(InputAction.CallbackContext obj)
    {
        menuOpen = !menuOpen;
        playerUI.menuOverlay.SetActive(menuOpen);
    }

    private void Debug_performed(InputAction.CallbackContext obj)
    {
        //ExecuteDamage();
        //IncrementLap();
    }

    public void ExecuteDamage(){
        damageTimer = 0;
        rotationBeforeDamage = carController.carModelTransform.localRotation;
        doDamage = true;
    }

    private void TakeDamage()
    {
        if(damageTimer < 2f){
            carController.carModelTransform.Rotate(0, 0, 450f * Time.deltaTime);
            return;
        }

        doDamage = false;
        carController.carModelTransform.localRotation = rotationBeforeDamage;
    }

    private void UseItem_performed(InputAction.CallbackContext obj)
    {
        NetworkPlayer networkPlayer = GetNetworkPlayer(NetworkManager.Singleton.LocalClientId);
        if (networkPlayer.item == NetworkPlayer.Item.NO_ITEM) return;

        circuitManager.UseItemServerRpc(NetworkManager.Singleton.LocalClientId,
         carController.transform.position.x,
          carController.transform.position.y,
           carController.transform.position.z,
            carController.transform.rotation.eulerAngles.x, 
            carController.transform.rotation.eulerAngles.y,
            carController.transform.rotation.eulerAngles.z,
            closestKartId);
    }

    private void Update()
    {
        if(OwnerClientId != NetworkManager.Singleton.LocalClientId){ return; }
        carController.locked = CircuitManager.contadorInicio > 0;

        if (timerActive)
        {
            currentTime +=  Time.deltaTime;
        }

        if(doDamage){
            damageTimer += Time.deltaTime;
            TakeDamage();
        }

        if(finishedRace) return;

        GetClosestKart();

        UpdateMyAudioServerRpc(NetworkManager.Singleton.LocalClientId, JsonConvert.SerializeObject(carController.networkPlayerAudio));
    }

    private void LateUpdate()
    {
        if(OwnerClientId != NetworkManager.Singleton.LocalClientId){ return; }
        UpdatePositionData();
        var ppd = CircuitManager.playerPositionDataDictonary[NetworkManager.Singleton.LocalClientId];
        position = ppd.rank;

        if(GameMode.BATTLE == GameState.gameMode && !finishedRace){
            if(ppd.lifes <= 0 || IsEveryoneExceptMeDead()){
                inputActions.Menu.Back.performed -= back_performed;
                menuOpen = false;
                playerUI.menuOverlay.SetActive(false);

                Destroy(carController.gameObject);
                circuitManager.FinishCircuitServerRpc(NetworkManager.LocalClientId);
                finishedRace = true;
            }
        }
    }

    private bool IsEveryoneExceptMeDead(){
        foreach(var item in CircuitManager.playerPositionDataDictonary.Values){
            if(item.clientId == NetworkManager.Singleton.LocalClientId) continue;
            if(!item.finishedRace) return false;
        }

        return true;
    }

    public void IncrementLap()
    {
        recordedTimes.Add(currentTime);
        if (actualLap < circuitManager.lapCount)
        {
            playerUI.RecordNewLapTime();
        }
        else
        {
            inputActions.Menu.Back.performed -= back_performed;
            menuOpen = false;
            playerUI.menuOverlay.SetActive(false);

            Destroy(carController.gameObject);
            circuitManager.FinishCircuitServerRpc(NetworkManager.LocalClientId);
            finishedRace = true;

            float menorTempo = recordedTimes.Min();
            PersistenceManager.AddNewBestTime(circuitManager.circuit, menorTempo);
        }
        actualLap++;
        currentTime = 0;
    }

    private void Submit_performed(InputAction.CallbackContext obj)
    {

        if(!menuOpen && !finishedRace) return;

        Debug.Log("ONLINE: " + GameState.online);

        if((NetworkManager.Singleton.IsHost && !GameState.online) || menuOpen){
            finishedRace = true;
            safeShutdown();
            MenuManager.initWithScreen = MenuManager.ScreenIdEnum.MAIN_MENU;
            NetworkManagerCustom.matchStarted = false;
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
        }
        else if(NetworkManager.Singleton.IsHost){
            if(CircuitManager.HasEveryoneFinishedRace()){
                finishedRace = true;
                foreach(var item in ConnectedClientsInfo.Values){
                    item.points += CircuitManager.playerPositionDataDictonary[item.clientId].points;
                }
                OnDestroy();
                MenuManager.initWithScreen = MenuManager.ScreenIdEnum.LOBBY;
                NetworkManagerCustom.matchStarted = false;
                SceneLoader.LoadANewScene("menu");
            }
        }
    }

    void OnDestroy(){
        try{inputActions.Menu.Submit.performed -= Submit_performed;}catch{}
        try{inputActions.Race.Debug.performed -= Debug_performed;}catch{}
        try{inputActions.Menu.Back.performed -= back_performed;}catch{}
    }

    public void GetClosestKart(){
        NetworkPlayer networkPlayer = GetNetworkPlayer(this.OwnerClientId);
        NetworkObject closestKart = null;

        List<NetworkObject> kartList = new();

        foreach(NetworkObject nO in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList){
            if(nO.tag != "Player") continue;
            if(nO.OwnerClientId == NetworkManager.Singleton.LocalClientId) continue;

            kartList.Add(nO);
        }

        foreach(NetworkObject player in kartList){
             if(closestKart == null){
                closestKart = player;
                continue;
            }

            GameObject kart = GetFirstActiveChildWithTag(player, "Kart");

            float distanceToA = Vector3.Distance(carController.gameObject.transform.position, closestKart.transform.position);
            float distanceToB = Vector3.Distance(carController.gameObject.transform.position, kart.transform.position);

            Vector3 direction = carController.gameObject.transform.position - kart.transform.position;
            float dotProduct = Vector3.Dot(direction.normalized, carController.gameObject.transform.forward);

            if(dotProduct < 0 && Mathf.Abs(distanceToB) < Mathf.Abs(distanceToA)){
                GameObject child = FindChildWithTag(closestKart.gameObject, "Missel_Target");
                child?.SetActive(false);
                closestKart = player;
            }
            else{
                GameObject child = FindChildWithTag(kart.gameObject, "Missel_Target");
                child?.SetActive(false);
            }
        }

        if(closestKart == null) {
            closestKartId = 999;
            return;
        }
        GameObject closestObj = GetFirstActiveChildWithTag(closestKart, "Kart");
        Vector3 direction2 = carController.gameObject.transform.position - closestObj.transform.position;
        float dotProduct2 = Vector3.Dot(direction2.normalized, carController.gameObject.transform.forward);
        FindChildWithTag(closestObj.gameObject, "Missel_Target")?.SetActive(dotProduct2 < 0 && networkPlayer.item == NetworkPlayer.Item.MISSILE);

        closestKartId = dotProduct2 < 0 ? closestKart.OwnerClientId : 999;
    }

    GameObject GetFirstActiveChildWithTag(NetworkObject parent, string tag)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.activeSelf && child.CompareTag(tag))
            {
                return child.gameObject;
            }
        }

        return null; 
    }

    GameObject FindChildWithTag(GameObject parent, string tag) {
       GameObject child = null;
     
       foreach(Transform transform in parent.transform) {
          if(transform.CompareTag(tag)) {
             child = transform.gameObject;
             break;
          }
       }
     
       return child;
    }


    private void UpdatePositionData()
    {
        if(finishedRace) return;
        PlayerPositionData playerPosition = new(NetworkManager.LocalClientId)
        {
            nextWaypoint = nextCheckpointSingleIndex,
            lap = actualLap,
            distanceToNextWaypoint = GameMode.BATTLE != GameState.gameMode ? Vector3.Distance(camera.carTarget.position, trackCheckpoints.checkpointSingleList[nextCheckpointSingleIndex].transform.position) : 0
        };
        circuitManager.UpdateMyPositionDataServerRpc(NetworkManager.LocalClientId, JsonConvert.SerializeObject(playerPosition));
    }
}
