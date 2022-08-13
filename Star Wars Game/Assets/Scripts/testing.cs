using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testing : MonoBehaviour {

	public Target[] targets;

    void Start(){
    	targets = GetComponentsInChildren<Target>();
    }
}