using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Mina : NetworkBehaviourCustom
{
    public ulong ownerId = 9999;

    public NetworkObject explosionPreFab;
    public Renderer childRenderer;
    public float contadorBlink = 0f;
    public bool acessa = false;

    void Update()
    {
        contadorBlink += Time.deltaTime;

        if(contadorBlink > 0.15f) {
            acessa = !acessa;
            contadorBlink = 0;
        }

        childRenderer.materials[1].color = acessa ? Color.red : Color.black;

        if(!IsHost) return;

        if(ownerId == 9999) return;

        foreach(NetworkObject nO in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList){
            if(nO.tag != "Player") continue;
            if(nO.OwnerClientId == ownerId) continue;

            GameObject gameObject = GetFirstActiveChildWithTag(nO, "Kart");

            bool isColliding = CheckCollision(gameObject.transform, transform);
            if(isColliding){
                Debug.Log("WTF?" + ownerId + nO.OwnerClientId);
                if(ConnectedClientsInfo[nO.OwnerClientId].shieldActive) {
                    SpawnExplosion(gameObject.transform.position);
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

    //O SISTEMA DE COLISÃO DA UNITY NÃO TRABALHA COM NETWORK OBJECTS, LOGO PRECISO EU MESMO CHECAR A COLISÃO OLHA QUE LEGAL :)
    bool CheckCollision(Transform objA, Transform objB)
    {
        Bounds boundsA = GetBounds(objA);
        Bounds boundsB = GetBounds(objB);

        return boundsA.Intersects(boundsB);
    }

    Bounds GetBounds(Transform obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();

        if (renderer != null)
        {
            return renderer.bounds;
        }
        else
        {            
            Bounds bounds = new Bounds(obj.position, Vector3.one);
            return bounds;
        }
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
