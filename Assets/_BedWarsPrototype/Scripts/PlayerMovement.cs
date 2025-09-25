using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckDistance = 0.05f;
    [SerializeField] private LayerMask groundLayers = -1;
    [SerializeField] private float groundSnapVelocity = -2f;
    [SerializeField] private float fallMultiplier = 1.5f;
    [SerializeField] private bool showGroundCheckGizmo = true;

    private Vector3 groundCheckOrigin;
    private float groundCheckRayLength;
    private float groundCheckRadius;
    private bool groundCheckHit;
    private Vector3 groundHitPoint;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.actions.Enable();
        EnsureActionMap();
        CacheActions();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        playerInput.actions.Disable();
    }

    private void EnsureActionMap()
    {
        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != "Player")
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    private void CacheActions()
    {
        moveAction = playerInput.currentActionMap.FindAction("Move");
        jumpAction = playerInput.currentActionMap.FindAction("Jump");
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void Update()
    {
        if (moveAction == null || jumpAction == null)
        {
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * speed * Time.deltaTime);

        groundCheckOrigin = transform.position + controller.center;
        groundCheckRadius = controller.radius;
        groundCheckRayLength = controller.height * 0.5f + groundCheckDistance;

        bool grounded = controller.isGrounded;
        groundCheckHit = grounded;
        groundHitPoint = groundCheckOrigin + Vector3.down * groundCheckRayLength;

        if (!grounded)
        {
            if (Physics.SphereCast(groundCheckOrigin, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckRayLength, groundLayers, QueryTriggerInteraction.Ignore))
            {
                grounded = true;
                groundCheckHit = true;
                groundHitPoint = hit.point;
            }
            else
            {
                groundCheckHit = false;
            }

            if (velocity.y < 0f && fallMultiplier > 1f)
            {
                velocity.y += gravity * (fallMultiplier - 1f) * Time.deltaTime;
            }
        }

        if (grounded && velocity.y <= 0f)
        {
            velocity.y = Mathf.Min(velocity.y, groundSnapVelocity);
        }

        if (grounded && jumpAction.WasPressedThisFrame())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            grounded = false;
            groundCheckHit = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGroundCheckGizmo)
        {
            return;
        }

        CharacterController cc = controller != null ? controller : GetComponent<CharacterController>();
        if (cc == null)
        {
            return;
        }

        Vector3 origin = Application.isPlaying ? groundCheckOrigin : transform.position + cc.center;
        float radius = Application.isPlaying ? groundCheckRadius : cc.radius;
        float rayLength = Application.isPlaying ? groundCheckRayLength : cc.height * 0.5f + groundCheckDistance;

        Vector3 endPoint = origin + Vector3.down * rayLength;
        Gizmos.color = groundCheckHit ? Color.green : Color.red;
        Gizmos.DrawLine(origin, endPoint);
        Gizmos.DrawWireSphere(origin, radius);
        Gizmos.DrawWireSphere(endPoint, radius);

        if (Application.isPlaying && groundCheckHit)
        {
            Gizmos.DrawSphere(groundHitPoint, radius * 0.25f);
        }
    }
#endif
}
