using UnityEngine;

public class WindManager : MonoBehaviour
{
    public static WindManager Instance { get; private set; }

    [Header("Wind")]
    // XZ 평면 기준 바람 방향입니다. Y값은 사용하지 않습니다.
    [SerializeField] private Vector3 windDirection = Vector3.forward;

    // 바람 세기입니다. 이 값은 공에 더해질 바람 가속도 크기로 사용합니다.
    [SerializeField] private float windStrength = 0f;

    public Vector3 WindDirection => windDirection;
    public float WindStrength => windStrength;

    private void Awake()
    {
        // 씬 전체에서 WindManager는 하나만 유지합니다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public Vector3 GetWindAccel()
    {
        // 문서 2.2: a_wind = windDirection.normalized * windStrength
        return windDirection.normalized * windStrength;
    }

    public void RandomizeWind()
    {
        // 임시 테스트용 랜덤 바람입니다.
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        float strength = Random.Range(0f, 3f);

        SetWind(direction, strength);
    }

    public void SetWind(Vector3 dir, float strength)
    {
        // 외부 UI나 GameManager에서 바람 방향과 세기를 직접 지정할 때 사용합니다.
        windDirection = Vector3.ProjectOnPlane(dir, Vector3.up);
        windStrength = Mathf.Max(0f, strength);
    }
}
