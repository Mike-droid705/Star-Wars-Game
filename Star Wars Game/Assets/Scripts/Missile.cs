using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof (Rigidbody))]
public class Missile : NetworkBehaviour {
    [SerializeField]
    float lifetime;
    [SerializeField]
    float speed;
    [SerializeField]
    float trackingAngle;
    [SerializeField]
    public float damage;
    [SerializeField]
    float damageRadius;
    [SerializeField]
    float turningGForce;
    [SerializeField]
    public LayerMask collisionMask;
    // [SerializeField] new MeshRenderer renderer;
    [SerializeField] GameObject explosionGraphic;

    Target owner;
    Target target;
    bool exploded;
    Vector3 lastPosition;
    float timer;

    public Rigidbody Rigidbody { get; private set; }

    [ClientRpc]
    public void Launch(Target owner, Target _target) {
        this.owner = owner;
        target = _target;

        Rigidbody = GetComponent<Rigidbody>();

        lastPosition = Rigidbody.position;
        timer = lifetime;
    }

    void Explode() {
        if (exploded) return;

        timer = lifetime;
        Rigidbody.isKinematic = true;
        // renderer.enabled = false;
        exploded = true;
        explosionGraphic.SetActive(true);

        var hits = Physics.OverlapSphere(Rigidbody.position, damageRadius, collisionMask.value);

        if(!isServer){return;}
        foreach (var hit in hits) {
            Target other = hit.GetComponent<Collider>().transform.parent.parent.GetComponent<Target>();

            if (other != null && other != owner) {
                other.ApplyDamage(damage, 0);
            }
        }
        Destroy(gameObject);
    }

    void CheckCollision() {
        //missile can travel very fast, collision may not be detected by physics system
        //use raycasts to check for collisions
        if(!isServer){return;}

        var currentPosition = Rigidbody.position;
        var error = currentPosition - lastPosition;
        var ray = new Ray(lastPosition, error.normalized);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, error.magnitude, collisionMask.value)) {
            Target other = hit.collider.gameObject.GetComponent<Target>();

            if (other == null || other != owner) {
                Rigidbody.position = hit.point;
                Explode();
            }
        }

        lastPosition = currentPosition;
    }

    void TrackTarget(float dt) {
        if (target == null) return;

        var targetPosition = Utilities.FirstOrderIntercept(Rigidbody.position, Vector3.zero, speed, target.Position, target.Velocity);

        var error = targetPosition - Rigidbody.position;
        var targetDir = error.normalized;
        var currentDir = Rigidbody.rotation * Vector3.forward;

        //if angle to target is too large, explode
        if (Vector3.Angle(currentDir, targetDir) > trackingAngle) {
            Explode();
            return;
        }

        //calculate turning rate from G Force and speed
        float maxTurnRate = (turningGForce * 9.81f) / speed;  //radians / s
        var dir = Vector3.RotateTowards(currentDir, targetDir, maxTurnRate * dt, 0);

        Rigidbody.rotation = Quaternion.LookRotation(dir);
    }

    void FixedUpdate() {
        timer = Mathf.Max(0, timer - Time.fixedDeltaTime);

        //explode missile automatically after lifetime ends
        //timer is reused to keep missile graphics alive after explosion
        if (timer == 0) {
            if (exploded) {
                Destroy(gameObject);
            } else {
                Explode();
            }
        }

        if (exploded) return;

        CheckCollision();
        TrackTarget(Time.fixedDeltaTime);

        //set speed to direction of travel
        Rigidbody.velocity = Rigidbody.rotation * new Vector3(0, 0, speed);
    }
}