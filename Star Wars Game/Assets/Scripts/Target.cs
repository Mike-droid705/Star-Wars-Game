using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof (Rigidbody))]
public class Target : NetworkBehaviour {
    [Header("General")]
    [SerializeField] new string name;
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private float points = 5f; 
    [SerializeField] private GameObject deathParticle; 
    [SerializeField] private GameObject[] flameParticles; 
    [SerializeField] private GameObject[] steamParticle; 
    [SerializeField] private float destroyInterval;  

    [Header("Shields")]
    [SerializeField] private bool isUsingShields; 
    [SerializeField] public float shieldHealth; 
    [SerializeField] private float shieldCooldown; 
    [SerializeField] private GameObject ShieldObject; 

    float currentHealth = 100f;
    float currentShieldHealth = 0f;
    private bool shieldActive = false; 
    float currentShieldCooldown = 0;
    float dt;
    public bool destroyed=false;

    public string Name {
        get {
            return name;
        }
    }

    public Vector3 Position {
        get {
            return rigidbody.position;
        }
    }

    public Vector3 Velocity {
        get {
            return rigidbody.velocity;
        }
    }

    public AeroplaneController AeroplaneController { get; private set; }
    private Meter meter;
    private HitDecal HitDecal;
    PlayerHUD PlayerHUD;
    TargetableObjects targetableObjects;

    new Rigidbody rigidbody;

    List<Missile> incomingMissiles;
    const float sortInterval = 0.5f;
    float sortTimer;

    void Awake() {
        dt = Time.fixedDeltaTime;

        rigidbody = GetComponent<Rigidbody>();
        AeroplaneController = GetComponent<AeroplaneController>();
        PlayerHUD = GetComponent<PlayerHUD>();
        meter = FindObjectOfType<Meter>();
        targetableObjects = FindObjectOfType<TargetableObjects>();
        HitDecal = GetComponent<HitDecal>();

        incomingMissiles = new List<Missile>();
        currentHealth = maxHealth;
        currentShieldHealth = shieldHealth;

        if(AeroplaneController){
            targetableObjects.AddToList(this.GetComponent<AeroplaneController>());
        }
    }

    void Start(){
        if(gameObject.tag == "Flagship" || gameObject.tag == "CapitalShip"){
            ActivateShield();
        }
    }

    void FixedUpdate() {
        sortTimer = Mathf.Max(0, sortTimer - Time.fixedDeltaTime);

        if (sortTimer == 0) {
            SortIncomingMissiles();
            sortTimer = sortInterval;
        }
    }

    void Update(){
        if(currentHealth <= 0){
            if(!destroyed){
                if(gameObject.tag == "Player"){
                    Kill();
                }else{
                    AIKill();
                }
            }  
        }
        if(currentShieldHealth <= 0){
            shieldActive=false;
            currentShieldCooldown=shieldCooldown;
        }
        if(isUsingShields){
            ShieldObject.SetActive(shieldActive);
        }
        if(shieldActive){
            if(gameObject.tag == "Player"){
                PlayerHUD.SetShieldHealth(currentShieldHealth);
            }
        }
        currentShieldCooldown = Mathf.Max(0, currentShieldCooldown - dt);
    }

    public void ToggleShieldUsage(){
        shieldActive=false;
    }

    public void ActivateShield(){
        if(shieldActive == false && currentShieldCooldown == 0){
            shieldActive=true;
        }
    }

    [ClientRpc]
    public void ApplyDamage(float hullDamage, float shieldDamage){
        // if(isServer == false){return;}
        if(gameObject.tag == "Decal"){
            HitDecal.DealDamage(hullDamage);
            return;
        }
        if(shieldActive){
            currentShieldHealth -= hullDamage;
            currentShieldHealth -= shieldDamage;
            if(gameObject.tag == "Player"){
                PlayerHUD.SetShieldHealth(currentShieldHealth);
            }
        }else{
            currentHealth -= hullDamage;                
        }
        if(gameObject.tag == "Player"){
            PlayerHUD.SetHealth(currentHealth);
        } 
    }

    [ClientRpc]
    void Kill(){
        targetableObjects.RemoveFromList(this.GetComponent<AeroplaneController>());
        meter.ChangeMeter(gameObject.layer, points);
        var dfx = Instantiate(deathParticle, transform.position, deathParticle.transform.rotation);
        Destroy(dfx, dfx.GetComponent<ParticleSystem>().main.duration);
        Destroy(gameObject, destroyInterval);
        destroyed=true;
    }

    void AIKill(){
        destroyed=true;
        if(AeroplaneController){
            targetableObjects.RemoveFromList(this.GetComponent<AeroplaneController>());
        }
        if(gameObject.tag == "Corvette" || gameObject.tag == "CapitalShip"){
            AeroplaneController.Immobilize();
            foreach(GameObject flameParticle in flameParticles){
                flameParticle.SetActive(true);
            }
        }
        meter.ChangeMeter(gameObject.layer, points);
        var dfx = Instantiate(deathParticle, transform.position, deathParticle.transform.rotation);
        Destroy(dfx, dfx.GetComponent<ParticleSystem>().main.duration);
        Destroy(gameObject, destroyInterval);
    }

    void SortIncomingMissiles() {
        var position = Position;

        if (incomingMissiles.Count > 0) {
            incomingMissiles.Sort((Missile a, Missile b) => {
                var distA = Vector3.Distance(a.Rigidbody.position, position);
                var distB = Vector3.Distance(b.Rigidbody.position, position);
                return distA.CompareTo(distB);
            });
        }
    }

    public Missile GetIncomingMissile() {
        if (incomingMissiles.Count > 0) {
            return incomingMissiles[0];
        }

        return null;
    }

    public void NotifyMissileLaunched(Missile missile, bool value) {
        if (value) {
            incomingMissiles.Add(missile);
            SortIncomingMissiles();
        } else {
            incomingMissiles.Remove(missile);
        }
    }
}