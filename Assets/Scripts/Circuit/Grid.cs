using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out CarController car))
        {
           // car.isCollidingWithGrid = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CarController car))
        {
           // car.isCollidingWithGrid = false;
        }
    }
}
