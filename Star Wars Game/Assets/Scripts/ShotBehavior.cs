using UnityEngine;
using System.Collections;
using Mirror;

public class ShotBehavior : NetworkBehaviour {

	[SerializeField] public float bulletHullDamage;
    [SerializeField] public float bulletShieldDamage;
	[SerializeField] public Target owner;
    [SerializeField] public Shooting ownerShooting;
	[SerializeField]  float lifetime;
    [SerializeField]  float width;
    [SerializeField]  GameObject hitParticle;
    [SerializeField] public LayerMask collisionMask;

	Vector3 lastPosition;
    float startTime;
    new Rigidbody rigidbody;

    void Awake(){
    	rigidbody = GetComponent<Rigidbody>();
    }

    [ClientRpc]
    public void Launch(Vector3 position, Quaternion rotation, Vector3 velocity, Target target, Shooting shooting){
        startTime = Time.time;
        rigidbody.WakeUp();

        transform.position = position;
        transform.rotation = rotation;
        gameObject.SetActive(true);
        rigidbody.velocity = velocity;

        owner = target;
        ownerShooting = shooting;

        lastPosition = rigidbody.position;
    }

    void Deactivate(){
        ownerShooting.PutBackInPool(this.gameObject);
        rigidbody.Sleep();
    }

    void FixedUpdate() {
        if(ownerShooting == null){
            Destroy(this.gameObject);
        }

        if (Time.time > startTime + lifetime) {
            Deactivate();
            return;
        }

        var diff = rigidbody.position - lastPosition;
        lastPosition = rigidbody.position;

        Ray ray = new Ray(lastPosition, diff.normalized);
        RaycastHit hit;

        if(!isServer){return;}
        
        if (Physics.SphereCast(ray, width, out hit, diff.magnitude, collisionMask.value)) {
            var hfx = Instantiate(hitParticle, hit.point, hitParticle.transform.rotation);
            Destroy(hfx, hfx.GetComponent<ParticleSystem>().main.duration);
            if(hit.collider.transform.parent.parent.GetComponent<Target>() != null){
                Target other = hit.collider.transform.parent.parent.GetComponent<Target>();
                if (other != owner) {
                    other.ApplyDamage(bulletHullDamage, bulletShieldDamage);
                }
            }
            Deactivate();
        }
    }
}
