using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof (Target))]
public class HitDecal : NetworkBehaviour
{
	[SerializeField]Target flagship;
	[SerializeField]GameObject explosionParticle; 

	private Target target;
	private float multiplier;
    // Start is called before the first frame update
    void Start()
    {
    	multiplier = Random.Range(2f, 5f);
    	if(netIdentity != null){
    		gameObject.SetActive(false);
    	}
    }

    void OnEnabled(){
        var dfx = Instantiate(explosionParticle, transform.position, explosionParticle.transform.rotation);
        Destroy(dfx, dfx.GetComponent<ParticleSystem>().main.duration);
    }

    // Update is called once per frame
    public void DealDamage(float damage)
    {
    	if(!isServer){return;}
        flagship.ApplyDamage(damage*multiplier, 0);
    }
}
