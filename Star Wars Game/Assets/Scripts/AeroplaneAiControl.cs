using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

[RequireComponent(typeof (AeroplaneController))]
[RequireComponent(typeof (FindTarget))]
public class AeroplaneAiControl : NetworkBehaviour
{
    //todo avoid objects
    // This script represents an AI 'pilot' capable of flying the plane towards a designated target.
    // It sends the equivalent of the inputs that a user would send to the Aeroplane controller.
    [SerializeField] private float m_ShootingAngle = 10f; 
    [SerializeField]float attackRange = 500f;        // How sensitively the AI applies the roll controls 
    [SerializeField] private bool isTargetingOneEnemy;    
    [SerializeField] private float m_RollSensitivity = .2f;         // How sensitively the AI applies the roll controls
    [SerializeField] private float m_PitchSensitivity = .5f;        // How sensitively the AI applies the pitch controls
    [SerializeField] private float m_LateralWanderDistance = 5;     // The amount that the plane can wander by when heading for a target
    [SerializeField] private float m_LateralWanderSpeed = 0.11f;    // The speed at which the plane will wander laterally
    [SerializeField] private float m_MaxClimbAngle = 45;            // The maximum angle that the AI will attempt to make plane can climb at
    [SerializeField] private float m_MaxRollAngle = 45;             // The maximum angle that the AI will attempt to u
    [SerializeField] private float m_SpeedEffect = 0.01f;           // This increases the effect of the controls based on the plane's speed.
    [SerializeField] private float m_TakeoffHeight = 20;  
    [SerializeField] float groundCollisionDistance;  
    [SerializeField] LayerMask groundCollisionMask;        // the AI will fly straight and only pitch upwards until reaching this height

    private float health = 100;
    private AeroplaneController m_AeroplaneController;  // The aeroplane controller that is used to move the plane
    private Shooting m_Shooting;
    private FindTarget findTarget;

    private AeroplaneController plane_Target;                    // the target to fly towards
    private Target m_Target;                    // the target to fly towards
    private float m_RandomPerlin;                       // Used for generating random point on perlin noise so that the plane will wander off path slightly
    private bool m_TakenOff;                            // Has the plane taken off yet
    private int m_targetLayer;    

    // setup script properties
    private void Start()
    {
        // get the reference to the aeroplane controller, so we can send move input to it and read its current state.
        m_AeroplaneController = GetComponent<AeroplaneController>();
        m_Shooting = GetComponent<Shooting>();
        findTarget = GetComponent<FindTarget>();

        // pick a random perlin starting point for lateral wandering
        m_RandomPerlin = Random.Range(0f, 100f);

        m_targetLayer = m_AeroplaneController.targetLayer;
    }


    // reset the object to sensible values
    public void Reset()
    {
        m_TakenOff = false;
    }

    void Update(){
        if(isServer == false){return;}
        if(!m_AeroplaneController.m_Immobilized){
            plane_Target = findTarget.SetTargetEnemy(m_targetLayer, isTargetingOneEnemy);
            if(plane_Target){
                m_Target = plane_Target.GetComponent<Target>();
            }
        }else{
            m_Target = null;
        }
    }

    void CheckIfAhead(){
        if(m_Shooting){
            if (m_Target){
                Vector3 targetDir = m_Target.Position - transform.position;
                float angle = Vector3.Angle(targetDir, transform.forward);
                float distanceToEnemy = Vector3.Distance(m_Target.transform.position, gameObject.transform.position);

                if (angle < m_ShootingAngle && distanceToEnemy <= attackRange){
                    m_Shooting.Fire(true);
                }else{
                    m_Shooting.Fire(false);
                }
            }else{
                m_Shooting.Fire(false);
            }
        }
    }

    // fixed update is called in time with the physics system update
    private void FixedUpdate()
    {
        CheckIfAhead();

        if(isServer == false){return;}
        if (m_Target)
        {
            // make the plane wander from the path, useful for making the AI seem more human, less robotic.
            Vector3 targetPos = m_Target.Position +
                                transform.right*
                                (Mathf.PerlinNoise(Time.time*m_LateralWanderSpeed, m_RandomPerlin)*2 - 1)*
                                m_LateralWanderDistance;

            // adjust the yaw and pitch towards the target
            Vector3 localTarget = transform.InverseTransformPoint(targetPos);
            float targetAngleYaw = Mathf.Atan2(localTarget.x, localTarget.z);
            float targetAnglePitch = -Mathf.Atan2(localTarget.y, localTarget.z);


            // Set the target for the planes pitch, we check later that this has not passed the maximum threshold
            targetAnglePitch = Mathf.Clamp(targetAnglePitch, -m_MaxClimbAngle*Mathf.Deg2Rad,
                                           m_MaxClimbAngle*Mathf.Deg2Rad);

            // calculate the difference between current pitch and desired pitch
            float changePitch = targetAnglePitch - m_AeroplaneController.PitchAngle;

            // AI always applies gentle forward throttle
            const float throttleInput = 0.5f;

            // AI applies elevator control (pitch, rotation around x) to reach the target angle
            float pitchInput = changePitch*m_PitchSensitivity;

            // clamp the planes roll
            float desiredRoll = Mathf.Clamp(targetAngleYaw, -m_MaxRollAngle*Mathf.Deg2Rad, m_MaxRollAngle*Mathf.Deg2Rad);
            float yawInput = 0;
            float rollInput = 0;
            if (!m_TakenOff)
            {
                // If the planes altitude is above m_TakeoffHeight we class this as taken off
                if (m_AeroplaneController.Altitude > m_TakeoffHeight)
                {
                    m_TakenOff = true;
                }
            }
            else
            {
                // now we have taken off to a safe height, we can use the rudder and ailerons to yaw and roll
                yawInput = targetAngleYaw;
                rollInput = -(m_AeroplaneController.RollAngle - desiredRoll)*m_RollSensitivity;
            }

            // adjust how fast the AI is changing the controls based on the speed. Faster speed = faster on the controls.
            float currentSpeedEffect = 1 + (m_AeroplaneController.ForwardSpeed*m_SpeedEffect);
            rollInput *= currentSpeedEffect;
            pitchInput *= currentSpeedEffect;
            yawInput *= currentSpeedEffect;

            var velocityRot = Quaternion.LookRotation(m_AeroplaneController.Rigidbody.velocity.normalized);
            var ray = new Ray(m_AeroplaneController.Rigidbody.position, velocityRot * Quaternion.Euler(15, 0, 0) * Vector3.forward);

            if (Physics.Raycast(ray, groundCollisionDistance + m_AeroplaneController.LocalAngularVelocity.z, groundCollisionMask.value)){
                rollInput=0;
                pitchInput=-3;
                yawInput=0;
            }
            // pass the current input to the plane (false = because AI never uses air brakes!)
            m_AeroplaneController.Move(rollInput, pitchInput, yawInput, throttleInput, false);
        }
        else
        {
            // no target set, send zeroed input to the plane
            m_AeroplaneController.Move(-1, 0, 0, 0, false);
        }
    }
}

