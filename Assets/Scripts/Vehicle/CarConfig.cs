using System;
using System.Collections.Generic;
using UnityEngine;

public class CarConfig : MonoBehaviour
{
    public enum Axel
    {
        Front,
        Rear
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public Axel axel;
        public WheelCollider collider;
    }

    public List<Wheel> wheelList = new();

    public List<GameObject> driftSmoke = new();

    public GameObject model;
}
