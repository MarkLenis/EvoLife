using UnityEngine;

namespace EvoLife.UI
{
    public enum DesktopCameraMode : byte
    {
        Free = 0,
        Orbit = 1
    }

    /// <summary>
    /// Desktop observer camera. Not XR. Follows a selected transform in orbit mode
    /// without reading creature biology.
    /// </summary>
    public sealed class DesktopCameraController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 12f;
        [SerializeField] float fastMultiplier = 3f;
        [SerializeField] float lookSensitivity = 2.4f;
        [SerializeField] float zoomSensitivity = 8f;
        [SerializeField] float minFov = 25f;
        [SerializeField] float maxFov = 80f;
        [SerializeField] float minOrbitDistance = 2.5f;
        [SerializeField] float maxOrbitDistance = 80f;
        [SerializeField] float orbitDistance = 14f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;
        [SerializeField] KeyCode focusKey = KeyCode.F;
        [SerializeField] KeyCode freeKey = KeyCode.C;
        [SerializeField] KeyCode speedUpKey = KeyCode.Equals;
        [SerializeField] KeyCode speedDownKey = KeyCode.Minus;

        Camera cam;
        DesktopCameraMode mode = DesktopCameraMode.Free;
        Transform focusTarget;
        float yaw;
        float pitch;
        float fov;

        public DesktopCameraMode Mode => mode;

        public Transform FocusTarget => focusTarget;

        public float MoveSpeed => moveSpeed;

        public void SetFocusTarget(Transform target)
        {
            focusTarget = target;
            if (mode == DesktopCameraMode.Orbit && focusTarget == null)
            {
                ReturnToFree();
            }
        }

        public void FocusSelected(Transform target)
        {
            if (target == null)
            {
                return;
            }

            focusTarget = target;
            mode = DesktopCameraMode.Orbit;
            var offset = transform.position - target.position;
            orbitDistance = Mathf.Clamp(offset.magnitude, minOrbitDistance, maxOrbitDistance);
            if (orbitDistance < 0.01f)
            {
                orbitDistance = 12f;
            }

            var euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
        }

        public void ReturnToFree()
        {
            mode = DesktopCameraMode.Free;
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            var euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
            fov = cam != null ? cam.fieldOfView : 60f;
        }

        void Update()
        {
            if (focusTarget == null && mode == DesktopCameraMode.Orbit)
            {
                ReturnToFree();
            }

            if (Input.GetKeyDown(focusKey) && focusTarget != null)
            {
                FocusSelected(focusTarget);
            }

            if (Input.GetKeyDown(freeKey))
            {
                ReturnToFree();
            }

            if (Input.GetKeyDown(speedUpKey) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                moveSpeed = Mathf.Min(80f, moveSpeed * 1.25f);
            }

            if (Input.GetKeyDown(speedDownKey) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                moveSpeed = Mathf.Max(1f, moveSpeed / 1.25f);
            }

            if (mode == DesktopCameraMode.Orbit)
            {
                UpdateOrbit();
                return;
            }

            UpdateFree();
        }

        void UpdateFree()
        {
            ApplyLook();
            var speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= fastMultiplier;
            }

            var planar = transform.forward;
            planar.y = 0f;
            if (planar.sqrMagnitude < 0.0001f)
            {
                planar = Vector3.forward;
            }

            planar.Normalize();
            var right = Vector3.Cross(Vector3.up, planar).normalized;
            var motion = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                motion += planar;
            }

            if (Input.GetKey(KeyCode.S))
            {
                motion -= planar;
            }

            if (Input.GetKey(KeyCode.D))
            {
                motion += right;
            }

            if (Input.GetKey(KeyCode.A))
            {
                motion -= right;
            }

            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space))
            {
                motion += Vector3.up;
            }

            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl))
            {
                motion -= Vector3.up;
            }

            if (motion.sqrMagnitude > 0f)
            {
                transform.position += motion.normalized * (speed * Time.unscaledDeltaTime);
            }

            ApplyZoomFov();
        }

        void UpdateOrbit()
        {
            if (focusTarget == null)
            {
                return;
            }

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, minPitch, maxPitch);
            }

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                orbitDistance = Mathf.Clamp(
                    orbitDistance - scroll * zoomSensitivity,
                    minOrbitDistance,
                    maxOrbitDistance);
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = focusTarget.position - rotation * Vector3.forward * orbitDistance;
        }

        void ApplyLook()
        {
            if (!Input.GetMouseButton(1))
            {
                return;
            }

            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void ApplyZoomFov()
        {
            if (cam == null)
            {
                return;
            }

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) <= 0.0001f)
            {
                return;
            }

            fov = Mathf.Clamp(fov - scroll * zoomSensitivity * 4f, minFov, maxFov);
            cam.fieldOfView = fov;
        }

        static float NormalizePitch(float eulerX)
        {
            if (eulerX > 180f)
            {
                eulerX -= 360f;
            }

            return eulerX;
        }
    }
}
