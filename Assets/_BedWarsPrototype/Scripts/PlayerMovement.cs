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
    [Header("Camera Controls")]
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float cameraRotationSpeed = 120f;
    [SerializeField] private float cameraZoomSpeed = 5f;
    [SerializeField] private float minCameraDistance = 2f;
    [SerializeField] private float maxCameraDistance = 12f;
    [SerializeField] private float turnSpeed = 15f;
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
    private Camera playerCamera;
    private Vector3 cameraOffset;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        InitializeCameraReference();
    }

    private void Start()
    {
        InitializeCameraOffset();
    }

    private void OnEnable()
    {
        playerInput.actions.Enable();
        EnsureActionMap();
        CacheActions();
        InitializeCameraOffset();
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
        Vector3 moveDirection = CalculateMoveDirection(input);
        controller.Move(moveDirection * speed * Time.deltaTime);
        UpdateFacing(moveDirection);

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

        HandleCameraControls();
    }

    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (playerCamera != null)
        {
            forward = playerCamera.transform.forward;
            right = playerCamera.transform.right;
        }

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = right * input.x + forward * input.y;
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction;
    }

    private void UpdateFacing(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        float t = Mathf.Clamp01(turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    private void HandleCameraControls()
    {
        if (playerCamera == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (cameraOffset.sqrMagnitude < 0.0001f)
        {
            cameraOffset = new Vector3(0f, 0f, -Mathf.Max(minCameraDistance, 0.1f));
        }

        float rotationInput = 0f;
        if (keyboard.jKey.isPressed)
        {
            rotationInput -= 1f;
        }

        if (keyboard.lKey.isPressed)
        {
            rotationInput += 1f;
        }

        if (!Mathf.Approximately(rotationInput, 0f))
        {
            Quaternion rotation = Quaternion.AngleAxis(rotationInput * cameraRotationSpeed * Time.deltaTime, Vector3.up);
            cameraOffset = rotation * cameraOffset;
        }

        float zoomInput = 0f;
        if (keyboard.iKey.isPressed)
        {
            zoomInput -= 1f;
        }

        if (keyboard.kKey.isPressed)
        {
            zoomInput += 1f;
        }

        if (!Mathf.Approximately(zoomInput, 0f))
        {
            float distance = Mathf.Clamp(cameraOffset.magnitude + zoomInput * cameraZoomSpeed * Time.deltaTime, minCameraDistance, maxCameraDistance);
            if (distance > Mathf.Epsilon)
            {
                cameraOffset = cameraOffset.normalized * distance;
            }
        }

        Vector3 focusPoint = transform.position + cameraFocusOffset;
        playerCamera.transform.position = focusPoint + cameraOffset;
        playerCamera.transform.LookAt(focusPoint);
    }


    private void InitializeCameraReference()
    {
        if (playerCamera != null)
        {
            return;
        }

        playerCamera = GetComponentInChildren<Camera>();
    }

    private void InitializeCameraOffset()
    {
        InitializeCameraReference();
        if (playerCamera == null)
        {
            return;
        }

        minCameraDistance = Mathf.Max(0.1f, minCameraDistance);
        maxCameraDistance = Mathf.Max(minCameraDistance, maxCameraDistance);

        Vector3 focusPoint = transform.position + cameraFocusOffset;
        cameraOffset = playerCamera.transform.position - focusPoint;
        float distance = cameraOffset.magnitude;
        if (distance < Mathf.Epsilon)
        {
            cameraOffset = new Vector3(0f, 0f, -minCameraDistance);
        }
        else
        {
            cameraOffset = cameraOffset.normalized * Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
        }
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
