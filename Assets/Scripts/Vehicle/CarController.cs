using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using static CarConfig;
using static CarController;
using static NetworkPlayer;

public class CarController : MonoBehaviour
{
    private float moveInput;
    private float steerInput;
    private float currentSpeed = 0;
    private float realSpeed;
    public float maxSpeed;
    public float boostSpeed;
    private float steerDirection;
    private float driftTime = 0;
    private float boostTime = 0;
    public float raycastGroundDist;
    public float raycastCollisionDist;

    private bool isSliding = false;
    private bool touchingGround = false;
    private bool driftLeft = false;
    private bool driftRight = false;
    public bool isOnBoostZone = false;
    public bool applyBoostZone = false;
    private bool isCollidingWithGrid = false;
    private bool isFrontalCollision = false;
    private bool isRearCollision = false;

    public bool isDriftButtonPressed = false;
    private bool isVFXWheelPlaying = false;

    private readonly float outwardsDriftForce = 500;

    public AudioClip engineSnd;
    public AudioClip driftSnd;
    public float engineSndPitch;
    private AudioSource audioSource;
    public AudioSource driftSource;

    public Transform carModelTransform;
    public Rigidbody carRb;

    private PlayerInputActions inputActions;
    private Vector3 baseModelRotation;

    public float extraGravity;

    private float maxSpeedOriginalValue;

    public NetworkPlayerAudio networkPlayerAudio;
    public GameObject shield;
    public float bounceForce = 4f;

    private ulong ownerId;

    [Serializable]
    public struct KartConfig
    {
        public CarConfig config;
        public Kart kart;
    }

    public List<KartConfig> kartConfigs = new();

    private CarConfig config;

    public Player player;
    internal bool locked;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = engineSnd;
        audioSource.volume = 0.04f;
        audioSource.loop = true;
        audioSource.Play();

        driftSource.volume = 0.2f;
        driftSource.clip = driftSnd;
        audioSource.loop = true;
        driftSource.Play();

        ownerId = player.OwnerClientId;

        foreach (var item in kartConfigs)
        {
            if(item.kart == NetworkBehaviourCustom.ConnectedClientsInfo[ownerId].kart){
                item.config.gameObject.SetActive(true);
                config = item.config;
                Debug.Log("FOUND CONFIG");
                continue;
            }
            item.config.gameObject.SetActive(false);
        }

        carModelTransform = config.gameObject.transform;

        if (!transform.parent.GetComponent<NetworkObject>().IsOwner)
        {
            return;
        }
        
        carRb = GetComponent<Rigidbody>();
        inputActions = new();
        inputActions.Race.Enable();
        baseModelRotation = carModelTransform.localRotation.eulerAngles;


        maxSpeedOriginalValue = maxSpeed;

        //inputActions.Race.Drift.performed += Drift_performed;
    }

    private void Drift_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        transform.parent.GetComponent<Player>().IncrementLap();
    
    }

    void GetInputs()
    {
        if(player.menuOpen){
            moveInput = 0;
            steerInput = 0;
            steerDirection = 0;
            isDriftButtonPressed = false;
            return;
        }

        moveInput = inputActions.Race.Throttle.ReadValue<float>();
        steerInput = inputActions.Race.Steer.ReadValue<float>();
        steerDirection = steerInput > 0 ? 1 : steerInput < 0 ? -1 : 0;
        isDriftButtonPressed = inputActions.Race.Drift.ReadValue<float>() > 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("COLL2");
        if (collision.gameObject.CompareTag("Grid"))
        {
            Debug.Log("COLL");
            Vector3 bounceDirection = Vector3.Reflect(carRb.velocity.normalized, collision.contacts[0].normal);
            

            carRb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(locked){
            carRb.velocity = new Vector3(0, carRb.velocity.y, 0);
            return;
        }
        touchingGround = config.wheelList[0].collider.isGrounded;

        //UPDATE AUDIO
        NetworkPlayer networkPlayer = NetworkBehaviourCustom.GetNetworkPlayer(ownerId);
        networkPlayerAudio = networkPlayer.networkPlayerAudio;
        audioSource.pitch = networkPlayerAudio.kartAudioMoving;
        driftSource.volume = networkPlayerAudio.kartAudioDrifting;

        shield.SetActive(networkPlayer.shieldActive);

        if (!player.IsOwner) return;
        UpdateCollisionDiretion();

        if(player.doDamage){
            moveInput = 0;
            steerInput = 0;
            Move();
            return;
        }

        GetInputs();
        Move();
        Tiresteer();
        Steer();
        GroundNormalRotation();

        if (networkPlayer.appliedBoost) boostTime = 0.1f;

        Drift();
        Boosts();

        networkPlayerAudio.kartAudioMoving = Mathf.Lerp(.7f, 2f, Mathf.InverseLerp(0, boostSpeed, currentSpeed));
        networkPlayerAudio.kartAudioDrifting = driftLeft || driftRight ? 0.2f : 0f;

        //Debug.Log("MAX SPEED: " + maxSpeed + " | REAL SPEED: " + realSpeed + " | CURRENT SPEED: " + currentSpeed + " | GROUNDED: " + touchingGround + " | DRIFT: " + driftLeft + " " + driftRight + " | STEER DIR: " + steerDirection + " | BOOST TIME: " + boostTime + " | isOnBoostZone: " + isOnBoostZone + " | GRID_COL: " + isCollidingWithGrid);
    }

    private void Move()
    {
        if (!touchingGround) return;

        realSpeed = transform.InverseTransformDirection(carRb.velocity).z;

        if (isOnBoostZone)
        {
            currentSpeed = 23f;
        }
        else if (moveInput > 0 && !isFrontalCollision)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 0.5f);
        }
        else if (moveInput < 0 && !isRearCollision)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, -maxSpeed / 1.75f, Time.deltaTime * 1f);
        }
        else if (isFrontalCollision || isRearCollision)
        {
            carRb.velocity = Vector3.zero;
            currentSpeed = 0;
            return;
        }
        else {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * 1.5f);
        }

        Vector3 newVelocity = transform.forward * currentSpeed;
        newVelocity.y = carRb.velocity.y;
        carRb.velocity = newVelocity;
    }

    private void Steer()
    {

        Vector3 steerDirV3;
        float steerAmount;
        bool isSteerAvailable = !isOnBoostZone && touchingGround;

        
        if(driftLeft && !driftRight && isSteerAvailable)
        {
          
            steerDirection = steerInput < 0 ? -1.5f : -0.5f;
            carModelTransform.localRotation = Quaternion.Lerp(carModelTransform.localRotation, Quaternion.Euler(carModelTransform.localRotation.eulerAngles.x, baseModelRotation.y - 20f, carModelTransform.localRotation.eulerAngles.z), 5f * Time.deltaTime); ;

            if (isSliding && touchingGround)
            {
                carRb.AddForce(transform.right * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
            }
        }
        else if (driftRight && !driftLeft && isSteerAvailable)
        {
            steerDirection = steerInput > 0 ? 1.5f : 0.5f;
            carModelTransform.localRotation = Quaternion.Lerp(carModelTransform.localRotation, Quaternion.Euler(carModelTransform.localRotation.eulerAngles.x, baseModelRotation.y + 20f, carModelTransform.localRotation.eulerAngles.z), 5f * Time.deltaTime);

            
            if (isSliding && touchingGround)
            {
                carRb.AddForce(transform.right * -outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
            }
            
        }
        else
        {
            carModelTransform.localRotation = Quaternion.Lerp(carModelTransform.localRotation, Quaternion.Euler(carModelTransform.localRotation.eulerAngles.x, baseModelRotation.y, carModelTransform.localRotation.eulerAngles.z), 5f * Time.deltaTime);
        }
        

        if (!isSteerAvailable) return;
        steerAmount = realSpeed > 30 ? realSpeed / 3.5f * steerDirection : realSpeed / 1f * steerDirection;

        steerAmount *= touchingGround ? 1 : 0;

        steerDirV3 = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + steerAmount, transform.eulerAngles.z);
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, steerDirV3, 4f * Time.deltaTime);
        
    }

    private void Tiresteer()
    {
        foreach (var wheel in config.wheelList)
        {
            Vector3 wheelLocalEuler = wheel.wheelModel.transform.localEulerAngles;
            if (currentSpeed > 30)
            {

                wheel.wheelModel.transform.Rotate(-90 * Time.deltaTime * currentSpeed * 0.5f, 0, 0);
            }
            else
            {
                wheel.wheelModel.transform.Rotate(-90 * Time.deltaTime * realSpeed * 0.5f, 0, 0);
            }

            if (wheel.axel == Axel.Rear) continue;

            float newSteerAngle;
            if (steerInput < 0)
            {
                newSteerAngle = Mathf.LerpAngle(wheelLocalEuler.z, -25, 5 * Time.deltaTime);
            }
            else if (steerInput > 0)
            {
                newSteerAngle = Mathf.LerpAngle(wheelLocalEuler.z, 25, 5 * Time.deltaTime);
            }
            else
            {
                newSteerAngle = Mathf.LerpAngle(wheelLocalEuler.z, 0, 5 * Time.deltaTime);
            }
            wheel.wheelModel.transform.localEulerAngles = new Vector3(wheelLocalEuler.x, wheelLocalEuler.y, newSteerAngle);
        }
    }

    private void GroundNormalRotation()
    {
        RaycastHit hit;
        if(touchingGround && Physics.Raycast(transform.position, -transform.up, out hit, raycastGroundDist))
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up * 2, hit.normal) * transform.rotation, 7.5f * Time.deltaTime);
            applyBoostZone = isOnBoostZone;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0), 20.5f * Time.deltaTime);
        }
        Debug.DrawRay(transform.position, -transform.up * raycastGroundDist, Color.red);
    }

    private void Drift()
    {
        if (isDriftButtonPressed && touchingGround)
        {
            if(steerDirection > 0)
            {
                driftRight = true;
                driftLeft = false;
            }
            else if (steerDirection < 0)
            {
                driftLeft = true;
                driftRight = false;
            }
        }


        if (isDriftButtonPressed && currentSpeed > 5 && steerInput != 0)
        {
            driftTime += Time.deltaTime;

            if(driftTime >= 1.5 && driftTime < 4)
            {

            }
        }

        if (!isDriftButtonPressed || realSpeed < 5)
        {
            driftLeft = false;
            driftRight = false;
            isSliding = false;

            if(driftTime > 1.5 && driftTime < 4)
            {
                boostTime = 0.75f;
            }
            else if (driftTime >= 4 && driftTime < 7)
            {
                boostTime = 1.5f;
            }
            else if (driftTime >= 7)
            {
                boostTime = 2.5f;
            }

            driftTime = 0;
        }

        //VISH
        if(driftLeft || driftRight){
            if(!isVFXWheelPlaying){
                config.driftSmoke[0].GetComponent<ParticleSystem>().Play();
                config.driftSmoke[1].GetComponent<ParticleSystem>().Play();
                isVFXWheelPlaying = true;
            }
        }
        else{
            config.driftSmoke[0].GetComponent<ParticleSystem>().Stop();
            config.driftSmoke[1].GetComponent<ParticleSystem>().Stop();
            isVFXWheelPlaying = false;
        }
    }

    private void Boosts()
    {
        boostTime -= Time.deltaTime;
        if(boostTime > 0)
        {
            maxSpeed = boostSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, 1f * Time.deltaTime);
        }
        else
        {
            maxSpeed = maxSpeedOriginalValue;
        }
    }

    private void FixedUpdate()
    {
        if (applyBoostZone) return;
        carRb.AddForce(Vector3.down * extraGravity * carRb.mass);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag != "Grid") return;
        isCollidingWithGrid = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Grid") return;
        isCollidingWithGrid = false;
    }

    private void UpdateCollisionDiretion()
    {
        isFrontalCollision = false;
        isRearCollision = false;

        //MEGA GO-HORSE SPEEDCODE 9000 NO DRAWRAY
#if DEBUG
        Debug.DrawRay(transform.position, transform.forward * raycastCollisionDist, Color.green);
        Debug.DrawRay(transform.position, (transform.forward + transform.right) * raycastCollisionDist, Color.green);
        Debug.DrawRay(transform.position, (transform.forward + -transform.right) * raycastCollisionDist, Color.green);
        Debug.DrawRay(transform.position, -transform.forward * raycastCollisionDist, Color.green);
        Debug.DrawRay(transform.position, (-transform.forward + transform.right) * raycastCollisionDist, Color.green);
        Debug.DrawRay(transform.position, (-transform.forward + -transform.right) * raycastCollisionDist, Color.green);
#endif
        if (!isCollidingWithGrid) return;

        //FRONT COL
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            isFrontalCollision = true;
            return;
        }

        /*
        hits = Physics.RaycastAll(transform.position, transform.forward + transform.right, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            carRb.AddForce((-Vector3.right + -Vector3.forward)  * 50, ForceMode.Force);
            return;
        }

        hits = Physics.RaycastAll(transform.position, transform.forward + -transform.right, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            carRb.AddForce((Vector3.right + -Vector3.forward) * 50, ForceMode.Force);
            return;
        }
        */

        //REAR COL
        hits = Physics.RaycastAll(transform.position, -transform.forward, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            isRearCollision = true;
            return;
        }

        /*
        hits = Physics.RaycastAll(transform.position, -transform.forward + transform.right, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            carRb.AddForce((-Vector3.right + Vector3.forward) * 50, ForceMode.Force);
            return;
        }

        hits = Physics.RaycastAll(transform.position, -transform.forward + -transform.right, raycastCollisionDist);
        if (checkIfHitTag(hits, "Grid"))
        {
            carRb.AddForce((Vector3.right + Vector3.forward) * 50, ForceMode.Force);
            return;
        }
        */

    }

    private bool checkIfHitTag(RaycastHit[] hits, String tag)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.tag == tag) return true;
        }

        return false;
    }
}
