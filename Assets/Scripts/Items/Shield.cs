using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Shield : NetworkBehaviour
{
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        /*
        if(!IsHost) return;
        if (other.TryGetComponent(out Missel missel))
        {
           Destroy(missel.gameObject);
        }
        */
    }
}
