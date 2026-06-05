// CameraController.cs
// Main Camera에 부착한다.
// 역할: 조준(Aiming) 상태에서는 공 뒤쪽 위에서 공을 바라보고,
//       공이 날아가는(Tracking) 상태에서는 공을 부드럽게 따라가며 바라본다.
// 공의 속도는 Rigidbody가 아니라 BallPhysics.Velocity를 참조한다.
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // 카메라 동작 상태입니다.
    private enum State
    {
        Aiming,   // 발사 전: 공 뒤쪽 위에서 조준 방향을 바라봅니다.
        Tracking  // 발사 후: 날아가는 공을 따라갑니다.
    }

    [Header("References")]
    [SerializeField] private BallPhysics ball;

    [Header("Camera Offset")]
    // 공으로부터 뒤로 떨어지는 거리입니다.
    [SerializeField] private float distance = 6f;
    // 공보다 위로 올라간 높이입니다.
    [SerializeField] private float height = 3f;
    // 위치/회전 보간 속도입니다. 클수록 빠르게 따라붙습니다.
    [SerializeField] private float smoothness = 5f;

    private State state = State.Aiming;

    private void Awake()
    {
        // Inspector 연결이 없으면 씬에서 자동으로 찾습니다.
        if (ball == null)
        {
            ball = FindFirstObjectByType<BallPhysics>();
        }
    }

    private void LateUpdate()
    {
        if (ball == null)
        {
            return;
        }

        // 매 프레임 IsFlying을 폴링해 상태를 전환합니다.
        state = ball.IsFlying ? State.Tracking : State.Aiming;

        if (state == State.Aiming)
        {
            UpdateAiming();
        }
        else
        {
            UpdateTracking();
        }
    }

    private void UpdateAiming()
    {
        // InputManager가 공 Transform을 회전시키므로, 공의 forward 기준 뒤쪽 위 오프셋만 유지합니다.
        Transform ballTransform = ball.transform;
        Vector3 desiredPosition =
            ballTransform.position - ballTransform.forward * distance + Vector3.up * height;

        // 조준 중에는 즉시 따라붙어 흔들림 없이 안정적으로 보이게 합니다.
        transform.position = desiredPosition;
        transform.rotation = Quaternion.LookRotation(ballTransform.position - transform.position, Vector3.up);
    }

    private void UpdateTracking()
    {
        Transform ballTransform = ball.transform;

        // 진행 방향(공 속도) 기준 뒤쪽 위에서 따라갑니다. 속도가 거의 0이면 forward로 대체합니다.
        Vector3 velocity = ball.Velocity;
        Vector3 followDir = velocity.sqrMagnitude > 0.001f
            ? velocity.normalized
            : ballTransform.forward;

        Vector3 desiredPosition =
            ballTransform.position - followDir * distance + Vector3.up * height;

        // 위치는 Lerp, 회전은 Slerp로 부드럽게 보간합니다.
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothness * Time.deltaTime);

        Quaternion desiredRotation = Quaternion.LookRotation(ballTransform.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothness * Time.deltaTime);
    }
}
