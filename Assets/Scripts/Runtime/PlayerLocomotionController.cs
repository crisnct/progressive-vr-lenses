using UnityEngine;
using UnityEngine.XR;

namespace ProgressiveVrLenses.Runtime
{
    /// <summary>
    /// Provides locomotion both in-flat (mouse + keyboard) and in XR (controller joysticks).
    /// Attach to the player root that owns the CharacterController and camera.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotionController : MonoBehaviour
    {
        [Header("Common")]
        [Tooltip("Camera (or XR head) used to determine forward direction.")]
        public Transform cameraTransform;
        [Tooltip("Gravity applied to the character in m/s^2.")]
        public float gravity = -9.81f;
        [Tooltip("Optional jump impulse; set to 0 to disable jumping.")]
        public float jumpVelocity = 0f;

        [Header("Desktop Controls")]
        public float moveSpeedDesktop = 4f;
        public float mouseSensitivity = 2.5f;
        public bool lockCursor = true;

        [Header("XR Controls")]
        public float moveSpeedVr = 2.5f;
        public float turnSpeedVr = 90f;
        [Tooltip("Minimum joystick magnitude required before movement is applied.")]
        public float vrMoveDeadzone = 0.15f;
        [Tooltip("Minimum joystick magnitude required before rotation is applied.")]
        public float vrTurnDeadzone = 0.2f;

        private CharacterController _characterController;
        private bool _cursorLocked;
        private float _pitch;
        private float _verticalVelocity;

        private InputDevice _leftHandDevice;
        private InputDevice _rightHandDevice;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.center = Vector3.up * (_characterController.height * 0.5f);

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _cursorLocked = true;
            }
        }

        private void OnDisable()
        {
            if (_cursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _cursorLocked = false;
            }
        }

        private void Update()
        {
            var xrActive = XRSettings.enabled && XRSettings.isDeviceActive;

            if (xrActive)
                HandleVrLocomotion();
            else
                HandleDesktopLocomotion();

            ApplyGravity();
        }

        private void HandleDesktopLocomotion()
        {
            var yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            var pitchDelta = -Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, yaw, Space.World);
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -85f, 85f);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            var forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            var right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            var move = (forward * input.y + right * input.x) * moveSpeedDesktop;
            _characterController.Move(move * Time.deltaTime);

            if (jumpVelocity > 0f && Input.GetButtonDown("Jump") && _characterController.isGrounded)
                _verticalVelocity = jumpVelocity;
        }

        private void HandleVrLocomotion()
        {
            EnsureDevices();

            Vector2 moveAxis = Vector2.zero;
            if (_leftHandDevice.isValid)
                _leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out moveAxis);

            Vector2 turnAxis = Vector2.zero;
            if (_rightHandDevice.isValid)
                _rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out turnAxis);

            if (moveAxis.magnitude < vrMoveDeadzone)
                moveAxis = Vector2.zero;

            if (turnAxis.magnitude >= vrTurnDeadzone)
            {
                var turn = turnAxis.x * turnSpeedVr * Time.deltaTime;
                transform.Rotate(Vector3.up, turn, Space.World);
            }

            if (cameraTransform == null)
                return;

            var headForward = cameraTransform.forward;
            var headRight = cameraTransform.right;
            headForward.y = 0f;
            headRight.y = 0f;
            headForward.Normalize();
            headRight.Normalize();

            var direction = headForward * moveAxis.y + headRight * moveAxis.x;
            _characterController.Move(direction * moveSpeedVr * Time.deltaTime);
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded && _verticalVelocity <= 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            var gravityMove = Vector3.up * _verticalVelocity * Time.deltaTime;
            _characterController.Move(gravityMove);
        }

        private void EnsureDevices()
        {
            if (!_leftHandDevice.isValid)
                _leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            if (!_rightHandDevice.isValid)
                _rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
    }
}
