using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof (AeroplaneController))]
public class AeroplaneUserControl : MonoBehaviour
{
    // these max angles are only used on mobile, due to the way pitch and roll input are handled
    public float maxRollAngle = 80;
    public float maxPitchAngle = 80;
    [SerializeField] float flyingSensitivity = 50;

    // reference to the aeroplane that we're controlling
    private AeroplaneController m_Aeroplane;
    private Shooting m_Shooting;
    private Target m_Target;
    private float m_Throttle;
    private bool m_AirBrakes;
    private float m_Yaw;

    public Rigidbody Rigidbody { get; private set; }


    private void Awake()
    {
        // Set up the reference to the aeroplane controller.
        m_Aeroplane = GetComponent<AeroplaneController>();
        Rigidbody = GetComponent<Rigidbody>();
        m_Shooting = GetComponent<Shooting>();
        m_Target = GetComponent<Target>();
    }

    private void DetectShootInput(){
        if(Input.GetButton("Fire1")){
            m_Shooting.Fire(true);
        }else{
            m_Shooting.Fire(false);
        }
    }

    private void DetectMissileInput(){
        if(Input.GetButtonUp("Right Auxillary")){
            m_Shooting.TryFireMissile();
        }
        if(Input.GetButton("Right Auxillary")){
            m_Shooting.StopMissileTrack(false);
        }else{
            m_Shooting.StopMissileTrack(true);
        }
    }

    private void DetectShieldInput(){
        if(Input.GetButton("Left Auxillary")){
            m_Target.ActivateShield();
        }
    }

    void Update(){
        DetectMissileInput();
        DetectShootInput();
        DetectShieldInput();
    }

    private void FixedUpdate()
    {
        Vector3 mousePos = Input.mousePosition;
        float relativeMousePosY = (mousePos.y - Screen.height/2)/flyingSensitivity;
        float relativeMousePosX = (mousePos.x - Screen.width/2)/flyingSensitivity;
        // Read input for the pitch, yaw, roll and throttle of the aeroplane.
        float roll = CrossPlatformInputManager.GetAxis("Horizontal");
        float pitch = relativeMousePosY;
        m_AirBrakes = CrossPlatformInputManager.GetButton("Brake");
        m_Yaw = relativeMousePosX;
        m_Throttle = CrossPlatformInputManager.GetAxis("Vertical");
        // Pass the input to the aeroplane
        m_Aeroplane.Move(roll, pitch, m_Yaw, m_Throttle, m_AirBrakes);
    }
}
