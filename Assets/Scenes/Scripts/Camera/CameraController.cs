// CameraController.cs
// Main Camera에 부착한다.
// 역할: 5단계 상태 머신으로 게임 흐름에 맞춘 카메라 연출을 담당한다.
//   Aiming   : 공 뒤쪽 위에서 조준 방향 유지 (발사 전)
//   Tracking : 탑뷰, 공 위에서 내려다보며 비행 추적 (발사 직후)
//   Approach : 탑뷰 유지하되 공~홀 중간 바라보기, FOV 좁힘 (그린 착지 후)
//   ZoomIn   : 홀컵 위 고정, 강한 줌인 (홀 근접 시)
//   HoleIn   : ZoomIn 위치 고정, 홀인 연출 감상
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum CameraState { Aiming, Tracking, Approach, ZoomIn, HoleIn }

    [Header("References")]
    [SerializeField] private BallPhysics ball;
    [SerializeField] private Transform holeTransform;

    [Header("Aiming")]
    [SerializeField] private float aimDistance = 6f;
    [SerializeField] private float aimHeight = 3f;
    [SerializeField] private float aimFOV = 60f;

    [Header("Tracking (탑뷰)")]
    // 탑뷰 고도입니다. 높을수록 더 넓게 보입니다.
    [SerializeField] private float trackingHeight = 30f;
    // Euler X 각도: 90=수직, 80=살짝 비스듬히(시인성 향상).
    [SerializeField] private float trackingTilt = 80f;

    [Header("Approach")]
    [SerializeField] private float approachHeight = 20f;
    [SerializeField] private float approachFOV = 45f;

    [Header("ZoomIn")]
    // 이 거리 이내로 들어오면 ZoomIn으로 전환합니다.
    [SerializeField] private float zoomInDistance = 15f;
    [SerializeField] private float zoomHeight = 5f;
    // 홀컵에서 공 방향으로 약간 뒤로 물러나는 거리입니다.
    [SerializeField] private float zoomBackOffset = 2f;
    [SerializeField] private float zoomFOV = 25f;

    [Header("HoleIn")]
    // HoleIn 카메라 유지 시간입니다 (연출 감상 후 다음 단계로 넘어갈 때 참고용).
    [SerializeField] private float holeInDuration = 2f;

    [Header("Common")]
    [SerializeField] private float smoothness = 5f;

    private Camera cam;
    private CameraState currentState = CameraState.Aiming;
    private bool wasFlying;

    public CameraState CurrentState => currentState;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (ball == null) ball = FindAnyObjectByType<BallPhysics>();
    }

    private void LateUpdate()
    {
        if (ball == null) return;

        HandleStateTransitions();
        UpdateCamera();
    }

    private void HandleStateTransitions()
    {
        bool isFlying = ball.IsFlying;

        // Aiming → Tracking: 발사 감지
        if (!wasFlying && isFlying && currentState == CameraState.Aiming)
        {
            currentState = CameraState.Tracking;
        }

        // 공 정지 시 Aiming 복귀 (HoleIn 제외)
        if (wasFlying && !isFlying && currentState != CameraState.HoleIn)
        {
            currentState = CameraState.Aiming;
            cam.fieldOfView = aimFOV;
        }

        wasFlying = isFlying;

        if (currentState == CameraState.HoleIn) return;

        // 홀 거리 기반: Tracking/Approach → ZoomIn
        if (holeTransform != null &&
            (currentState == CameraState.Tracking || currentState == CameraState.Approach))
        {
            float dist = Vector3.Distance(ball.transform.position, holeTransform.position);
            if (dist < zoomInDistance)
            {
                currentState = CameraState.ZoomIn;
                return;
            }
        }

        // Tracking → Approach: 그린 착지
        if (currentState == CameraState.Tracking && ball.CurrentSurface == SurfaceType.Green)
        {
            currentState = CameraState.Approach;
        }
    }

    private void UpdateCamera()
    {
        switch (currentState)
        {
            case CameraState.Aiming:   DoAiming();   break;
            case CameraState.Tracking: DoTracking(); break;
            case CameraState.Approach: DoApproach(); break;
            case CameraState.ZoomIn:   DoZoomIn();   break;
            case CameraState.HoleIn:   DoHoleIn();   break;
        }
    }

    private void DoAiming()
    {
        Transform ballT = ball.transform;
        Vector3 target = ballT.position - ballT.forward * aimDistance + Vector3.up * aimHeight;

        // 조준 중에는 즉시 붙어서 흔들림 없이 안정적으로 보입니다.
        transform.position = target;
        transform.rotation = Quaternion.LookRotation(ballT.position - transform.position, Vector3.up);
        cam.fieldOfView = aimFOV;
    }

    private void DoTracking()
    {
        Vector3 target = ball.transform.position + Vector3.up * trackingHeight;
        transform.position = Vector3.Lerp(transform.position, target, smoothness * Time.deltaTime);

        // 공 이동 방향으로 Y축 정렬해서 진행 방향 파악이 쉽게 합니다.
        float yaw = 0f;
        if (ball.Velocity.sqrMagnitude > 0.01f)
        {
            Vector3 flat = Vector3.ProjectOnPlane(ball.Velocity, Vector3.up);
            if (flat.sqrMagnitude > 0.001f)
                yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }
        Quaternion targetRot = Quaternion.Euler(trackingTilt, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, smoothness * Time.deltaTime);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, aimFOV, smoothness * Time.deltaTime);
    }

    private void DoApproach()
    {
        if (holeTransform == null) { DoTracking(); return; }

        // 공과 홀의 중간 지점 위에 위치합니다.
        Vector3 midPoint = (ball.transform.position + holeTransform.position) * 0.5f;
        Vector3 target = midPoint + Vector3.up * approachHeight;
        transform.position = Vector3.Lerp(transform.position, target, smoothness * Time.deltaTime);

        // 공~홀 중간을 내려다봅니다.
        Vector3 dir = midPoint - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, smoothness * Time.deltaTime);
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, approachFOV, smoothness * Time.deltaTime);
    }

    private void DoZoomIn()
    {
        if (holeTransform == null) return;

        // 공→홀 방향의 반대(뒤쪽)에서 홀컵을 내려다봅니다.
        Vector3 backDir = (ball.transform.position - holeTransform.position).normalized;
        if (backDir.sqrMagnitude < 0.001f) backDir = -transform.forward;
        Vector3 target = holeTransform.position
            + Vector3.up * zoomHeight
            + backDir * zoomBackOffset;

        transform.position = Vector3.Lerp(transform.position, target, smoothness * Time.deltaTime);

        Vector3 dir = holeTransform.position - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, smoothness * Time.deltaTime);
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoomFOV, smoothness * Time.deltaTime);
    }

    private void DoHoleIn()
    {
        // HoleIn은 ZoomIn 위치를 유지하며 연출을 감상합니다.
        DoZoomIn();
    }

    // GameManager가 홀인 감지 시 호출합니다.
    public void SetState(CameraState newState)
    {
        currentState = newState;
    }

    // OnBallInHole에서 직접 호출할 수 있는 편의 메서드입니다.
    public void SetHoleInCamera()
    {
        currentState = CameraState.HoleIn;
    }
}
