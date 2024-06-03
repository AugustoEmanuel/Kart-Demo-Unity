using static NetworkBehaviourCustom;

public enum CircuitMap
{
    NONE,
    LAKE,
    DESERT_PARADISE
};

public enum GameMode
{
    TIME_TRIAL,
    RACE,
    BATTLE
};

public class GameState
{
    public static GameMode gameMode;
    public static Circuit selectedCircuit;
    public static bool online;
    public static bool coldStart = true;
    public static bool locked = false;
}