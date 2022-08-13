using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindTarget : MonoBehaviour
{
    AeroplaneController targetEnemy;
    TargetableObjects targetableObjects;

    void Awake(){
    	targetableObjects = FindObjectOfType<TargetableObjects>();
    }

    public AeroplaneController SetTargetEnemy(int layer, bool isTargetingOneEnemy){
        var sceneTargets = FindObjectsWithLayer(layer);
        if(sceneTargets == null){
        	return targetEnemy;
        }

        if(targetEnemy && isTargetingOneEnemy){
        	return targetEnemy;
        }
        AeroplaneController closestTarget = sceneTargets[0].GetComponent<AeroplaneController>();

        foreach (AeroplaneController testTarget in sceneTargets){
            closestTarget = GetClosest(closestTarget, testTarget.GetComponent<AeroplaneController>());
        }
        targetEnemy = closestTarget;
        return targetEnemy;
    }

    public AeroplaneController GetClosest(AeroplaneController enemyA, AeroplaneController enemyB){
        float distToEnemyA = Vector3.Distance(enemyA.transform.position, transform.position);
        float distToEnemyB = Vector3.Distance(enemyB.transform.position, transform.position);
        if(distToEnemyA < distToEnemyB){
            return enemyA;
        }else
        return enemyB;
    }


    public AeroplaneController[] FindObjectsWithLayer (int layer) {
    	AeroplaneController[] goArray = targetableObjects.Targets.ToArray() as AeroplaneController[];
    	List<AeroplaneController> goList = new List<AeroplaneController>();
    	for (int i = 0; i < goArray.Length; i++) { 
            if(goArray[i] != null){
                if (goArray[i].gameObject.layer == layer) { 
                    goList.Add(goArray[i]); 
                }
            }
    	} 
    	if (goList.Count == 0) { return null; } 
    	return goList.ToArray(); 
 	}
}
