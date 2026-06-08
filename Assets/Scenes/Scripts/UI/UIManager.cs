// UIManager.cs
// Canvas에 부착한다.
// 역할: 파워 게이지·타수·바람·거리 등 실시간 UI 갱신 및
//       홀 정보·클럽 정보·상황 텍스트·타격 결과·홀인 결과 표시를 담당한다.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager input;
    [SerializeField] private ScoreManager score;
    [SerializeField] private BallPhysics ball;
    [SerializeField] private Transform holeTransform;

    // ── 기존 UI ──────────────────────────────────────────────────────────────

    [Header("Power")]
    [SerializeField] private Slider powerSlider;

    [Header("Score")]
    [SerializeField] private TMP_Text shotCountText;

    [Header("Wind")]
    [SerializeField] private Image windArrow;
    [SerializeField] private TMP_Text windStrengthText;

    [Header("Distance to Hole")]
    [SerializeField] private TMP_Text distanceText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;

    // ── 추가 UI ──────────────────────────────────────────────────────────────

    [Header("Hole Info  (좌상단: 1H / 374Y / PAR 4)")]
    [SerializeField] private TMP_Text holeInfoText;

    [Header("Club Info  (하단: 1W N 202Y)")]
    [SerializeField] private TMP_Text clubInfoText;

    [Header("Situation Text  (페이드인·아웃)")]
    [SerializeField] private TMP_Text situationText;
    [SerializeField] private float situationFadeIn  = 0.3f;
    [SerializeField] private float situationFadeOut = 0.5f;

    [Header("Hit Result  (타격 후 비거리 등)")]
    [SerializeField] private TMP_Text hitResultText;

    [Header("Hole-In Result  (홀인 결과)")]
    [SerializeField] private TMP_Text holeInResultText;

    // 현재 실행 중인 상황 텍스트 Coroutine을 추적합니다.
    private Coroutine situationRoutine;
    private Coroutine hitResultRoutine;

    private void Awake()
    {
        if (input == null) input = FindAnyObjectByType<InputManager>();
        if (score == null) score = FindAnyObjectByType<ScoreManager>();
        if (ball  == null) ball  = FindAnyObjectByType<BallPhysics>();

        if (resultPanel != null)   resultPanel.SetActive(false);

        // 상황 텍스트는 투명하게 시작합니다.
        SetTextAlpha(situationText, 0f);
        SetTextAlpha(hitResultText, 0f);
        SetTextAlpha(holeInResultText, 0f);
    }

    private void Update()
    {
        UpdatePower();
        UpdateScore();
        UpdateWind();
        UpdateDistance();
    }

    // ── 기존 메서드 ───────────────────────────────────────────────────────────

    private void UpdatePower()
    {
        if (powerSlider != null && input != null)
            powerSlider.value = input.CurrentPower;
    }

    private void UpdateScore()
    {
        if (shotCountText != null && score != null)
            shotCountText.text = $"Shots: {score.TotalShots}";
    }

    private void UpdateWind()
    {
        if (WindManager.Instance == null) return;

        Vector3 windDir = WindManager.Instance.WindDirection;
        float strength  = WindManager.Instance.WindStrength;

        if (windArrow != null && windDir.sqrMagnitude > 0.0001f)
        {
            // 스프라이트 기본 방향이 남쪽(↓)이므로 180도 오프셋을 더합니다.
            float angle = Mathf.Atan2(windDir.x, windDir.z) * Mathf.Rad2Deg;
            windArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle + 180f);
        }

        if (windStrengthText != null)
            windStrengthText.text = $"Wind: {strength:F1}";
    }

    private void UpdateDistance()
    {
        if (distanceText != null && ball != null && holeTransform != null)
        {
            float dist = Vector3.Distance(ball.transform.position, holeTransform.position);
            distanceText.text = $"Distance: {dist:F1}m";
        }
    }

    public void ShowResultPanel(bool win)
    {
        if (resultPanel != null) resultPanel.SetActive(win);
    }

    // ── 추가 메서드 ───────────────────────────────────────────────────────────

    // 홀 정보를 좌상단에 표시합니다 (예: "1H\n374Y\nPAR 4").
    public void SetHoleInfo(int hole, int yards, int par)
    {
        if (holeInfoText != null)
            holeInfoText.text = $"{hole}H\n{yards}Y\nPAR {par}";
    }

    // 클럽 정보를 하단에 표시합니다 (예: "1W N 202Y").
    public void UpdateClubInfo(string clubName, string direction, int yards)
    {
        if (clubInfoText != null)
            clubInfoText.text = $"{clubName} {direction} {yards}Y";
    }

    // 상황 텍스트를 페이드인 → 유지 → 페이드아웃 순서로 표시합니다.
    public void ShowSituationText(string text, float duration = 2f)
    {
        if (situationText == null) return;

        // 이미 실행 중이면 취소하고 새로 시작합니다.
        if (situationRoutine != null) StopCoroutine(situationRoutine);
        situationRoutine = StartCoroutine(SituationTextRoutine(text, duration));
    }

    private IEnumerator SituationTextRoutine(string text, float duration)
    {
        situationText.text = text;

        // 페이드인
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / situationFadeIn;
            SetTextAlpha(situationText, Mathf.Clamp01(t));
            yield return null;
        }
        SetTextAlpha(situationText, 1f);

        // 유지
        yield return new WaitForSeconds(duration);

        // 페이드아웃
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / situationFadeOut;
            SetTextAlpha(situationText, Mathf.Clamp01(t));
            yield return null;
        }
        SetTextAlpha(situationText, 0f);
    }

    // 타격 결과 텍스트를 일정 시간 표시합니다 (예: "Drive: 185Y").
    public void ShowHitResult(string result, float duration = 2f)
    {
        if (hitResultText == null) return;

        if (hitResultRoutine != null) StopCoroutine(hitResultRoutine);
        hitResultRoutine = StartCoroutine(HitResultRoutine(result, duration));
    }

    private IEnumerator HitResultRoutine(string result, float duration)
    {
        hitResultText.text = result;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / situationFadeIn;
            SetTextAlpha(hitResultText, Mathf.Clamp01(t));
            yield return null;
        }
        SetTextAlpha(hitResultText, 1f);

        yield return new WaitForSeconds(duration);

        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / situationFadeOut;
            SetTextAlpha(hitResultText, Mathf.Clamp01(t));
            yield return null;
        }
        SetTextAlpha(hitResultText, 0f);
    }

    // 홀인 결과를 표시하고 ResultPanel을 엽니다.
    public void ShowHoleInResult(int shots)
    {
        if (holeInResultText != null)
        {
            SetTextAlpha(holeInResultText, 1f);
            holeInResultText.text = $"HOLE IN!\n{shots} Shots";
        }

        ShowResultPanel(true);
    }

    // TMP_Text의 색상 알파값만 바꾸는 헬퍼입니다.
    private static void SetTextAlpha(TMP_Text label, float alpha)
    {
        if (label == null) return;
        Color c = label.color;
        c.a = alpha;
        label.color = c;
    }
}
