using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof (Shooting))]
[RequireComponent(typeof (FindTarget))]
public class CannonController : NetworkBehaviour
{
    //todo make chaning targets more seamless
    //Parameters
	[SerializeField]public Transform cannonTop;
	[Range(200, 5000)][SerializeField]public float attackRange = 200f;
	[SerializeField]public bool isTargetingOneEnemy;
    [SerializeField]public float yMinClamp;
    [SerializeField]public float yMaxClamp;
    [SerializeField]public float xMinClamp;
    [SerializeField]public float xMaxClamp;
    [SerializeField]public bool isUsingMissiles;
    [Range(200, 5000)][SerializeField]public float missileAttackRange = 300f;

	int targetLayer;
    //State
	AeroplaneController plane_targetEnemy;
    Target targetEnemy;
	private Shooting m_Shooting;
	private FindTarget findTarget;

	private void Awake()
    {
        m_Shooting = GetComponent<Shooting>();
        findTarget = GetComponent<FindTarget>();

        if(gameObject.layer == 9){
            targetLayer = 8;
        }else if(gameObject.layer == 8){
            targetLayer = 9;
        }
    }

    // Update is called once per frame
    void Update()
    {	
        plane_targetEnemy = findTarget.SetTargetEnemy(targetLayer, isTargetingOneEnemy);
        if(plane_targetEnemy){
            targetEnemy = plane_targetEnemy.GetComponent<Target>();
        }

    	if(targetEnemy){
            Vector3 targetDir = targetEnemy.Position - cannonTop.position;
            float angle = Vector3.Angle(targetDir, cannonTop.forward);

	 		cannonTop.LookAt(targetEnemy.transform);

            float rotX = cannonTop.eulerAngles.x;
            float rotY = cannonTop.eulerAngles.y;
            if(rotY > ((yMinClamp+yMaxClamp)/2)+180){
                rotY-=360;
            }
            if(rotX > ((xMinClamp+xMaxClamp)/2)+180){
                rotX-=360;
            }
            rotY = Mathf.Clamp(rotY, yMinClamp, yMaxClamp);
            rotX = Mathf.Clamp(rotX, xMinClamp, xMaxClamp);
            cannonTop.rotation = Quaternion.Euler(rotX, rotY, cannonTop.eulerAngles.z);

            if(angle < 10){
	 		    FireAtEnemy(true);
            }else{
                FireAtEnemy(false);
            }
    	}else{
	 		m_Shooting.Fire(false);
    	}
    }

    private void FireAtEnemy(bool b){
    	float distanceToEnemy = Vector3.Distance(targetEnemy.transform.position, gameObject.transform.position);
    	if(distanceToEnemy <= attackRange){
    		m_Shooting.Fire(b);
    	}else{
    		m_Shooting.Fire(false);
    	}

        if(isUsingMissiles && b){
            if(distanceToEnemy <= missileAttackRange){
                m_Shooting.TryFireMissile();
            }
        }
    }
}
