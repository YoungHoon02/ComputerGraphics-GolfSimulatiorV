using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private BallPhysics ball;
    // 추가 참조: 타수/UI 흐름을 관리합니다.
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UIManager uiManager;

    // 공 정지 감지를 위해 직전 프레임의 비행 상태를 저장합니다.
    private bool wasFlying;

    private void Awake()
    {
        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<InputManager>();
        }

        if (ball == null)
        {
            ball = FindFirstObjectByType<BallPhysics>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnShoot += HandleShoot;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnShoot -= HandleShoot;
        }
    }

    private void Update()
    {
        // BallPhysics에는 정지 이벤트가 없으므로, IsFlying의 true→false 전환을 폴링해 정지를 감지합니다.
        if (ball == null)
        {
            return;
        }

        bool isFlying = ball.IsFlying;
        if (wasFlying && !isFlying)
        {
            HandleBallStopped();
        }

        wasFlying = isFlying;
    }

    private void HandleShoot(float power, float pitch, Vector3 forward)
    {
        if (ball == null)
        {
            return;
        }

        ball.Launch(power, pitch, forward);

        // 한 번 칠 때마다 타수를 증가시킵니다.
        if (scoreManager != null)
        {
            scoreManager.AddShot();
        }
    }

    private void HandleBallStopped()
    {
        // 공이 멈췄을 때의 처리 지점입니다. (다음 샷 준비 등 확장 가능)
    }

    // HoleTrigger가 홀인을 통지하면 호출됩니다. 클리어 처리를 담당합니다.
    public void OnBallInHole()
    {
        if (uiManager != null)
        {
            uiManager.ShowResultPanel(true);
        }

        if (ball != null)
        {
            ball.Stop();
        }
    }
}
