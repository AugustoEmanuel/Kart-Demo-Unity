using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCheckpoints : MonoBehaviour
{

    public List<CheckpointSingle> checkpointSingleList;

    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("Checkpoints");

        checkpointSingleList = new List<CheckpointSingle>();
        foreach (Transform checkpointTransform in checkpointsTransform)
        {
            CheckpointSingle checkpointSingle = checkpointTransform.GetComponent<CheckpointSingle>();
            checkpointSingle.SetTrackCheckpoints(this);
            checkpointSingleList.Add(checkpointSingle);
        }
    }

    public void PlayerThroghCheckpoint(CheckpointSingle checkpointSingle, Player player)
    {
        if(checkpointSingleList.IndexOf(checkpointSingle) == player.nextCheckpointSingleIndex)
        {
            player.nextCheckpointSingleIndex = (player.nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            if(checkpointSingleList.IndexOf(checkpointSingle) == checkpointSingleList.Count - 1)
            {
                player.IncrementLap();
            }
            Debug.Log("Correct");
            Debug.Log(player.nextCheckpointSingleIndex + " | " + checkpointSingleList.Count);
        }
        else
        {
            Debug.Log("Wrong");
        }
    }

    public void MisselThroghCheckpoint(CheckpointSingle checkpointSingle, Missel missel)
    {
        if(checkpointSingleList.IndexOf(checkpointSingle) == missel.nextCheckpointSingleIndex)
        {
            missel.nextCheckpointSingleIndex = (missel.nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            Debug.Log("Correct");
        }
        else
        {
            Debug.Log("Wrong");
        }
    }
}
