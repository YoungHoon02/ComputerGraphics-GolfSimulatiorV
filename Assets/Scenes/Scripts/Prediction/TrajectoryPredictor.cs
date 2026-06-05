// TrajectoryPredictor.cs
// 빈 오브젝트 + LineRenderer에 부착한다.
// 역할: 발사 전 InputManager의 현재 파워/각도와 공 forward를 읽어 궤적을 미리 시뮬레이션해 LineRenderer로 표시한다.
// 실제 물리와 동일한 결과를 보장하기 위해 PhysicsCore.CalculateLaunchVelocity()와 PhysicsCore.Step()을 그대로 재사용한다.
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private BallPhysics ball;
    [SerializeField] private InputManager input;

    [Header("Physics")]
    // BallPhysics와 동일한 중력 크기입니다(기본 9.81). 같은 값을 써야 예측이 실제와 일치합니다.
    [SerializeField] private float gravity = 9.81f;

    [Header("Simulation")]
    // 예측 스텝 수와 한 스텝의 시간 간격입니다.
    [SerializeField] private int simulationSteps = 100;
    [SerializeField] private float simulationDt = 0.05f;

    [Header("Ground Check")]
    // 예측 경로가 지면과 충돌하면 거기서 예측을 중단합니다.
    [SerializeField] private float groundCheckRadius = 0.1f;

    // 예측 경로 충돌 검사에서 공 자신의 콜라이더는 무시하기 위해 캐싱합니다.
    private Collider ballCollider;

    private void Awake()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        if (ball == null)
        {
            ball = FindFirstObjectByType<BallPhysics>();
        }

        if (input == null)
        {
            input = FindFirstObjectByType<InputManager>();
        }

        if (ball != null)
        {
            ballCollider = ball.GetComponent<Collider>();
        }
    }

    private void Update()
    {
        if (line == null || ball == null || input == null)
        {
            return;
        }

        // 공이 이미 날아가는 중이면 예측선을 숨깁니다. 조준 중에만 표시합니다.
        if (ball.IsFlying)
        {
            line.positionCount = 0;
            return;
        }

        DrawPrediction();
    }

    private void DrawPrediction()
    {
        Transform ballTransform = ball.transform;

        // InputManager.Shoot()과 동일한 파워 변환을 사용합니다.
        float power = Mathf.Lerp(2f, 18f, input.CurrentPower);
        float pitch = input.CurrentPitch;
        Vector3 forward = ballTransform.forward;

        // 실제 물리와 동일한 초기 속도 계산입니다.
        Vector3 position = ballTransform.position;
        Vector3 velocity = PhysicsCore.CalculateLaunchVelocity(power, pitch, forward);

        // 중력 + 바람 가속도를 실제 물리와 동일하게 구성합니다.
        Vector3 windAccel = WindManager.Instance != null ? WindManager.Instance.GetWindAccel() : Vector3.zero;
        Vector3 totalAccel = new Vector3(0f, -gravity, 0f) + windAccel;

        // 첫 점은 공의 현재 위치입니다.
        line.positionCount = 1;
        line.SetPosition(0, position);

        for (int i = 1; i <= simulationSteps; i++)
        {
            Vector3 previous = position;

            // 자체 적분 로직을 새로 만들지 않고 PhysicsCore.Step()을 그대로 사용합니다.
            PhysicsCore.Step(ref position, ref velocity, totalAccel, simulationDt);

            // 이전 점에서 새 점까지의 경로가 지면/장애물과 충돌하면 거기까지만 그리고 멈춥니다.
            Vector3 segment = position - previous;
            float segmentLength = segment.magnitude;
            if (segmentLength > Mathf.Epsilon &&
                Physics.SphereCast(
                    previous,
                    groundCheckRadius,
                    segment.normalized,
                    out RaycastHit hit,
                    segmentLength,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) &&
                hit.collider != ballCollider)
            {
                line.positionCount = i + 1;
                line.SetPosition(i, hit.point);
                return;
            }

            line.positionCount = i + 1;
            line.SetPosition(i, position);
        }
    }
}
