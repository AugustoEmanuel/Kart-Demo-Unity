using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{

    private TrackCheckpoints TrackCheckpoints;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("SOMETHING COLLIDED!");

        if (other.TryGetComponent(out CarController car))
        {
            Player player = car.transform.parent.GetComponent<Player>();
            TrackCheckpoints.PlayerThroghCheckpoint(this, player);
        }
        else if (other.TryGetComponent(out Missel mis))
        {
             TrackCheckpoints.MisselThroghCheckpoint(this, mis);
        }
    }

    public void SetTrackCheckpoints(TrackCheckpoints trackCheckpoints)
    {
        TrackCheckpoints = trackCheckpoints;
    }
}
