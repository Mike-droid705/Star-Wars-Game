using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDecalController : MonoBehaviour
{
	[SerializeField] float delay = 1f;

    public void ActivateHitDecals(){
    	StartCoroutine(Activate());
    }

    IEnumerator Activate(){
    	foreach(Transform child in transform){
    		child.gameObject.SetActive(true);
    		yield return new WaitForSeconds(delay);
    	}
    }
}
