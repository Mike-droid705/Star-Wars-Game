using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Subsystem : MonoBehaviour
{
	[SerializeField]private string subsystemName;
    [SerializeField]private GameObject[] SubsystemGameObject;
    [SerializeField]private UnityEvent m_event;

    bool onetime=false;

    // Update is called once per frame
    void Update()
    {
    	int j=0;
    	for(int i=0; i < SubsystemGameObject.Length; i++){
    		if(SubsystemGameObject[i] == null){
    			j++;
    		}
    	}
    	if(!onetime){
    		if(j == SubsystemGameObject.Length){
	        	m_event.Invoke();
	        	onetime=true;
	        }
    	}
    }
}
