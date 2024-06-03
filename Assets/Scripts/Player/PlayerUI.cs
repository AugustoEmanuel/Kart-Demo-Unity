using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static CarController;

public class PlayerUI : MonoBehaviour
{
    [Serializable]
    public struct ItemUI
    {
        public NetworkPlayer.Item Item;
        public GameObject gameObject;
    }

    [SerializeField] public Canvas canvas;
    [SerializeField] private TextMeshProUGUI textLap;
    [SerializeField] private TextMeshProUGUI textTime;
    [SerializeField] private TextMeshProUGUI recordedTimes;
    [SerializeField] private TextMeshProUGUI textPosition;
    [SerializeField] private TextMeshProUGUI textPositionSuffix;
    [SerializeField] private TextMeshProUGUI blinkLapText;
    [SerializeField] private TextMeshProUGUI blinkTimeText;
    [SerializeField] private TextMeshProUGUI itemTimer;
    [SerializeField] private TextMeshProUGUI lifeCounter;
    [SerializeField] private TextMeshProUGUI pointsCounter;

    [SerializeField] private Text mapNameText;
    [SerializeField] private Text gameModeText;

    [SerializeField] private TextMeshProUGUI enterButtonLabel;

    [SerializeField] private TextMeshProUGUI contadorCircuito;

    [SerializeField] private RawImage enterButtonIcon;

    [SerializeField] private AudioSource lapCountSfx;

    [SerializeField] private List<ItemUI> itemUIs;

    private Player player;
    private CircuitManager circuit;

    private float contadorBlink = 0;
    private float contadorBlinkTotal = 0;
    private bool blinkToggle = false;
    private bool blinkActive = false;

    public GameObject lapTime;
    public GameObject lapCount;
    public GameObject lifeCount;
    public GameObject pointCount;

    public GameObject menuOverlay;
    public GameObject finishOverlay;
    public GameObject loadingOverlay;
    public List<GameObject> timeTrialTimes;


    private void Start()
    {
        recordedTimes.text = "";
        player = GetComponent<Player>();
        circuit = GameObject.Find("CIRCUIT_MANAGER").GetComponent<CircuitManager>();
        finishOverlay.SetActive(false);

        bool isBattle = GameState.gameMode == GameMode.BATTLE;
        lapTime.SetActive(!isBattle);
        lapCount.SetActive(!isBattle);
        lifeCount.SetActive(isBattle);
        pointCount.SetActive(isBattle);

    }

    private void Update()
    {
        loadingOverlay.SetActive(!NetworkBehaviourCustom.IsEveryoneInSync());

        contadorCircuito.gameObject.SetActive(CircuitManager.contadorInicio > 0);
        contadorCircuito.text = ((int)Math.Ceiling(CircuitManager.contadorInicio)).ToString();

        if(player.finishedRace)
        {
            OpenFinishOverlay();
            if(NetworkManager.Singleton.IsHost && !GameState.online){
                enterButtonLabel.text = "Back to main menu";
                enterButtonIcon.gameObject.SetActive(true);
            }
            else if(NetworkManager.Singleton.IsHost){
                bool b = CircuitManager.HasEveryoneFinishedRace();
                enterButtonLabel.text = b ? "Back to lobby" : "Waiting until all players finish...";
                enterButtonIcon.gameObject.SetActive(b);
            }
            else{
                enterButtonLabel.text = "";
                enterButtonIcon.gameObject.SetActive(false);
            }
            return;
        }

        if (canvas == null) return;
        UpdateActualLap();
        UpdateActualLapTime();
        UpdateActualItem();
        textPosition.text = player.position.ToString();

        textPositionSuffix.gameObject.SetActive(GameMode.TIME_TRIAL != GameState.gameMode);
        textPosition.gameObject.SetActive(GameMode.TIME_TRIAL != GameState.gameMode);

        switch(player.position){
            case 1:
                textPositionSuffix.text = "st";
                textPositionSuffix.color = new Color(255, 187, 0, 255);
                textPosition.color = new Color(255, 187, 0, 255);
                break;
            case 2:
                textPositionSuffix.text = "nd";
                textPositionSuffix.color = new Color(159, 159, 159, 255);
                textPosition.color = new Color(159, 159, 159, 255);
                break;
            case 3:
                textPositionSuffix.text = "rd";
                textPositionSuffix.color = new Color(231, 100, 0, 255);
                textPosition.color = new Color(231, 100, 0, 255);
                break;
            default:
                textPositionSuffix.text = "th";
                textPositionSuffix.color = new Color(255, 0, 23, 255);
                textPosition.color = new Color(255, 0, 23, 255);
                break;
        }

        PlayerPositionData data = CircuitManager.playerPositionDataDictonary[NetworkManager.Singleton.LocalClientId];
        pointsCounter.text = data.points.ToString();
        lifeCounter.text = data.lifes.ToString();

        if (blinkActive)
        {
            contadorBlink += Time.deltaTime;
            if (contadorBlink > 0.3f)
            {
                blinkToggle = !blinkToggle;
                contadorBlinkTotal += contadorBlink;
                contadorBlink = 0;
            }
            if(contadorBlinkTotal > 3)
            {
                blinkActive = false;
                contadorBlinkTotal = 0;
                contadorBlink = 0;
            }
        }

        blinkTimeText.gameObject.SetActive(blinkActive && blinkToggle);
        blinkLapText.gameObject.SetActive(blinkActive && blinkToggle);
    }

    private void UpdateActualLapTime()
    {
        textTime.text = GetTimeFormated(player.currentTime);
    }

    private void UpdateActualLap()
    {
        textLap.text = player.actualLap + "/" + circuit.lapCount;
    }

    private void UpdateActualItem()
    {
        NetworkPlayer networkPlayer = NetworkBehaviourCustom.GetNetworkPlayer(NetworkManager.Singleton.LocalClientId);
        if(networkPlayer.item == NetworkPlayer.Item.NO_ITEM)
        {
            //MEU
            itemTimer.gameObject.SetActive(true);
            itemTimer.text = (3 - (int)networkPlayer.itemTimer).ToString(); //KJKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK
        }
        else
        {
            //DEUS
            itemTimer.gameObject.SetActive(false);
        }

        foreach (var item in itemUIs)
        {
            item.gameObject.SetActive(item.Item == networkPlayer.item);
        }
    }

    public void RecordNewLapTime()
    {
        int lapIndex = player.actualLap - 1;
        string previousMapTime = GetTimeFormated(player.recordedTimes[lapIndex]);

        blinkLapText.text = "LAP " + player.actualLap;
        blinkTimeText.text = previousMapTime;
        recordedTimes.text += previousMapTime + "\n";
        blinkActive = true;
        lapCountSfx.Play();
    }

    public void OpenFinishOverlay()
    {
        /* TIME TRIAL */
        if(GameState.gameMode == GameMode.TIME_TRIAL){
            int i = 0;
            foreach (var item in timeTrialTimes)
            {
                if(i >= circuit.lapCount)
                {
                    item.SetActive(false);
                    i++;
                    continue;
                }

            item.transform.Find("lap").GetComponent<Text>().text = (i+1) + "º LAP"; 
            item.transform.Find("time").GetComponent<Text>().text = GetTimeFormated(player.recordedTimes[i]);
            i++;
            }
            
        }
        /* VERSUS */
        else {
            var orderedPlayers = CircuitManager.playerPositionDataDictonary.Values
                .Where(player => player.finishedRace)
                .OrderBy(player => player.rank)
                .Concat(CircuitManager.playerPositionDataDictonary.Values.Where(player => !player.finishedRace))
                .ToList();

            int i = 0;
            foreach (var item in timeTrialTimes)
            {
                if (i >= orderedPlayers.Count || !orderedPlayers[i].finishedRace)
                {
                    item.SetActive(false);
                    i++;
                    continue;
                }

                item.transform.Find("lap").GetComponent<Text>().text = orderedPlayers[i].rank + "º"; 
                item.transform.Find("time").GetComponent<Text>().text = CircuitManager.ConnectedClientsInfo[orderedPlayers[i].clientId].username + (GameState.gameMode == GameMode.BATTLE ? " - " + orderedPlayers[i].points : "");
                item.SetActive(true);
                i++;
            }
        }

        mapNameText.text = GameState.selectedCircuit.GetName().ToUpper();

        switch (GameState.gameMode)
        {
            case GameMode.TIME_TRIAL:
                gameModeText.text = "TIME TRIAL";
                break;
            case GameMode.RACE:
                gameModeText.text = "RACE";
                break;
            case GameMode.BATTLE:
                gameModeText.text = "BATTLE";
                break;
        }

        canvas.gameObject.SetActive(false);
        finishOverlay.SetActive(true);
    }

    private string GetTimeFormated(float timeFl)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeFl);
        return time.Minutes.ToString("D2") + "'" + time.Seconds.ToString("D2") + "\"" + time.Milliseconds.ToString("D3");
    }
}
