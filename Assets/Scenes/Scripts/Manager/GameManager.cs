using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private BallPhysics ball;

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

    private void HandleShoot(float power, float pitch, Vector3 forward)
    {
        if (ball == null)
        {
            return;
        }

        ball.Launch(power, pitch, forward);
    }
}
