using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class PlayerHUD : NetworkBehaviour
{
    [Header("Targeting")]
    [SerializeField] Image TargetBox;
    [SerializeField] Transform missileLock;
    [SerializeField] GameObject canvasPrefab;
    [SerializeField] Canvas canvas;
    [SerializeField] Color normalColor;
    [SerializeField] Color lockColor;

    [Header("UI")]
    [SerializeField] Slider healthBar;
    [SerializeField] Slider shieldBar;
    [SerializeField] Text meterText;

    private Meter meter;
    float playerHealth;
    float playerShieldHealth;
    List<Image> TargetBoxList = new List<Image>();
    Vector2 pos;
	AeroplaneController[] sceneEnemies;
    Shooting Shooting;
    AeroplaneController AeroplaneController;

    GameObject missileLockGO;
    Image missileLockImage;
    bool track = true;

    private int m_targetLayer; 

    Vector3 TransformToHUDSpace(Vector3 worldSpace) {
        var screenSpace = Camera.main.WorldToScreenPoint(worldSpace);
        return screenSpace - new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2);
    }

    void Awake(){
        Shooting = GetComponent<Shooting>();
        AeroplaneController = GetComponent<AeroplaneController>();
        meter = FindObjectOfType<Meter>();

        GameObject canvasGO = Instantiate(canvasPrefab, transform.position, transform. rotation);
        canvas = canvasGO.GetComponent<Canvas>();
    }

    void Start(){
        m_targetLayer = AeroplaneController.targetLayer;

        if(!isLocalPlayer){
    		Destroy(canvas.gameObject);
    		this.enabled = false;
    		return;
    	}
		       
        meter.GetMeterBar(canvas.transform);
        shieldBar = GetChildObject(canvas.gameObject.transform, "ShieldBar").GetComponent<Slider>();
        healthBar = GetChildObject(canvas.gameObject.transform, "HealthBar").GetComponent<Slider>();
        missileLock = GetChildObject(canvas.gameObject.transform, "MissileLock").GetComponent<Transform>();
        meterText = GetChildObject(canvas.gameObject.transform, "MeterText").GetComponent<Text>(); 

        //missiles
        missileLockGO = missileLock.gameObject;
        missileLockImage = missileLock.GetComponent<Image>();

        //healthBar
        playerHealth = GetComponent<Target>().maxHealth;
        healthBar.maxValue = playerHealth;
        healthBar.value = playerHealth;

        //shieldBar
        playerShieldHealth = GetComponent<Target>().shieldHealth;
        shieldBar.maxValue = playerShieldHealth;
        shieldBar.value = 0;
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

    void Update()
    {
    	sceneEnemies = FindObjectsWithLayer(m_targetLayer);
    	InstantiateTargetBox();
    	CalculteTargetBoxPosition();
        UpdateWeaponHUD();
        UpdateMeterText();
    }

    void UpdateMeterText(){
        if(!meter.startingPhase){
            if(gameObject.layer == LayerMask.NameToLayer("New Republic")){
                if(meter.rebelAttack){
                    meterText.text = "Assault Cruisers";
                }else{
                    meterText.text = "Defend Frigattes";
                }
            }
            if(gameObject.layer == LayerMask.NameToLayer("Galactic Empire")){
                if(meter.impAttack){
                    meterText.text = "Attack Frigattes";
                }else{
                    meterText.text = "Defend Cruisers";
                }
            }
        }else{
            meterText.text = "Engage in Fight";
        }
    }

    public void SetHealth(float hp){
        if(canvas != null)
            healthBar.value = hp;
    }

    public void SetShieldHealth(float sh){
        if(canvas != null)
            shieldBar.value = sh;
    }

    void CheckIfReloading(){
        float timers=0;
        for (int i = 0; i < Shooting.MissileReloadTimers.Count; i++) {
            timers += Shooting.MissileReloadTimers[i];
        }
        if(timers == 0){
            track=true;
        }else{
            track=false;
        }
        if(Shooting.MissileLocked){
            track=true;
        }
        if(Shooting.MissileTracking){
            track=true;
        }
    }

    void UpdateWeaponHUD(){
        if(Shooting.Target == null){missileLockGO.SetActive(false);return;} 

        var targetDistance = Vector3.Distance(AeroplaneController.Rigidbody.position, Shooting.Target.Position);
        var targetPos = TransformToHUDSpace(Shooting.Target.Position);
        var missileLockPos = Shooting.MissileLocked ? targetPos : TransformToHUDSpace(AeroplaneController.Rigidbody.position + Shooting.MissileLockDirection * targetDistance);

        // CheckIfReloading();

        if (track && Shooting.MissileTracking && missileLockPos.z > 0) {
            missileLockGO.SetActive(true);
            missileLock.localPosition = new Vector3(missileLockPos.x, missileLockPos.y, 0);
        } else {
            missileLockGO.SetActive(false);
        }

        if (Shooting.MissileLocked) {
            missileLockImage.color = lockColor;
        } else {
            missileLockImage.color = normalColor;
        }
    }

    void InstantiateTargetBox(){
    	if(sceneEnemies == null){
    		for (int i=0; i < TargetBoxList.Count; i++){
    			Destroy(TargetBoxList[i]);
    			TargetBoxList.RemoveAt(i);
    		}
    		return;
    	}
        if(sceneEnemies.Length < TargetBoxList.Count){
        	int lastTargetBoxInt = TargetBoxList.Count - 1;
        	Destroy(TargetBoxList[lastTargetBoxInt]);
        	TargetBoxList.RemoveAt(lastTargetBoxInt);
        }

    	foreach (AeroplaneController enemy in sceneEnemies){
	    	if(sceneEnemies.Length > TargetBoxList.Count){
	    		Image TargetBoxClone = Instantiate(TargetBox, TargetBox.transform.position, TargetBox.transform.rotation);
	    		TargetBoxClone.transform.SetParent(canvas.transform, false);
	    		TargetBoxList.Add(TargetBoxClone);
	    	}
        }
    }

    void CalculteTargetBoxPosition(){
    	// var sceneEnemies = FindObjectsWithLayer(LayerMask.NameToLayer(targetLayer));

    	float minX = TargetBox.GetPixelAdjustedRect().width;
    	float maxX = Screen.width - minX;

    	float minY = TargetBox.GetPixelAdjustedRect().height;
    	float maxY = Screen.height - minY;

		if(sceneEnemies != null){
    		for (int i=0; i < sceneEnemies.Length; i++){
		    	pos = Camera.main.WorldToScreenPoint(sceneEnemies[i].transform.position);

		    	if(Vector3.Dot((sceneEnemies[i].transform.position - transform.position), transform.forward) < 0){
		    		if(pos.x < Screen.width / 2){
		    			pos.x = maxX;
		    		}else{
		    			pos.x = minX;
		    		}
		    	}
		    	pos.x = Mathf.Clamp(pos.x, minX, maxX);
		    	pos.y = Mathf.Clamp(pos.y, minY, maxY);
		    	TargetBoxList[i].transform.position = pos;
    		}
        }

    }

    public AeroplaneController[] FindObjectsWithLayer (int layer) {
    	AeroplaneController[] goArray = FindObjectsOfType<AeroplaneController>() as AeroplaneController[];
    	List<AeroplaneController> goList = new List<AeroplaneController>();
    	for (int i = 0; i < goArray.Length; i++) { 
    		if (goArray[i].gameObject.layer == layer) { 
    			goList.Add(goArray[i]); 
    		} 
    	} 
    	if (goList.Count == 0) { return null; } 
    	return goList.ToArray(); 
 	}
}
