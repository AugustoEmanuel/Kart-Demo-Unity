using Unity.Netcode;
using UnityEngine;

public class CameraCar : NetworkBehaviour
{

    public float moveSmoothness;
    public float rotSmoothness;

    public Vector3 moveOffset = new Vector3(0, 5, -5);
    public Vector3 rotOffset = new Vector3(0, 0, 0);

    public Transform carTarget;

    private PlayerInputActions inputActions;

    void HandleMovement()
    {
        bool isInvertCamera = inputActions.Race.InvertCamera.IsPressed();
        Vector3 targetPos = new Vector3(moveOffset.x, moveOffset.y, moveOffset.z * (isInvertCamera ? -1 : 1));
        targetPos = carTarget.TransformPoint(targetPos);

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * (false ? 0 : 1) * Time.deltaTime);
    }

    void HandleRotation()
    {
        var direction = carTarget.position - transform.position;
        var rotation = Quaternion.LookRotation(direction + rotOffset, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotSmoothness * Time.deltaTime);
    }

    void FollowTarget()
    {
        HandleMovement();
        HandleRotation();
    }

    private void Start()
    {
        if (!transform.parent.GetComponent<NetworkObject>().IsOwner)
        {
            GetComponent<Camera>().enabled = false;
            return;
        }

        inputActions = new();
        inputActions.Race.Enable();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (carTarget == null) return;
        FollowTarget();
    }
}
