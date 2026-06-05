// UIManager.cs
// Canvas(또는 빈 UI 관리 오브젝트)에 부착한다.
// 역할: 파워 게이지, 타수, 바람 방향/세기, 공~홀컵 거리 표시를 매 프레임 갱신하고, 홀인 시 결과 패널을 띄운다.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References - Sources")]
    [SerializeField] private InputManager input;
    [SerializeField] private ScoreManager score;
    [SerializeField] private BallPhysics ball;
    [SerializeField] private Transform holeTransform;

    [Header("UI - Power")]
    // PowerGauge_Panel > Power_Slider
    [SerializeField] private Slider powerSlider;

    [Header("UI - Score")]
    // ShotCount_Text
    [SerializeField] private TMP_Text shotCountText;

    [Header("UI - Wind")]
    // WindIndicator_Panel > WindArrow_Image
    [SerializeField] private Image windArrow;
    // WindIndicator_Panel > WindStrength_Text
    [SerializeField] private TMP_Text windStrengthText;

    [Header("UI - Distance")]
    // Distance_Text
    [SerializeField] private TMP_Text distanceText;

    [Header("UI - Result")]
    // ResultPanel (기본 비활성)
    [SerializeField] private GameObject resultPanel;

    private void Awake()
    {
        // Inspector 연결이 없을 때를 대비한 자동 연결입니다.
        if (input == null)
        {
            input = FindFirstObjectByType<InputManager>();
        }

        if (score == null)
        {
            score = FindFirstObjectByType<ScoreManager>();
        }

        if (ball == null)
        {
            ball = FindFirstObjectByType<BallPhysics>();
        }

        // 결과 패널은 시작 시 항상 숨깁니다.
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void Update()
    {
        UpdatePower();
        UpdateScore();
        UpdateWind();
        UpdateDistance();
    }

    private void UpdatePower()
    {
        // InputManager.CurrentPower(0~1)를 슬라이더에 반영합니다.
        if (powerSlider != null && input != null)
        {
            powerSlider.value = input.CurrentPower;
        }
    }

    private void UpdateScore()
    {
        // ScoreManager.TotalShots를 텍스트에 반영합니다.
        if (shotCountText != null && score != null)
        {
            shotCountText.text = $"Shots: {score.TotalShots}";
        }
    }

    private void UpdateWind()
    {
        if (WindManager.Instance == null)
        {
            return;
        }

        Vector3 windDir = WindManager.Instance.WindDirection;
        float windStrength = WindManager.Instance.WindStrength;

        // 바람 방향(XZ 평면)을 화면상의 화살표 Z축 회전으로 변환합니다.
        if (windArrow != null && windDir.sqrMagnitude > 0.0001f)
        {
            // XZ 평면의 방향을 화면 각도로 매핑합니다(+Z를 위쪽으로 간주).
            float angle = Mathf.Atan2(windDir.x, windDir.z) * Mathf.Rad2Deg;
            windArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        if (windStrengthText != null)
        {
            windStrengthText.text = $"Wind: {windStrength:F1}";
        }
    }

    private void UpdateDistance()
    {
        // 공과 홀컵 사이의 거리를 표시합니다.
        if (distanceText != null && ball != null && holeTransform != null)
        {
            float dist = Vector3.Distance(ball.transform.position, holeTransform.position);
            distanceText.text = $"Distance: {dist:F1}m";
        }
    }

    public void ShowResultPanel(bool win)
    {
        // 홀인 등 라운드 종료 시 결과 패널을 표시합니다.
        if (resultPanel != null)
        {
            resultPanel.SetActive(win);
        }
    }
}
