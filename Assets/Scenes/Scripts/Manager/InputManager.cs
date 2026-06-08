using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform aimTarget;

    [Header("Power")]
    [SerializeField] private float currentPower;
    [SerializeField] private float chargeSpeed = 1.2f;
    [SerializeField] private bool isCharging;
    private bool chargingUp = true;

    [Header("Pitch")]
    [SerializeField] private float currentPitch = 25f;
    [SerializeField] private float pitchSpeed = 40f;
    [SerializeField] private float minPitch = 0f;
    [SerializeField] private float maxPitch = 45f;

    [Header("Aim")]
    [SerializeField] private float yawSpeed = 120f;

    [Header("Putter")]
    // 퍼터 모드 파워 범위입니다. 일반 클럽(2~18)보다 훨씬 좁습니다.
    [SerializeField] private float putterMinPower = 0.3f;
    [SerializeField] private float putterMaxPower = 5f;

    public event Action<float, float, Vector3> OnShoot;

    public float CurrentPower => currentPower;
    public float CurrentPitch => currentPitch;
    public bool IsCharging => isCharging;
    public bool IsPutterMode { get; private set; }

    // 현재 조준 방향을 8방위 문자열로 반환합니다 (UI ClubInfo 표시용).
    public string AimDirection
    {
        get
        {
            if (aimTarget == null) return "N";
            float y = aimTarget.eulerAngles.y;
            // 0=N, 45=NE, 90=E, 135=SE, 180=S, 225=SW, 270=W, 315=NW
            int idx = Mathf.RoundToInt(y / 45f) % 8;
            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return dirs[idx < 0 ? idx + 8 : idx];
        }
    }

    private void Awake()
    {
        if (aimTarget == null)
        {
            BallPhysics ball = FindFirstObjectByType<BallPhysics>();
            if (ball != null) aimTarget = ball.transform;
        }
    }

    private void Update()
    {
        HandlePowerInput();
        HandlePitchInput();
        HandleAimInput();
    }

    // GameManager가 그린 감지 시 호출합니다.
    public void SetPutterMode(bool enabled)
    {
        IsPutterMode = enabled;
        // 퍼터는 지면 굴리기 — 피치를 0으로 고정합니다.
        if (enabled) currentPitch = 0f;
    }

    private void HandlePowerInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPower = 0f;
            isCharging = true;
            chargingUp = true;
        }

        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            if (chargingUp)
            {
                currentPower += chargeSpeed * Time.deltaTime;
                if (currentPower >= 1f) { currentPower = 1f; chargingUp = false; }
            }
            else
            {
                currentPower -= chargeSpeed * Time.deltaTime;
                if (currentPower <= 0f) { currentPower = 0f; chargingUp = true; }
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            isCharging = false;
            Shoot();
        }
    }

    private void HandlePitchInput()
    {
        // 퍼터 모드에서는 피치 조작을 막습니다.
        if (IsPutterMode) return;

        float pitchInput = 0f;
        if (Input.GetKey(KeyCode.Q)) pitchInput += 1f;
        if (Input.GetKey(KeyCode.E)) pitchInput -= 1f;
        pitchInput += Input.mouseScrollDelta.y;
        currentPitch += pitchInput * pitchSpeed * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
    }

    private void HandleAimInput()
    {
        if (aimTarget == null) return;
        float mouseX = Input.GetAxis("Mouse X");
        aimTarget.Rotate(Vector3.up, mouseX * yawSpeed * Time.deltaTime, Space.World);
    }

    private void Shoot()
    {
        float power = IsPutterMode
            ? Mathf.Lerp(putterMinPower, putterMaxPower, currentPower)
            : Mathf.Lerp(2f, 18f, currentPower);

        Vector3 forward = aimTarget != null ? aimTarget.forward : Vector3.forward;
        OnShoot?.Invoke(power, currentPitch, forward);
    }
}
