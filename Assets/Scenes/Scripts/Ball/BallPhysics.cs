using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BallPhysics : MonoBehaviour
{
    [Header("State")]
    // 공의 속도는 Rigidbody.linearVelocity가 아니라 이 자체 변수로 관리합니다.
    [SerializeField] private Vector3 velocity;
    [SerializeField] private bool isFlying;
    [SerializeField] private bool isGrounded;
    [SerializeField] private SurfaceType currentSurface = SurfaceType.None;

    [Header("Forces")]
    // 중력도 Unity 기본 중력이 아니라 FixedUpdate에서 직접 계산합니다.
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float stopThreshold = 0.1f;
    [SerializeField] private float fairwayFriction = 0.85f;
    [SerializeField] private float roughFriction = 1.4f;
    [SerializeField] private float bunkerFriction = 2.2f;
    [SerializeField] private float greenFriction = 0.45f;

    [Header("Surface Check")]
    // 표면별 마찰 판정은 충돌 이벤트 대신 공 아래 Raycast로 확인합니다.
    [SerializeField] private float surfaceCheckDistance = 0.7f;

    [Header("Collision")]//BallPhysics.OnCollisionEnter()에서 충돌한 오브젝트의 Layer를 읽고, 그 표면에 맞는 반발계수 e를 적용한다
    [SerializeField] private float greenRestitution = 0.40f;
    [SerializeField] private float fairwayRestitution = 0.50f;
    [SerializeField] private float bunkerRestitution = 0.10f;
    [SerializeField] private float obstacleRestitution = 0.60f;
    [SerializeField] private float f = 0.1f;

    [Header("Shot Test")]
    // 임시 테스트 입력용 값입니다. 실제 UI가 붙으면 Launch()에 UI 값을 넘기면 됩니다.
    [SerializeField] private float testPower = 12f;
    [SerializeField] private float testPitch = 35f;

    private Rigidbody rb;
    private SphereCollider sphere;
    private Vector3 startPosition;

    // 공 중심에서 표면까지의 월드 반지름. 지면 안착 높이를 계산할 때 사용합니다.
    private float BallRadius => sphere != null ? sphere.radius * transform.lossyScale.x : 0.1f;

    public Vector3 Velocity => velocity;
    public bool IsFlying => isFlying;
    public bool IsGrounded => isGrounded;
    public SurfaceType CurrentSurface => currentSurface;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();

        // Rigidbody는 충돌 감지용으로만 사용하고, 실제 이동은 이 스크립트가 계산합니다.
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // 이동/마찰 계산 전에 현재 공 아래 표면을 먼저 갱신합니다.
        UpdateCurrentSurfaceByRaycast();

        // 지면에 얹혀 완전히 멈춰 있을 때만 계산을 건너뜁니다(바람에 의한 미끄러짐 방지).
        // 공중(접지 아님)이면 아래에서 중력으로 떨어지게 둡니다.
        if (!isFlying && isGrounded && velocity.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;
        Vector3 position = transform.position;

        Vector3 frictionAccel = isGrounded ? GetFrictionAccel() : Vector3.zero;
        // 단계 2(바람 외력): 추후 OOP를 지키려면 WindManager 호출을 ForceProvider 같은 외력 컴포넌트로 분리할 수 있습니다.
        Vector3 a_wind = WindManager.Instance != null ? WindManager.Instance.GetWindAccel() : Vector3.zero;
        Vector3 a_gravity = new Vector3(0f, -gravity, 0f);
        Vector3 a_total = a_gravity + a_wind + frictionAccel;

        // 핵심 요구사항: Semi-implicit Euler 적분입니다.
        PhysicsCore.Step(ref position, ref velocity, a_total, dt);

        // 시작 높이 고정(평평한 투명 바닥) 대신, 공 바로 아래 '실제 지면' 높이를 찾아 그 위에 안착시킵니다.
        // 빠르게 떨어질 때 지면을 뚫지 않도록 공보다 위에서 쏘고, 자기 레이어는 마스크로 제외합니다.
        // 이렇게 하면 공이 굴곡 지형의 언덕·계곡 표면을 그대로 따라갑니다.
        int groundMask = ~(1 << gameObject.layer);
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit,
                5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            SurfaceType groundSurface = LayerToSurface(groundHit.collider.gameObject.layer);
            if (groundSurface != SurfaceType.None && groundSurface != SurfaceType.OutOfBounds)
            {
                float surfaceY = groundHit.point.y + BallRadius;   // 공 중심이 놓일 높이
                if (position.y < surfaceY)                          // 지면에 닿거나 파고들면 안착
                {
                    position.y = surfaceY;
                    velocity.y = 0f;
                    isGrounded = true;
                    // isFlying은 여기서 끄지 않습니다. 완전히 멈출 때 아래 정지 판정이 끕니다.
                }
            }
        }

        transform.position = position;
        rb.MovePosition(position);
        rb.linearVelocity = Vector3.zero;
        // 이동 후 위치 기준으로 다시 표면을 확인해 다음 FixedUpdate의 마찰에 반영합니다.
        UpdateCurrentSurfaceByRaycast();

        // 매우 작은 속도는 0으로 처리해서 끝없이 미끄러지는 현상을 막습니다.
        if (isGrounded && velocity.magnitude < stopThreshold)
        {
            velocity = Vector3.zero;
            isFlying = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void UpdateCurrentSurfaceByRaycast()
    {
        // 충돌 반발은 OnCollisionEnter에서 처리하고, 현재 표면 상태는 Raycast로 안정적으로 갱신합니다.
        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            surfaceCheckDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            SurfaceType detectedSurface = LayerToSurface(hit.collider.gameObject.layer);

            if (detectedSurface != SurfaceType.None && detectedSurface != SurfaceType.OutOfBounds)
            {
                currentSurface = detectedSurface;

                if (!isFlying)
                {
                    // 실제로 지면에 닿을 만큼 가까울 때만 접지로 봅니다.
                    // 살짝 떠 있으면 접지가 아니므로 위에서 중력으로 떨어져 표면에 안착합니다.
                    isGrounded = hit.distance <= BallRadius + 0.05f;
                }

                return;
            }
        }

        if (!isFlying)
        {
            currentSurface = SurfaceType.None;
            isGrounded = false;
        }
    }

    private Vector3 GetFrictionAccel()
    {
        float mu = GetSurfaceFriction();
        return -mu * gravity * velocity.normalized;
    }

    private float GetSurfaceFriction()
    {//GetSurfaceFriction()은 굴러갈 때 감속에 쓰는 값입니다.
        // 현재 표면 레이어에 따라 마찰을 다르게 적용합니다.
        // 단계 3(표면별 마찰): 추후 OOP를 지키려면 Surface/SurfaceProfile이 마찰 값을 소유하게 분리할 수 있습니다.
        switch (currentSurface)
        {
            case SurfaceType.Rough:
                return roughFriction;
            case SurfaceType.Bunker:
                return bunkerFriction;
            case SurfaceType.Green:
                return greenFriction;
            case SurfaceType.Fairway:
                return fairwayFriction;
            default:
                return fairwayFriction;
        }
    }

    private float GetRestitution(SurfaceType surface)
    {//GetRestitution(SurfaceType surface)은 충돌 순간에 얼마나 튈지 정하는 값
        // 단계 4(충돌 반발): 추후 OOP를 지키려면 Surface/SurfaceProfile이 반발계수 값을 소유하게 분리할 수 있습니다.
        switch (surface)
        {
            case SurfaceType.Green:
                return greenRestitution;
            case SurfaceType.Bunker:
                return bunkerRestitution;
            case SurfaceType.Obstacle:
                return obstacleRestitution;
            case SurfaceType.Fairway:
            case SurfaceType.Rough:
            default:
                return fairwayRestitution;
        }
    }

    public void Launch(float power, float pitch, Vector3 forward)
    {
        // 발사 순간의 초기 속도를 계산하고 공중 상태로 전환합니다.
        velocity = PhysicsCore.CalculateLaunchVelocity(power, pitch, forward);
        isFlying = true;
        isGrounded = false;
    }

    public void Stop()
    {
        // 자체 속도와 Rigidbody 속도를 모두 초기화합니다.
        velocity = Vector3.zero;
        isFlying = false;
        isGrounded = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void ResetBall(Vector3 position)
    {
        // 벌타/재시작 때 원하는 위치로 공을 되돌립니다.
        Stop();
        transform.position = position;
        startPosition = position;
        currentSurface = SurfaceType.None;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 n = collision.GetContact(0).normal;

        SurfaceType hitSurface = LayerToSurface(collision.gameObject.layer);
        // 충돌 순간의 표면만 저장하고, 이후 마찰 계산은 이 값을 계속 사용합니다.
        currentSurface = hitSurface;

        // 표면을 향해 '실제로 빠르게 들어올 때'만 반발합니다.
        // 지면에 얹혀 구르거나 정지 중인 접촉은 매 프레임 충돌이 재발생하는데,
        // 이때도 반발하면 작은 속도가 계속 재충전되어 공이 끝없이 미끄러집니다.
        // (그런 접촉은 raycast 안착과 마찰이 대신 처리합니다.)
        float approachSpeed = -Vector3.Dot(velocity, n);
        if (approachSpeed < 0.5f)
        {
            return;
        }

        float e = GetRestitution(hitSurface);

        Vector3 v_n = Vector3.Dot(velocity, n) * n;
        Vector3 v_t = velocity - v_n;

        velocity = -e * v_n + (1f - f) * v_t;

        transform.position += n * 0.01f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hole"))
        {
            Stop();
            Debug.Log("Hole in");
            return;
        }

        if (LayerToSurface(other.gameObject.layer) == SurfaceType.OutOfBounds)
        {
            Debug.Log("Out of bounds");
            ResetBall(startPosition);
        }
    }

    private SurfaceType LayerToSurface(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);

        switch (layerName)
        {
            case "Fairway":
            case "Fairaway":
                return SurfaceType.Fairway;
            case "Rough":
                return SurfaceType.Rough;
            case "Bunker":
                return SurfaceType.Bunker;
            case "Green":
                return SurfaceType.Green;
            case "OutOfBounds":
                return SurfaceType.OutOfBounds;
            case "Obstacle":
                return SurfaceType.Obstacle;
            default:
                return SurfaceType.None;
        }
    }

    private void Update()
    {
        // 임시 테스트: R로 시작 위치 리셋. 발사는 InputManager가 담당합니다.
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall(startPosition);
        }
    }
}
