// BallAnimationController.cs
// Ball 오브젝트에 부착한다.
// 역할: 홀인 순간의 연출 애니메이션을 담당한다.
//       BallPhysics와 분리된 연출 전용 컴포넌트로, 물리 계산에 관여하지 않는다.
//       GameManager.OnBallInHole()에서 PlayHoleInAnimation()을 호출해 시작한다.
using System;
using System.Collections;
using UnityEngine;

public class BallAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform holeTransform;

    [Header("Animation")]
    // 홀인 전체 연출 시간입니다.
    [SerializeField] private float animDuration = 1f;
    // 낙하 시작 높이 — 홀컵 중심 위로 이 높이에서 공이 시작합니다.
    [SerializeField] private float dropHeight = 0.5f;

    // 애니메이션 완료 시 호출됩니다. GameManager가 구독해 ResultPanel을 엽니다.
    public Action OnAnimationComplete;

    private MeshRenderer meshRenderer;
    private Vector3 originalScale;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalScale = transform.localScale;
    }

    // GameManager.OnBallInHole()에서 호출합니다.
    public void PlayHoleInAnimation()
    {
        StartCoroutine(HoleInCoroutine());
    }

    private IEnumerator HoleInCoroutine()
    {
        if (holeTransform == null)
        {
            OnAnimationComplete?.Invoke();
            yield break;
        }

        Vector3 holePos = holeTransform.position;
        // 1. 공을 홀컵 바로 위로 텔레포트합니다.
        Vector3 startPos = holePos + Vector3.up * dropHeight;
        transform.position = startPos;

        // 2. dropHeight에서 홀컵 높이까지 서서히 낙하합니다.
        float half = animDuration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
            transform.position = Vector3.Lerp(startPos, holePos, t);
            yield return null;
        }
        transform.position = holePos;

        // 3. 홀컵 높이에 도달하면 MeshRenderer를 끕니다.
        if (meshRenderer != null) meshRenderer.enabled = false;

        // 4. 스케일을 0으로 줄입니다 (렌더러가 꺼진 상태라도 물리적 일관성 유지).
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / half);
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }
        transform.localScale = Vector3.zero;

        // 5. 완료 콜백을 호출합니다.
        OnAnimationComplete?.Invoke();
    }

    // BallPhysics.ResetBall() 이후 공을 재사용할 때 호출해 외형을 복원합니다.
    public void ResetAnimation()
    {
        StopAllCoroutines();
        if (meshRenderer != null) meshRenderer.enabled = true;
        transform.localScale = originalScale;
    }
}
