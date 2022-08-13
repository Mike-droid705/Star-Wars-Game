using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WaveSystem : NetworkBehaviour
{
	[SerializeField]float SpawnInterval = 30f;
	[SerializeField]Transform parent;
	[SerializeField]Transform[] SpawnPos;
	[SerializeField]GameObject[] SpawnPrefabs;
	[SerializeField]Transform CorvetteSpawnPos;
	[SerializeField]GameObject CorvettePrefab;

	private Meter meter;
	bool Rwait=false;
	bool Iwait=false;

	void Awake(){
		meter = FindObjectOfType<Meter>();
	}

    void Start()
    {
        InvokeRepeating("Spawn", 0, SpawnInterval);
        GameObject CorvetteObject = Instantiate(CorvettePrefab, CorvetteSpawnPos.transform.position, CorvetteSpawnPos.transform.rotation);
        CorvetteObject.transform.SetParent(parent);
        NetworkServer.Spawn(CorvetteObject);
    }

    void Update(){
    	if(!meter.startingPhase){
    		if(gameObject.layer == LayerMask.NameToLayer("New Republic")){
                if(meter.rebelAttack && !Rwait){
                    GameObject prefab = Instantiate(CorvettePrefab, CorvetteSpawnPos.transform.position, CorvetteSpawnPos.transform.rotation);
                    prefab.transform.SetParent(parent);
                    NetworkServer.Spawn(prefab);
                    Rwait=true;
                }
                if(meter.rebelDefend){Rwait=false;}
            }
            if(gameObject.layer == LayerMask.NameToLayer("Galactic Empire")){
                if(meter.impAttack && !Iwait){
                    GameObject prefab = Instantiate(CorvettePrefab, CorvetteSpawnPos.transform.position, CorvetteSpawnPos.transform.rotation);
                    prefab.transform.SetParent(parent);
                    NetworkServer.Spawn(prefab);
                    Iwait=true;
                }
                if(meter.impDefend){Iwait=false;}
            }
    	}
    }

    void Spawn(){
    	foreach(var spawnpos in SpawnPos){
    		GameObject prefab = Instantiate(SpawnPrefabs[Random.Range(0, SpawnPrefabs.Length)], spawnpos.position, spawnpos.rotation);
    		prefab.transform.SetParent(parent);
    		NetworkServer.Spawn(prefab);
    	}
    }
}
