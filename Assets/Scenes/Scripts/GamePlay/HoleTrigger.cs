// HoleTrigger.cs
// HoleCup 오브젝트에 부착한다 (Tag: Hole, Collider isTrigger = true).
// 역할: 공이 홀컵 트리거에 들어오면 게임 흐름(클리어 판정)을 담당하는 GameManager에 홀인을 통지한다.
// BallPhysics.OnTriggerEnter의 Debug.Log는 그대로 두고, 실제 클리어 처리는 이 경로로 진행한다.
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoleTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Rules")]
    // 진입 속도가 이 값보다 빠르면 컵을 지나친 것으로 보고 무시합니다. 0 이하이면 속도 제한을 두지 않습니다.
    [SerializeField] private float maxEntrySpeed = 0f;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 공(BallPhysics)인지 확인합니다.
        BallPhysics ball = other.GetComponent<BallPhysics>();
        if (ball == null)
        {
            ball = other.GetComponentInParent<BallPhysics>();
        }

        if (ball == null)
        {
            return;
        }

        // 너무 빠른 진입은 홀인으로 인정하지 않습니다(선택 규칙).
        if (maxEntrySpeed > 0f && ball.Velocity.magnitude > maxEntrySpeed)
        {
            return;
        }

        if (gameManager != null)
        {
            gameManager.OnBallInHole();
        }
    }
}
