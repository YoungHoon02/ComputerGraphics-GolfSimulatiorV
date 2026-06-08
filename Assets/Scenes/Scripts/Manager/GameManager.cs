// GameManager.cs
// 역할: 게임 전체 흐름을 관리한다.
//   - InputManager.OnShoot → ball.Launch() + 타수 증가 + 발사 위치 저장
//   - 매 Update: 공 정지 감지(비거리 계산·UI 표시) + 표면 변화 감지(상황 텍스트)
//   - OnBallInHole: 홀인 연출(카메라·애니메이션·결과 패널) 처리
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private BallPhysics ball;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CameraController cameraController;
    // HoleCup Transform: 홀인 연출 및 비거리 기준입니다.
    [SerializeField] private Transform holeTransform;

    // IsFlying 폴링 — true→false 전환 시 공 정지 처리합니다.
    private bool wasFlying;

    // 발사 시점 위치 — 정지 후 비거리 계산에 사용합니다.
    private Vector3 shotStartPosition;

    // 표면 변화 감지 — 바뀔 때마다 상황 텍스트를 표시합니다.
    private SurfaceType previousSurface = SurfaceType.None;

    // Ball에 부착된 홀인 애니메이션 컴포넌트입니다.
    private BallAnimationController ballAnimController;

    private void Awake()
    {
        if (inputManager == null)
            inputManager = FindAnyObjectByType<InputManager>();

        if (ball == null)
            ball = FindAnyObjectByType<BallPhysics>();

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<CameraController>();

        // BallAnimationController는 Ball에 부착되어 있어야 합니다.
        // 없으면 자동으로 추가합니다.
        if (ball != null)
        {
            ballAnimController = ball.GetComponent<BallAnimationController>();
            if (ballAnimController == null)
                ballAnimController = ball.gameObject.AddComponent<BallAnimationController>();
        }
    }

    [Header("Hole Settings")]
    [SerializeField] private int holeNumber = 1;
    [SerializeField] private int holePar = 4;

    [Header("Club Settings")]
    [SerializeField] private string clubName = "1W";
    [SerializeField] private string clubDirection = "N";

    private void Start()
    {
        if (uiManager == null) return;

        // 티(공 시작 위치)~홀컵 거리를 자동 계산해 야드로 변환합니다.
        int holeYards = 0;
        if (ball != null && holeTransform != null)
        {
            float meters = Vector3.Distance(ball.transform.position, holeTransform.position);
            holeYards = Mathf.RoundToInt(meters * 1.094f);
        }

        uiManager.SetHoleInfo(holeNumber, holeYards, holePar);

        // 클럽 거리도 홀컵까지 거리 기준으로 표시합니다.
        uiManager.UpdateClubInfo(clubName, clubDirection, holeYards);
    }

    private void OnEnable()
    {
        if (inputManager != null)
            inputManager.OnShoot += HandleShoot;
    }

    private void OnDisable()
    {
        if (inputManager != null)
            inputManager.OnShoot -= HandleShoot;
    }

    private void Update()
    {
        if (ball == null) return;

        bool isFlying = ball.IsFlying;

        // IsFlying true→false: 공 정지 처리합니다.
        if (wasFlying && !isFlying)
            HandleBallStopped();

        wasFlying = isFlying;

        // 표면 변화 감지: 타격 후 공이 움직이는 동안만 확인합니다.
        if (isFlying || ball.IsGrounded)
            CheckSurfaceChange();
    }

    private void HandleShoot(float power, float pitch, Vector3 forward)
    {
        if (ball == null) return;

        // 발사 위치를 기록합니다.
        shotStartPosition = ball.transform.position;

        ball.Launch(power, pitch, forward);

        if (scoreManager != null)
            scoreManager.AddShot();
    }

    private void HandleBallStopped()
    {
        if (uiManager == null || ball == null) return;

        // 비거리를 미터 → 야드로 환산합니다 (1m ≈ 1.094yd).
        float meters = Vector3.Distance(shotStartPosition, ball.transform.position);
        int yards = Mathf.RoundToInt(meters * 1.094f);

        // 첫 번째 샷은 "Drive", 이후는 "Shot"으로 표시합니다.
        string label = (scoreManager != null && scoreManager.TotalShots == 1) ? "Drive" : "Shot";
        uiManager.ShowHitResult($"{label}: {yards}Y");
    }

    private void CheckSurfaceChange()
    {
        SurfaceType current = ball.CurrentSurface;
        if (current == previousSurface) return;

        previousSurface = current;

        // 그린 진입/이탈 시 퍼터 자동 전환합니다.
        if (inputManager != null)
            inputManager.SetPutterMode(current == SurfaceType.Green);

        if (uiManager == null) return;

        switch (current)
        {
            case SurfaceType.Green:
                uiManager.ShowSituationText("Green Edge");
                break;
            case SurfaceType.Bunker:
                uiManager.ShowSituationText("Bunker!");
                break;
            case SurfaceType.Rough:
                uiManager.ShowSituationText("Rough");
                break;
            case SurfaceType.OutOfBounds:
                uiManager.ShowSituationText("Out of Bounds!");
                break;
        }
    }

    // HoleTrigger가 홀인을 감지했을 때 호출합니다.
    public void OnBallInHole()
    {
        // 1. 카메라를 HoleIn 상태로 전환합니다.
        if (cameraController != null)
            cameraController.SetHoleInCamera();

        // 2. 공 물리를 멈춥니다.
        if (ball != null)
            ball.Stop();

        // 3. 홀인 애니메이션을 재생하고, 완료 후 결과를 표시합니다.
        if (ballAnimController != null)
        {
            // 매번 깨끗하게 교체해 중복 구독을 방지합니다.
            ballAnimController.OnAnimationComplete = () =>
            {
                if (uiManager != null && scoreManager != null)
                    uiManager.ShowHoleInResult(scoreManager.TotalShots);
            };
            ballAnimController.PlayHoleInAnimation();
        }
        else
        {
            // 애니메이션 컴포넌트가 없으면 즉시 결과를 표시합니다.
            if (uiManager != null && scoreManager != null)
                uiManager.ShowHoleInResult(scoreManager.TotalShots);
        }
    }
}
