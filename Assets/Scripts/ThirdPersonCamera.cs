using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -4f);
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (target != null)
            yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rot * offset;

        transform.position = desiredPos;
        Vector3 lookPoint = target.position + Vector3.up * 1.2f;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}

