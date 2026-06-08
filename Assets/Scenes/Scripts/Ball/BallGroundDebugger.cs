// BallGroundDebugger.cs
// 진단 전용(읽기 전용) 스크립트입니다. BallPhysics는 일절 수정하지 않습니다.
// 역할: 공이 왜 지면에 안 닿고 떠 있는지 원인을 눈으로 확인하게 해줍니다.
//   - 공 아래로 Raycast를 그려 지면까지의 실제 간격을 표시(Scene/Game 뷰)
//   - 간격 vs surfaceCheckDistance(0.7) vs 접지 임계(반지름+0.05) 비교
//   - 지면 오브젝트 이름/레이어/표면종류 + BallPhysics 상태(isGrounded 등) 표시
// 사용법: Ball 오브젝트에 이 컴포넌트를 추가하고 Play. (확인 끝나면 제거 권장)
using UnityEngine;

public class BallGroundDebugger : MonoBehaviour
{
    [Header("Probe")]
    // 지면을 멀리 있어도 찾기 위해 길게 쏩니다(코드의 0.7과 별개로 진단용).
    [SerializeField] private float probeDistance = 50f;
    // BallPhysics의 surfaceCheckDistance와 동일 값(비교 표시용). 코드 기본값 0.7.
    [SerializeField] private float surfaceCheckDistance = 0.7f;
    // 화면 좌상단 오버레이 표시 여부.
    [SerializeField] private bool showOnScreen = true;
    // 콘솔 로그 주기(초). 0이면 로그 끔.
    [SerializeField] private float logInterval = 0.5f;

    private BallPhysics ball;
    private SphereCollider sphere;
    private float logTimer;

    // 마지막 측정 결과(표시용)
    private bool hitGround;
    private float gap;
    private string groundName = "-";
    private string groundLayer = "-";
    private float ballRadius;

    private void Awake()
    {
        ball = GetComponent<BallPhysics>();
        sphere = GetComponent<SphereCollider>();
    }

    private void Update()
    {
        ballRadius = sphere != null ? sphere.radius * transform.lossyScale.x : 0.2f;

        Vector3 origin = transform.position;
        hitGround = Physics.Raycast(
            origin, Vector3.down, out RaycastHit hit,
            probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (hitGround)
        {
            gap = hit.distance;
            groundName = hit.collider.gameObject.name;
            groundLayer = LayerMask.LayerToName(hit.collider.gameObject.layer);

            // 0.7(접지 탐지 한계) 안쪽은 초록, 바깥쪽은 노랑으로 그려 한눈에 비교
            Color near = gap <= surfaceCheckDistance ? Color.green : Color.yellow;
            Debug.DrawLine(origin, hit.point, near);
            // 접지 탐지 한계선(0.7) 위치를 빨간 짧은 선으로 표시
            Vector3 limit = origin + Vector3.down * surfaceCheckDistance;
            Debug.DrawLine(limit + Vector3.left * 0.2f, limit + Vector3.right * 0.2f, Color.red);
        }
        else
        {
            gap = -1f;
            groundName = "(없음)";
            groundLayer = "-";
            Debug.DrawRay(origin, Vector3.down * probeDistance, Color.red);
        }

        if (logInterval > 0f)
        {
            logTimer += Time.deltaTime;
            if (logTimer >= logInterval)
            {
                logTimer = 0f;
                Debug.Log(BuildReport());
            }
        }
    }

    private string BuildReport()
    {
        string surface = ball != null ? ball.CurrentSurface.ToString() : "?";
        string grounded = ball != null ? ball.IsGrounded.ToString() : "?";
        string flying = ball != null ? ball.IsFlying.ToString() : "?";
        float nearTh = ballRadius + 0.05f;

        if (!hitGround)
            return $"[GroundDebug] 지면 못 찾음(아래 {probeDistance}m 내 콜라이더 없음) | " +
                   $"isGrounded={grounded}, currentSurface={surface}";

        string verdict;
        if (gap > surfaceCheckDistance)
            verdict = $"❌ 간격 {gap:F2} > 0.7 → Raycast가 지면을 못 잡음(너무 높이 떠 있음)";
        else if (gap > nearTh)
            verdict = $"⚠️ 간격 {gap:F2} ≤ 0.7 이지만 접지임계 {nearTh:F2} 초과 → 표면은 감지하나 접지 안 됨";
        else
            verdict = $"✅ 간격 {gap:F2} ≤ 접지임계 {nearTh:F2} → 접지 가능 거리";

        return $"[GroundDebug] {verdict} | 지면='{groundName}'(Layer:{groundLayer}) | " +
               $"ballRadius={ballRadius:F3}, ballY={transform.position.y:F2} | " +
               $"isGrounded={grounded}, isFlying={flying}, currentSurface={surface}";
    }

    private void OnGUI()
    {
        if (!showOnScreen) return;

        string surface = ball != null ? ball.CurrentSurface.ToString() : "?";
        string grounded = ball != null ? ball.IsGrounded.ToString() : "?";
        string flying = ball != null ? ball.IsFlying.ToString() : "?";
        float nearTh = ballRadius + 0.05f;

        var style = new GUIStyle { fontSize = 16, richText = true };
        style.normal.textColor = Color.white;

        string gapText = hitGround ? $"{gap:F2} m" : "지면 없음";
        string line2;
        if (!hitGround) line2 = "<color=red>아래에 콜라이더 없음 → 레이어/콜라이더 확인</color>";
        else if (gap > surfaceCheckDistance) line2 = "<color=red>간격 > 0.7 → 너무 높이 떠 있음(공 Y를 내리거나 지면을 올려야)</color>";
        else if (gap > nearTh) line2 = "<color=yellow>표면 감지 OK, 접지임계 초과 → 공 Y를 표면 바로 위로</color>";
        else line2 = "<color=lime>접지 가능 거리</color>";

        GUI.Box(new Rect(8, 8, 560, 132), GUIContent.none);
        GUI.Label(new Rect(16, 12, 540, 24), $"<b>[Ball Ground Debug]</b>  ballY={transform.position.y:F2}  radius={ballRadius:F3}", style);
        GUI.Label(new Rect(16, 36, 540, 24), $"지면까지 간격: <b>{gapText}</b>   (탐지한계 0.7 / 접지임계 {nearTh:F2})", style);
        GUI.Label(new Rect(16, 60, 540, 24), $"지면: {groundName}   Layer: {groundLayer}", style);
        GUI.Label(new Rect(16, 84, 540, 24), $"isGrounded={grounded}  isFlying={flying}  surface={surface}", style);
        GUI.Label(new Rect(16, 108, 540, 24), line2, style);
    }
}
