using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


class CircuitAttributes : Attribute
{
    internal CircuitAttributes(string SceneName, string name)
    {
        this.SceneName = SceneName;
        this.Name = name;
    }
    public string SceneName { get; private set; }
    public string Name { get; private set; }
}

public static class Circuits
{
    private static CircuitAttributes GetAttr(Circuit p)
    {
        return (CircuitAttributes)Attribute.GetCustomAttribute(ForValue(p), typeof(CircuitAttributes));
    }

    public static string GetSceneName(this Circuit c)
    {
        return GetAttr(c).SceneName;
    }

    public static string GetName(this Circuit c)
    {
        return GetAttr(c).Name;
    }

    private static MemberInfo ForValue(Circuit p)
    {
        return typeof(Circuit).GetField(Enum.GetName(typeof(Circuit), p));
    }

}

[Serializable]
public enum Circuit
{
    [CircuitAttributes("HoverCircuit", "Lake Park")] LAKE_PARK,
    [CircuitAttributes("DesertParadise", "Desert Paradise")] DESERT_PARADISE,
    [CircuitAttributes("Arena", "Arena")] ARENA
}
