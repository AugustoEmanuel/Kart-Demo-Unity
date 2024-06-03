using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Netcode;
using UnityEngine;

public class PersistenceManager
{
    public static GameSave gameSave;

    public static GameSave LoadPlayerData()
    {
        string filePath = Application.persistentDataPath + "/playerData.dat";
        if (File.Exists(filePath))
        {
            try{
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream fileStream = File.Open(filePath, FileMode.Open);
                gameSave = (GameSave)formatter.Deserialize(fileStream);
                fileStream.Close();
                return gameSave;
            }
            catch{
                return null;
            }
        }
        else
        {
            Debug.LogError("Save file not found.");
            return null;
        }
    }

    public static bool AddNewBestTime(Circuit circuit, float time){

        if(gameSave.circuitTimes == null){
            gameSave.circuitTimes = new();
        }

        if(!gameSave.circuitTimes.ContainsKey(circuit)){
            gameSave.circuitTimes.Add(circuit, time);
            SaveGameData();
            return true;
        }

        if(gameSave.circuitTimes[circuit] < time){
            gameSave.circuitTimes[circuit] = time;
            SaveGameData();
            return false;
        }
        return true;
    }

    public static string GetBestTime(Circuit circuit){
        try{
            return GetTimeFormated(gameSave.circuitTimes[circuit]);
        }
        catch{}
        return "--.--.---";
    }

    private static string GetTimeFormated(float timeFl)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeFl);
        return time.Minutes.ToString("D2") + "'" + time.Seconds.ToString("D2") + "\"" + time.Milliseconds.ToString("D3");
    }

    public static void SaveGameData()
    {
        string filePath = Application.persistentDataPath + "/playerData.dat";
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream fileStream = File.Create(filePath);
        formatter.Serialize(fileStream, gameSave);
        fileStream.Close();
    }

    public static void CreateNewSaveData(string username)
    {
        gameSave = new();
        gameSave.username = username;

        SaveGameData();
    }
}

[Serializable]
public class GameSave
{
    public string username;
    public Dictionary<Circuit, float> circuitTimes;
}
