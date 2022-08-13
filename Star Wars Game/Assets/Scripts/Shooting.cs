using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Shooting : NetworkBehaviour
{
	[Header("Lasers")]
	[SerializeField]Transform[] shootingPositions;
	[SerializeField]float cannonFireRate = 8f;
	[SerializeField]float cannonSpread;
	[SerializeField]GameObject bulletPrefab;
	[SerializeField]float bulletSpeed;
	[SerializeField]float bulletHullDamage = 20f;
	[SerializeField]float bulletShieldDamage = 0f;
	[SerializeField]bool isPartOfFlagship = false;
	[SerializeField]bool isPartOfCapitalShip = false;
	[SerializeField]float strongBulletHullDamage = 20f;
	[SerializeField]float strongBulletShieldDamage = 20f;


	[Header("Laser Reloads")]
	[SerializeField]float cannonCapacity = 200f;
	[SerializeField]float cannonReloadRate = 3f;
	[SerializeField]bool isUsingReloads = true;


	[Header("Missiles")]
	[SerializeField]bool isUsingMissiles;
    [SerializeField]float missileDamage;
	[SerializeField]float missileReloadTime;
    [SerializeField]float missileDebounceTime;
    [SerializeField]GameObject missilePrefab;
    [SerializeField]List<Transform> hardpoints;
    [SerializeField]float lockRange;
    [SerializeField]float lockSpeed;
    [SerializeField]float lockAngle;

    bool stopMissileTrack = false;
	float missileDebounceTimer;
	int missileIndex;
	Vector3 missileLockDirection;
	List<float> missileReloadTimers;
	public bool MissileLocked { get; private set; }
	public bool MissileTracking { get; private set; }
	float curBulletHullDamage;
	float curBulletShieldDamage;

	float cannonFiringTimer;
	bool isCannonFiring = false;
	bool isCannonReloading = false;

	public List<float> MissileReloadTimers {get {return missileReloadTimers;}}
	public Vector3 MissileLockDirection {get {return Rigidbody.rotation * missileLockDirection;}}
	public Target Target {get {return target;}}

	float dt;
	int targetLayer;
	AeroplaneController plane_target;
	Target target;

	private Rigidbody Rigidbody;
	private FindTarget findTarget;
	private TurretAccuracy TurretAccuracy;
	private Meter meter;

	float bulletsFired = 0;

	GameObject deactivatedGO;

	[Header("Pool")]
    public int maxSize = 20;

    [Header("Debug")]
    [SerializeField] Queue<GameObject> pool;
    [SerializeField] int currentCount;


    void Start()
    {
    	pool = new Queue<GameObject>();
    }

    GameObject CreateNew()
    {
        if (currentCount > maxSize)
        {
            Debug.LogError($"Pool has reached max size of {maxSize}");
            return null;
        }

        // use this object as parent so that objects dont crowd hierarchy
        GameObject next = Instantiate(bulletPrefab);
        next.name = $"{bulletPrefab.name}_pooled_{currentCount}";
        next.SetActive(false);
        currentCount++;
        return next;
    }

    public GameObject GetFromPool()
    {
        GameObject next = pool.Count > 0
            ? pool.Dequeue() // take from pool
            : CreateNew(); // create new because pool is empty

        // CreateNew might return null if max size is reached
        if (next == null) { return null; }

        // set position/rotation and set active
        return next;
    }

    public void PutBackInPool(GameObject spawned)
    {
        // disable object
        spawned.SetActive(false);

        // add back to pool
        pool.Enqueue(spawned);
    }

	void Awake(){
		dt = Time.fixedDeltaTime;

		Rigidbody = GetComponent<Rigidbody>();
		findTarget = GetComponent<FindTarget>();
		if(isPartOfCapitalShip){
			meter = FindObjectOfType<Meter>();
		}
		if(isPartOfFlagship){
			TurretAccuracy = transform.parent.GetComponent<TurretAccuracy>();
		}

		curBulletHullDamage = bulletHullDamage;
		curBulletShieldDamage = bulletShieldDamage;

		missileReloadTimers = new List<float>(hardpoints.Count);

		foreach (var h in hardpoints) {
            missileReloadTimers.Add(0);
        }

        missileLockDirection = Vector3.forward;
        //layer setup
        if(gameObject.layer == 9){
            targetLayer = 8;
        }else if(gameObject.layer == 8){
            targetLayer = 9;
        }
	}

	void Update(){
		if(isUsingMissiles){
			UpdateTarget();
			if(gameObject.tag == "Turret") return;
			UpdateMissileLock();
		}
		if(isPartOfFlagship){
			cannonSpread=TurretAccuracy.m_cannonSpread;
			cannonFireRate=TurretAccuracy.m_cannonFireRate;
		}
		if(isPartOfCapitalShip){
			if(gameObject.layer == LayerMask.NameToLayer("New Republic")){
                if(meter.rebelAttack){
                    curBulletHullDamage = strongBulletHullDamage;
                    curBulletShieldDamage = strongBulletShieldDamage;
                }else{
                    curBulletHullDamage = bulletHullDamage;
                    curBulletShieldDamage = bulletShieldDamage;
                }
            }
            if(gameObject.layer == LayerMask.NameToLayer("Galactic Empire")){
                if(meter.impAttack){
                    curBulletHullDamage = strongBulletHullDamage;
                    curBulletShieldDamage = strongBulletShieldDamage;
                }else{
                    curBulletHullDamage = bulletHullDamage;
                    curBulletShieldDamage = bulletShieldDamage;
                }
            }
			if(meter.startingPhase){
				curBulletHullDamage = strongBulletHullDamage;
                curBulletShieldDamage = strongBulletShieldDamage;
			}
		}
	}   

    void FixedUpdate(){
        UpdateCannons();
        UpdateWeaponCooldown();
    }

	void UpdateTarget(){
		//missile target
		plane_target = findTarget.SetTargetEnemy(targetLayer, false);
		if(plane_target){
			target = plane_target.GetComponent<Target>();
		}
	}

	public void TryFireMissile() {
        //try all available missiles
        for (int i = 0; i < hardpoints.Count; i++) {
            var index = (missileIndex + i) % hardpoints.Count;
            if (missileDebounceTimer == 0 && missileReloadTimers[index] == 0) {
                if(gameObject.tag == "Turret"){
                    AIFireMissile(index);
                }else if(gameObject.tag == "Player"){
                    CmdFireMissile(index);
                }

                missileIndex = (index + 1) % hardpoints.Count;
                missileReloadTimers[index] = missileReloadTime;
                missileDebounceTimer = missileDebounceTime;

                break;
            }
        }
    }

    [Command]
    void CmdFireMissile(int index) {
        var hardpoint = hardpoints[index];

        var missileGO = Instantiate(missilePrefab, hardpoint.position, hardpoint.rotation);
        var missile = missileGO.GetComponent<Missile>();

        missile.damage = missileDamage;
        missile.collisionMask = (1<<targetLayer);

        NetworkServer.Spawn(missileGO);
    	missile.Launch(this.GetComponent<Target>(), MissileLocked ? target : null);
    }

    void AIFireMissile(int index) {
        if(isServer == false){return;}
        var hardpoint = hardpoints[index];

        var missileGO = Instantiate(missilePrefab, hardpoint.position, hardpoint.rotation);
        var missile = missileGO.GetComponent<Missile>();

        missile.damage = missileDamage;
        missile.collisionMask = (1<<targetLayer);

        NetworkServer.Spawn(missileGO);
        missile.Launch(this.GetComponent<Target>(), target);
    }


	void UpdateWeaponCooldown(){
		missileDebounceTimer = Mathf.Max(0, missileDebounceTimer - dt);
		cannonFiringTimer = Mathf.Max(0, cannonFiringTimer - dt);
		//Cannon Reloading Function
		if(!isCannonFiring){
			bulletsFired-=cannonReloadRate;
		}

		if(bulletsFired >= cannonCapacity){
			isCannonReloading = true;
		}else{
			isCannonReloading = false;
		}

		if(!isUsingReloads){
			isCannonReloading = false;
		}

		bulletsFired = Mathf.Max(0, bulletsFired);

		for (int i = 0; i < missileReloadTimers.Count; i++) {
            missileReloadTimers[i] = Mathf.Max(0, missileReloadTimers[i] - dt);
        }
	}

	public void StopMissileTrack(bool b){
		if(!MissileLocked){
			stopMissileTrack = b;
		}
	}

	void UpdateMissileLock() {
        //default neutral position is forward
        Vector3 targetDir = Vector3.forward;
        MissileTracking = false;

        if(stopMissileTrack) return;

        if (target != null) {
            var error = target.Position - Rigidbody.position;
            var errorDir = Quaternion.Inverse(Rigidbody.rotation) * error.normalized; //transform into local space

            if (error.magnitude <= lockRange && Vector3.Angle(Vector3.forward, errorDir) <= lockAngle) {
                MissileTracking = true;
                targetDir = errorDir;
            }
        }

        //missile lock either rotates towards the target, or towards the neutral position
        missileLockDirection = Vector3.RotateTowards(missileLockDirection, targetDir, Mathf.Deg2Rad * lockSpeed * dt, 0);

        MissileLocked = target != null && MissileTracking && Vector3.Angle(missileLockDirection, targetDir) < lockSpeed * dt;
    }

	public void Fire(bool _value){
		isCannonFiring = _value;
	}

	void UpdateCannons(){
		if (isCannonFiring && cannonFiringTimer == 0 && !isCannonReloading) {
            cannonFiringTimer = 1f / cannonFireRate;

            if(gameObject.tag == "Player"){
            	CmdShoot();
        	}else{
            	AIShoot();
        	}
        }
	}

	[Command]
	void CmdShoot(){
		var spread = Random.insideUnitCircle * cannonSpread;

        foreach(Transform shootingPos in shootingPositions){
            // GameObject bullet = Instantiate(bulletPrefab, shootingPos.position, shootingPos.rotation * Quaternion.Euler(spread.x, spread.y, 0)) as GameObject;
            GameObject bullet = GetFromPool();

            ShotBehavior bulletClone = bullet.GetComponent<ShotBehavior>();

            bulletClone.bulletHullDamage = curBulletHullDamage;
            bulletClone.bulletShieldDamage = curBulletShieldDamage;
            bulletClone.collisionMask = (1<<targetLayer);
        	bulletsFired++;
        	NetworkServer.Spawn(bullet);
        	// bulletClone.RpcOwnerSetup(this.GetComponent<Target>(), this);
            bulletClone.Launch(
            	shootingPos.position,
            	shootingPos.rotation * Quaternion.Euler(spread.x, spread.y, 0),
            	shootingPos.forward * bulletSpeed,
                this.GetComponent<Target>(), 
                this);
        }
	}

	void AIShoot(){
        if(isServer == false){return;}
		var spread = Random.insideUnitCircle * cannonSpread;

        foreach(Transform shootingPos in shootingPositions){
            GameObject bullet = GetFromPool();

            ShotBehavior bulletClone = bullet.GetComponent<ShotBehavior>();

            bulletClone.bulletHullDamage = curBulletHullDamage;
            bulletClone.bulletShieldDamage = curBulletShieldDamage;
            bulletClone.collisionMask = (1<<targetLayer);
        	bulletsFired++;
        	NetworkServer.Spawn(bullet);
        	// bulletClone.RpcOwnerSetup(this.GetComponent<Target>(), this);
            bulletClone.Launch(
            	shootingPos.position,
            	shootingPos.rotation * Quaternion.Euler(spread.x, spread.y, 0),
            	shootingPos.forward * bulletSpeed,
                this.GetComponent<Target>(), 
                this);
        }
	}
}
