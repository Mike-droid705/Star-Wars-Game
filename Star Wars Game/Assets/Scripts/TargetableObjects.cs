using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetableObjects : MonoBehaviour
{
    public List<AeroplaneController> Targets = new List<AeroplaneController>();

    public void AddToList(AeroplaneController i){
    	Targets.Add(i);
    }
    public void RemoveFromList(AeroplaneController i){
    	Targets.Remove(i);
    }
}
