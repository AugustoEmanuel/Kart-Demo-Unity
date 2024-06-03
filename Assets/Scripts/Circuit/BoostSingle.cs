using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostSingle : MonoBehaviour
{

    public bool isCancel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CarController car))
        {
            car.isOnBoostZone = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out CarController car))
        {
            car.transform.rotation = Quaternion.Lerp(car.transform.rotation, Quaternion.Euler(car.transform.rotation.x, transform.rotation.eulerAngles.y, car.transform.rotation.z), 7.5f * Time.deltaTime);
            car.transform.position = Vector3.Lerp(car.transform.position, new Vector3(transform.parent.position.x, car.transform.position.y, car.transform.position.z), .1f * Time.deltaTime);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CarController car))
        {
            car.isOnBoostZone = false;
        }
    }
}
