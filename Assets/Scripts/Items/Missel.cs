using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Missel : NetworkBehaviourCustom
{
    public ulong targetId;
    public ulong ownerId;

    public NetworkVariable<bool> alternateModel = new();

    public int nextCheckpointSingleIndex;

    public GameObject misselModel;
    public GameObject alternateMisselModel;

    public NetworkObject explosionPreFab;

    public float lifeTimer = 0;

    void Update()
    {
        
        misselModel.SetActive(!alternateModel.Value);
        alternateMisselModel.SetActive(alternateModel.Value);

        if(!IsHost) return;

        Debug.Log("RUNNING MISSEL");

        if(ownerId == 9999) return;
        lifeTimer += Time.deltaTime;

        bool isColliding = false;

        if(targetId == 999){
            transform.Translate(Vector3.forward * 50f * Time.deltaTime);
            foreach(NetworkObject nO in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList){
                if(nO.tag != "Player") continue;
                if(nO.OwnerClientId == ownerId) continue;

                GameObject gameObject = GetFirstActiveChildWithTag(nO, "Kart");

                isColliding = CheckCollision(gameObject.transform, transform);
                if(isColliding){
                    if(ConnectedClientsInfo[nO.OwnerClientId].shieldActive) {
                        Destroy(this.gameObject);
                        return;
                    }
                    CircuitManager.AddPointsToPlayer(ownerId);
                    CircuitManager.TakeDamage(nO.OwnerClientId);
                    TakeDamageClientRpc(nO.OwnerClientId);
                    SpawnExplosion(gameObject.transform.position);
                    Destroy(this.gameObject);
                    return;
                }
            }

            if(lifeTimer > 6f){
                Destroy(this.gameObject);
            }

            return;
        }

        Transform target = GetTargetTransformKart();
        transform.position = Vector3.MoveTowards(transform.position, target.position, 50f * Time.deltaTime);
        transform.LookAt(target);

        isColliding = CheckCollision(target, transform);
        if(isColliding){
            if(ConnectedClientsInfo[targetId].shieldActive) {
                Destroy(this.gameObject);
                return;
            }
            CircuitManager.AddPointsToPlayer(ownerId);
            CircuitManager.TakeDamage(targetId);
            TakeDamageClientRpc(targetId);
            SpawnExplosion(target.position);
            Destroy(this.gameObject);
        }

        if(lifeTimer > 6f){
            Destroy(this.gameObject);
        }
    }

    GameObject GetFirstActiveChildWithTag(NetworkObject parent, string tag)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.activeSelf && child.CompareTag(tag))
            {
                return child.gameObject;
            }
        }

        return null; 
    }

    private void SpawnExplosion(Vector3 position)
    {
        NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(explosionPreFab, 0, true, true, false, position, Quaternion.Euler(Vector3.zero));

    }

    bool CheckCollision(Transform objA, Transform objB)
    {
        // Get the bounding boxes of both objects
        Bounds boundsA = GetBounds(objA);
        Bounds boundsB = GetBounds(objB);

        // Check if the bounding boxes intersect
        return boundsA.Intersects(boundsB);
    }

    Bounds GetBounds(Transform obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            // Get the bounding box of the object's renderer
            return renderer.bounds;
        }
        else
        {            
            Bounds bounds = new Bounds(obj.position, Vector3.one);
            return bounds;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        SpawnExplosion(transform.position);
        Destroy(this.gameObject);
    }

    public Transform GetTargetTransformKart(){
        foreach(NetworkObject nO in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList){
            if(nO.tag != "Player") continue;
            if(nO.OwnerClientId != targetId) continue;

            GameObject gameObject = GetFirstActiveChildWithTag(nO, "Kart");

            return gameObject.transform;
        }

        return null;
    }

    [ClientRpc]
    public void TakeDamageClientRpc(ulong clientId) {
        if(NetworkManager.Singleton.LocalClientId != clientId) return;

        GetTargetObjectKart(clientId).transform.parent.GetComponent<Player>().ExecuteDamage();
    }

    private static NetworkObject GetTargetObjectKart(ulong clientId){
        foreach(NetworkObject nO in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList){
            if(nO.tag != "Kart") continue;
            if(nO.OwnerClientId == clientId) return nO;

        }

        return null;
    }
}
