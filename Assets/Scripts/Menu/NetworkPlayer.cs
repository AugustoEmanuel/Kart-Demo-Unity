using System;
using System.Collections.Generic;

public class NetworkPlayer
{
    public enum Kart
    {
        KART_1,
        KART_2,
        KART_3,
        KART_4
    };

    public enum Item
    {
        NO_ITEM,
        BOOST,
        MISSILE,
        MISSILE_NO_LOCK,
        SHIELD,
        MINA
    };

    public ulong clientId;
    public Kart kart;
    public string username;
    public int points;
    public Item item = Item.NO_ITEM;
    public float itemTimer = 0;
    public float boostTimer = 0;
    public bool appliedBoost;
    public bool shieldActive;
    public float shieldTimer;
    public bool sync;

    public bool takeDamage;

    public NetworkPlayerAudio networkPlayerAudio = new();

    public NetworkPlayer(ulong clientId, string username)
    {
        this.clientId = clientId; 
        this.username = username;
        networkPlayerAudio = new();
    }

    public static Dictionary<Kart, string> kartNameMap = new Dictionary<Kart, string> {
        {Kart.KART_1, "KART 1"}, 
        {Kart.KART_2, "KART 2"}, 
        {Kart.KART_3, "KART 3"},
        {Kart.KART_4, "KART 4"}
    };
}

[Serializable]
public class NetworkPlayerAudio
{
    public float kartAudioMoving = 0;
    public float kartAudioDrifting = 0;
}
