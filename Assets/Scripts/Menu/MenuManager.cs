using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static NetworkPlayer;

/* MAIOR GO HORSE AT� AGORA, MEU DEUS ISSO DEVERIA SER UMA CLASSE SEPARADA PARA TELA, MAS N�O DA, EU TO PROGRAMANDO ISSO AS 3 HORAS DA MANH�, E TENHO QUE ACORDAR AS 7, SOCORRO VOU ME MATAR!!!!!!!!!!!!!!! */

public class MenuManager : NetworkBehaviourCustom
{
    public enum ScreenIdEnum
    {
        START_SCREEN,
        MAIN_MENU,
        MULTIPLAYER_SELECTION,
        MODE_SELECTION,
        LOBBY,
        CLIENT_CONNECTION,
        KART_SELECTION,
        MAP_SELECTION,
        FIRST_EXECUTION_SCREEN,
        NO_SCREEN,
        ERROR_SCREEN
    }

    [Serializable]
    public struct ScreenInfo
    {
        public ScreenIdEnum screenId;
        public Canvas canvas;
    }

    [Serializable]
    public struct MapSelection
    {
        public GameObject map;
        public Circuit circuit;
    }

    [Serializable]
    public struct CircuitImage
    {
        public RawImage image;
        public Circuit circuit;
    }

    public static ScreenIdEnum initWithScreen = ScreenIdEnum.NO_SCREEN;

    private ScreenInfo actualGameScreen;
    [SerializeField] private List<ScreenInfo> screenInfos = new();

    [Header("START SCREEN")]
    [SerializeField] private TextMeshProUGUI pressStartToPlayTxt;
    private float pressStartToPlayTimer = 0f;

    [Header("MAIN MENU SCREEN")]
    [SerializeField] private ButtonCustom singleplayerBtn;
    [SerializeField] private ButtonCustom multiplayerBtn;
    [SerializeField] private ButtonCustom optionsBtn;
    [SerializeField] private ButtonCustom exitBtn;

    [Header("MULTIPLAYER SCREEN")]
    [SerializeField] private ButtonCustom hostBtn;
    [SerializeField] private ButtonCustom connectBtn;

    [Header("SINGLEPLAYER SCREEN")]
    [SerializeField] private ButtonCustom btnTimeTrial;
    [SerializeField] private ButtonCustom btnRace;
    [SerializeField] private ButtonCustom btnBattle;

    [Header("LOBBY SCREEN")]
    [SerializeField] private TextMeshProUGUI connectedPlayersTxt;
    [SerializeField] private TextMeshProUGUI selectedModeLobby;
    [SerializeField] private TextMeshProUGUI selectedMapLobby;
    [SerializeField] private List<GameObject> playerLobbyList;
    [SerializeField] private GameObject startRaceBtn;
    [SerializeField] private GameObject changeMapBtn;

    [Header("CLIENT CONNECTION SCREEN")]
    [SerializeField] private TMP_InputField hostIpIpt;
    [SerializeField] private ButtonCustom connectHostBtn;

    [Header("KART SELECTION SCREEN")]
    [SerializeField] private TextMeshProUGUI kartName;
    [SerializeField] private GameObject kartSelection;

    [Header("FIRST EXECUTION SCREEN")]
    [SerializeField] private TMP_InputField nicknameTxt;

    [Header("MAP SELECTION SCREEN")]
    [SerializeField] private TextMeshProUGUI mapName;
    [SerializeField] private List<MapSelection> mapSelectionList;
    [SerializeField] private List<CircuitImage> circuitMapSelectionList;
    [SerializeField] private TextMeshProUGUI bestTime;

    [Header("SOUND EFFECTS")]
    private AudioSource source;
    [SerializeField] private AudioClip audioMenuBack;

    private PlayerInputActions inputActions;
    [SerializeField] private LoadingIcon loadingIcon;

    //NETWORK
    private delegate void MenuManagerDelegate();
    private event MenuManagerDelegate CallbackDelegate;
    private SceneLoader sceneLoader = new();
    private float connectionTimeout = 0f;
    private bool multiplayerSelected = false;

    //FX BG
    private float fxbg_time = 1f;
    private float fxbg_delay = 5f;
    private float fxbg_delay_time = 1.5f;
    private int fxbg_imgIndex = 0;

    //ALERT
    [SerializeField] private GameObject alertGameObject;
    [SerializeField] private TextMeshProUGUI alertTextObj;

    public static string alertaText;

    // Start is called before the first frame update
    void Start()
    {
        inputActions = new();
        inputActions.Menu.Enable();
        source = GetComponent<AudioSource>();

        GameSave game = PersistenceManager.LoadPlayerData();
        if(game == null)
        {
            ChangeMenuScreen(ScreenIdEnum.FIRST_EXECUTION_SCREEN, true);
        }
        else if(initWithScreen != ScreenIdEnum.NO_SCREEN)
        {
            ChangeMenuScreen(initWithScreen, true);
        }
        else if(NetworkManager.Singleton.IsClient){
            ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
        }
        else
        {
            ChangeMenuScreen(ScreenIdEnum.START_SCREEN, true);
        }

        initWithScreen = ScreenIdEnum.NO_SCREEN;

        InitMultiplayerActions();
        InitMainMenuActions();
        InitLobbyActions();
        InitModeSelectionActions();

        inputActions.Menu.Submit.performed += Submit_performed;
        inputActions.Menu.Back.performed += Back_performed;
        inputActions.Menu.Left.performed += Left_performed;
        inputActions.Menu.Right.performed += Right_performed;
        inputActions.Menu.Extra.performed += Extra_performed;
        inputActions.Menu.Map.performed += Map_performed;

        GameState.coldStart = false;
    }

    private void Submit_performed(InputAction.CallbackContext obj)
    {
        if(alertaText != null && alertaText != ""){
            alertaText = "";
            return;
        }

        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.LOBBY:
                if(NetworkManager.Singleton.IsHost  && NetworkManager.Singleton.ConnectedClientsList.Count > 1){
                    SceneLoader.LoadANewScene(GameState.selectedCircuit.GetSceneName());
                }
                break;
            case ScreenIdEnum.KART_SELECTION:
                if (multiplayerSelected || GameState.online)
                {
                    ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
                }
                else
                {
                    SceneLoader.LoadANewScene(GameState.selectedCircuit.GetSceneName());
                }
                break;
            case ScreenIdEnum.MAP_SELECTION:
                if (!multiplayerSelected) InitiateHost(!multiplayerSelected);

                if (multiplayerSelected)
                {
                    ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
                }
                else
                {
                    ChangeMenuScreen(ScreenIdEnum.KART_SELECTION, true);
                }
                break;
            case ScreenIdEnum.FIRST_EXECUTION_SCREEN:
                if (nicknameTxt.text.Length <= 0) return;
                PersistenceManager.CreateNewSaveData(nicknameTxt.text);
                ChangeMenuScreen(ScreenIdEnum.START_SCREEN, true);
                break;
            case ScreenIdEnum.CLIENT_CONNECTION:
                NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = hostIpIpt.text.Trim();
                ConnectClient();
                break;

        }
    }

    private void Extra_performed(InputAction.CallbackContext obj)
    {
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.LOBBY: // KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK MISERICORDIA
                ChangeMenuScreen(ScreenIdEnum.KART_SELECTION, true);
                break;
        }
    }

    private void Map_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.LOBBY: // KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK MISERICORDIA
                if(!IsHost) return;
                ChangeMenuScreen(ScreenIdEnum.MAP_SELECTION, true);
                break;
        }
    }

    private void Right_performed(InputAction.CallbackContext obj)
    {
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.KART_SELECTION: // KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK MISERICORDIA
                Kart selectedKart = GetClientKart(NetworkManager.Singleton.LocalClientId);
                selectedKart = (Kart)(((int)selectedKart + 1) % Enum.GetValues(typeof(Kart)).Length);
                ChangeMyKartServerRpc(selectedKart, NetworkManager.Singleton.LocalClientId);
                kartSelection.GetComponent<PlayerLobby>().SetPlayerAndActive("", null, selectedKart);
                break;
            case ScreenIdEnum.MAP_SELECTION:
                if(GameState.gameMode == GameMode.BATTLE) return;
                GameState.selectedCircuit = (Circuit)(((int)GameState.selectedCircuit + 1) % (Enum.GetValues(typeof(Circuit)).Length - 1));
                ChangeGameStateMapSyncClientRpc(GameState.selectedCircuit);
                break;
        }
    }

    private void Left_performed(InputAction.CallbackContext obj)
    {
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.KART_SELECTION:
                Kart selectedKart = GetClientKart(NetworkManager.Singleton.LocalClientId);
                selectedKart = (Kart)(((int)selectedKart - 1 + Enum.GetValues(typeof(Kart)).Length) % Enum.GetValues(typeof(Kart)).Length);
                ChangeMyKartServerRpc(selectedKart, NetworkManager.Singleton.LocalClientId);
                kartSelection.GetComponent<PlayerLobby>().SetPlayerAndActive("", null, selectedKart);
                break;
            case ScreenIdEnum.MAP_SELECTION:
                if(GameState.gameMode == GameMode.BATTLE) return;
                GameState.selectedCircuit = (Circuit)(((int)GameState.selectedCircuit - 1 + (Enum.GetValues(typeof(Circuit)).Length - 1)) % (Enum.GetValues(typeof(Circuit)).Length - 1));
                ChangeGameStateMapSyncClientRpc(GameState.selectedCircuit);
                break;
        }
    }

    private void Back_performed(InputAction.CallbackContext obj)
    {
        if(alertaText != null && alertaText != ""){
            alertaText = "";
            return;
        }

        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.MULTIPLAYER_SELECTION:
            case ScreenIdEnum.MODE_SELECTION:
                ChangeMenuScreen(ScreenIdEnum.MAIN_MENU, false);
                break;
            case ScreenIdEnum.LOBBY:
                Destroy(GetComponent<NetworkObject>());
                safeShutdown();
                ChangeMenuScreen(ScreenIdEnum.MULTIPLAYER_SELECTION, false);
                break;
            case ScreenIdEnum.CLIENT_CONNECTION:
                ChangeMenuScreen(ScreenIdEnum.MULTIPLAYER_SELECTION, false);
                break;
        }
    }

    private void BackgroundImageFade()
    {
        if (actualGameScreen.screenId == ScreenIdEnum.MAP_SELECTION)
        {
            foreach (var item in mapSelectionList)
            {
                item.map.GetComponent<RawImage>().color = Color.white;
            }
            return;
        }

        foreach (var item in mapSelectionList)
        {
            item.map.SetActive(true);
        }

        if(fxbg_delay < fxbg_delay_time)
        {
            fxbg_delay += Time.deltaTime;
            return;
        }

        int nextImageIndex = (fxbg_imgIndex + 1) % mapSelectionList.Count;

        var actualImageColor = mapSelectionList[fxbg_imgIndex].map.GetComponent<RawImage>().color;
        var nextImageColor = mapSelectionList[nextImageIndex].map.GetComponent<RawImage>().color;

        actualImageColor.a = Mathf.Lerp(actualImageColor.a, 0f, fxbg_time * Time.deltaTime);
        nextImageColor.a = 1 - actualImageColor.a;

        mapSelectionList[fxbg_imgIndex].map.GetComponent<RawImage>().color = actualImageColor;
        mapSelectionList[nextImageIndex].map.GetComponent<RawImage>().color = nextImageColor;


        if (actualImageColor.a < 0.01f)
        {
            fxbg_imgIndex = nextImageIndex;
            fxbg_delay = 0f;
        }
    }

    private void changeMapSelection()
    {
        foreach (var item in mapSelectionList)
        {
            item.map.SetActive(item.circuit == GameState.selectedCircuit);
        }

        foreach (var item in circuitMapSelectionList)
        {
            item.image.gameObject.SetActive(item.circuit == GameState.selectedCircuit);
        }

        mapName.text = GameState.selectedCircuit.GetName().ToUpper();
    }

    private void InitModeSelectionActions()
    {
        btnTimeTrial.OnClick(() =>
        {
            if (multiplayerSelected) return;
            GameState.gameMode = GameMode.TIME_TRIAL;
            ChangeMenuScreen(ScreenIdEnum.MAP_SELECTION, true);
        });

        btnRace.OnClick(() =>
        {
            InitiateHost(false);
            GameState.gameMode = GameMode.RACE;
            GameState.selectedCircuit = Circuit.LAKE_PARK;
            ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
        });

        btnBattle.OnClick(() =>
        {
            InitiateHost(false);
            GameState.gameMode = GameMode.BATTLE;
            GameState.selectedCircuit = Circuit.ARENA;
            ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
        });
    }

    private void InitMultiplayerActions()
    {
        hostBtn.OnClick(() =>
        {
            ChangeMenuScreen(ScreenIdEnum.MODE_SELECTION, true);
        });
        connectBtn.OnClick(() =>
        {
            ChangeMenuScreen(ScreenIdEnum.CLIENT_CONNECTION, true);
        });
    }

    private void InitLobbyActions()
    {
        /*
        if (NetworkManager.Singleton.IsClient)
        {
            startRaceBtn.gameObject.SetActive(false);
            return;
        }
        startRaceBtn.onClick.AddListener(() =>
        {
            sceneLoader.loadANewScene(GameState.selectedCircuit.GetSceneName());
        });
        */
    }


    private void InitMainMenuActions()
    {
        multiplayerBtn.OnClick(() =>
        {
            multiplayerSelected = true;
            ChangeMenuScreen(ScreenIdEnum.MULTIPLAYER_SELECTION, true);
        });

        singleplayerBtn.OnClick(() =>
        {
            multiplayerSelected = false;
            ChangeMenuScreen(ScreenIdEnum.MODE_SELECTION, true);
        });

        exitBtn.OnClick(() =>
        {
            Application.Quit();
        });
    }

    public void Update()
    {
        connectionTimeout += Time.deltaTime;

        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.START_SCREEN:
                pressStartToPlayTimer += Time.deltaTime;

                if (inputActions.Menu.Submit.ReadValue<float>() > 0)
                {
                    ChangeMenuScreen(ScreenIdEnum.MAIN_MENU, true);
                }

                break;

            case ScreenIdEnum.MAP_SELECTION:
                bestTime.text = "BEST TIME: " + PersistenceManager.GetBestTime(GameState.selectedCircuit);
                break;
        }
        BackgroundImageFade();

        startRaceBtn.SetActive(NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClientsList.Count > 1);
        changeMapBtn.SetActive(NetworkManager.Singleton.IsHost);
        btnRace.gameObject.SetActive(multiplayerSelected);
        btnTimeTrial.gameObject.SetActive(!multiplayerSelected);
        btnBattle.gameObject.SetActive(multiplayerSelected);
        
        alertGameObject.SetActive(alertaText != null && alertaText != "");
        alertTextObj.text = alertaText;
    }

    public void LateUpdate()
    {
        CallbackDelegate?.Invoke();
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.START_SCREEN:

                if(pressStartToPlayTimer > .6f)
                {
                    pressStartToPlayTxt.enabled = !pressStartToPlayTxt.enabled;
                    pressStartToPlayTimer = 0;
                }

                break;

            case ScreenIdEnum.LOBBY:
                int numberOfConnectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
                int count = 0;
                foreach (var player in playerLobbyList)
                {
                    if (count + 1 > numberOfConnectedPlayers) break;
                    ulong playerId = NetworkManager.Singleton.ConnectedClientsIds[count];
                    if (!ConnectedClientsInfo.ContainsKey(playerId)) continue;
                    var playerInfo = ConnectedClientsInfo[playerId];
                    playerLobbyList[count].GetComponent<PlayerLobby>().SetPlayerAndActive(playerInfo.username, playerInfo.points.ToString(), playerInfo.kart);
                    count++;
                }        

                switch(GameState.gameMode){
                    case GameMode.RACE:
                        selectedModeLobby.text = "SELECTED MODE: RACE";
                        break;
                    case GameMode.BATTLE:
                        selectedModeLobby.text = "SELECTED MODE: BATTLE";
                        break;
                    case GameMode.TIME_TRIAL:
                        selectedModeLobby.text = "WTF? COMO?";
                        break;
                }

                changeMapSelection();

                selectedMapLobby.text = "SELECTED MAP: " + GameState.selectedCircuit.GetName();

                break;

            case ScreenIdEnum.KART_SELECTION:
                kartName.text = kartNameMap[GetClientKart(NetworkManager.Singleton.LocalClientId)];
                break;

            case ScreenIdEnum.CLIENT_CONNECTION:
                EventSystem.current.SetSelectedGameObject(hostIpIpt.gameObject);
                break;
        }

        if(GetComponent<NetworkObject>() == null)
        {
            gameObject.AddComponent<NetworkObject>();
        }

    }

    private void ChangeMenuScreen(ScreenIdEnum screenId, bool forward)
    {
        if (actualGameScreen.canvas != null)
        {
            actualGameScreen.canvas.gameObject.SetActive(false);
        }

        if (!forward) PlaySFXMenuBack();

        actualGameScreen = GetScreenInfoByID(screenId);
        actualGameScreen.canvas.gameObject.SetActive(true);
        InitNewScreen();
    }

    private void InitNewScreen()
    {
        switch (actualGameScreen.screenId)
        {
            case ScreenIdEnum.MAIN_MENU:
                singleplayerBtn.Select();
                break;
            case ScreenIdEnum.MULTIPLAYER_SELECTION:
                EventSystem.current.SetSelectedGameObject(hostBtn.gameObject);
                break;
            case ScreenIdEnum.MODE_SELECTION:
                btnBattle.gameObject.SetActive(multiplayerSelected);
                btnRace.gameObject.SetActive(multiplayerSelected);
                btnTimeTrial.gameObject.SetActive(!multiplayerSelected);
                EventSystem.current.SetSelectedGameObject(multiplayerSelected ? btnRace.gameObject : btnTimeTrial.gameObject);
                break;
            case ScreenIdEnum.KART_SELECTION:
                kartSelection.GetComponent<PlayerLobby>().SetPlayerAndActive("", null, ConnectedClientsInfo[NetworkManager.Singleton.LocalClientId].kart);
                break;
            case ScreenIdEnum.MAP_SELECTION:
                AckGameStateSyncClientRpc(GameState.gameMode);
                break;
                /*
            case ScreenIdEnum.LOBBY:
                startRaceBtn.gameObject.SetActive(NetworkManager.Singleton.IsHost);
                break;
                */
        }
    }

    private void PlaySFXMenuBack()
    {
        source.PlayOneShot(audioMenuBack);
    }

    private ScreenInfo GetScreenInfoByID(ScreenIdEnum screenId)
    {
        foreach (ScreenInfo screenInfo in screenInfos)
        {
            if (screenInfo.screenId == screenId) return screenInfo;
        }

        throw new Exception("THE SCREEN ID " + screenId + " IS NOT PRESENT IN THE SCREEN LIST!");
    }

    protected void ConnectClient()
    {
        loadingIcon.ToggleLoadingIcon(true);
        bool isConnectionMade = false;
        try
        {
            isConnectionMade = NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        if (!isConnectionMade) 
        {
            loadingIcon.ToggleLoadingIcon(false);
            return;
        }
        connectionTimeout = 0;
        CallbackDelegate += AsyncConnectionValidations;
    }

    private void AsyncConnectionValidations()
    {
        bool isFullyConnected = IsConnectedToClient();
        if (!isFullyConnected && connectionTimeout < 5f) return;

        if (!isFullyConnected)
        {
            if (NetworkManager.Singleton.IsClient) {
                Destroy(GetComponent<NetworkObject>());
                NetworkManager.Singleton.Shutdown();
            }
        }

        if (isFullyConnected)
        {
            AddPlayerToClientsListServerRpc(PersistenceManager.gameSave.username, NetworkManager.LocalClientId);
            ChangeMenuScreen(ScreenIdEnum.LOBBY, true);
            AskGameStateSyncServerRpc();
            GameState.online = true;
        }

        loadingIcon.ToggleLoadingIcon(false);
        CallbackDelegate -= AsyncConnectionValidations;
    }

    public bool IsConnectedToClient()
    {
        return NetworkManager.Singleton.IsConnectedClient;
    }

    void OnDestroy(){
        inputActions.Menu.Submit.performed -= Submit_performed;
        inputActions.Menu.Right.performed -= Right_performed;
        inputActions.Menu.Left.performed -= Left_performed;
        inputActions.Menu.Back.performed -= Back_performed;
        inputActions.Menu.Extra.performed -= Extra_performed;
        inputActions.Menu.Map.performed -= Map_performed;
    }
}
