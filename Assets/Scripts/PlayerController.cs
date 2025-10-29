using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Gravedad")]
    [SerializeField] private float gravity = 20f;

    [Header("Chequeo de suelo")]
    [SerializeField] private Transform groundCheck;    
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterController controller;
    private Vector3 velocity; 
    private bool isGrounded = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Transform cam = Camera.main ? Camera.main.transform : null;
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 move;
        if (cam)
        {
            Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            move = forward * input.z + right * input.x;
        }
        else
        {
            move = transform.forward * input.z + transform.right * input.x;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        isGrounded = CheckGrounded();
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; 

        velocity.y -= gravity * Time.deltaTime;

        Vector3 total = move * moveSpeed + new Vector3(0f, velocity.y, 0f);
        controller.Move(total * Time.deltaTime);
    }

    private bool CheckGrounded()
    {
        if (groundCheck != null)
        {
            return Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            Vector3 origin = transform.position + Vector3.up * 0.05f;
            float rayLen = (controller ? controller.skinWidth : 0.05f) + 0.25f;
            return Physics.SphereCast(
                origin,
                groundCheckRadius,
                Vector3.down,
                out _,
                rayLen,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (groundCheck)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        else
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, groundCheckRadius);
    }
}


