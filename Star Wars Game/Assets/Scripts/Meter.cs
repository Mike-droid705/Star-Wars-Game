using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Meter : MonoBehaviour
{
    [SerializeField]float meter = 50f;
    [SerializeField]float RebelTeamMeter;
    [SerializeField]float ImperialTeamMeter;
    [SerializeField]Slider MeterBar;

    public bool startingPhase=true;
    public bool rebelAttack=false;
    public bool rebelDefend=false; 
    public bool impAttack=false;
    public bool impDefend=false;

    void Awake(){
    	RebelTeamMeter = meter/2;
    	ImperialTeamMeter = meter/2;
    }

    public void GetMeterBar(Transform _parent){
        MeterBar = GetChildObject(_parent, "Meter").GetComponent<Slider>();
    }

    GameObject GetChildObject(Transform parent, string _tag){
        for(int i = 0; i < parent.childCount; i++){
            Transform child = parent.GetChild(i);
            if(child.tag == _tag){
                return child.gameObject;
            }
            if(child.childCount > 0){
                GetChildObject(child, _tag);
            }
        }
        return parent.gameObject;
    }

    public void ChangeMeter(float layer, float points){
    	//check who was shot
    	if(layer == LayerMask.NameToLayer("Galactic Empire")){
            if(startingPhase || RebelTeamMeter != meter){
                //to check if the points pass the meter
                if(points + RebelTeamMeter <= meter){
                    RebelTeamMeter += points;
                    ImperialTeamMeter -= points;
                }else{
                    RebelTeamMeter = meter;
                    ImperialTeamMeter = 0;
                }
            }
    	}else if(layer == LayerMask.NameToLayer("New Republic")){
            if(startingPhase || ImperialTeamMeter != meter){
                if(points + ImperialTeamMeter <= meter){
                    ImperialTeamMeter += points;
                    RebelTeamMeter -= points;
                }else{
                    ImperialTeamMeter = meter;
                    RebelTeamMeter = 0;
                }
            }
    	}
    }

    // Update is called once per frame
    void Update()
    {
        if(MeterBar != null){
            MeterBar.maxValue = meter;
            MeterBar.value = RebelTeamMeter;
        }

        if(RebelTeamMeter == meter){
        	rebelAttack=true;
            rebelDefend=false; 
            impAttack=false;
            impDefend=true;
            startingPhase=false;
        }
        if(ImperialTeamMeter == meter){
            rebelAttack=false;
            rebelDefend=true; 
            impAttack=true;
            impDefend=false;
            startingPhase=false;
        }
    }
}
