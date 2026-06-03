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

    [Header("Pitch")]
    [SerializeField] private float currentPitch = 25f;
    [SerializeField] private float pitchSpeed = 40f;
    [SerializeField] private float minPitch = 0f;
    [SerializeField] private float maxPitch = 45f;

    [Header("Aim")]
    [SerializeField] private float yawSpeed = 120f;

    public event Action<float, float, Vector3> OnShoot;

    public float CurrentPower => currentPower;
    public float CurrentPitch => currentPitch;
    public bool IsCharging => isCharging;

    private void Awake()
    {
        if (aimTarget == null)
        {
            BallPhysics ball = FindFirstObjectByType<BallPhysics>();
            if (ball != null)
            {
                aimTarget = ball.transform;
            }
        }
    }

    private void Update()
    {
        HandlePowerInput();
        HandlePitchInput();
        HandleAimInput();
    }

    private void HandlePowerInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentPower = 0f;
            isCharging = true;
        }

        if (Input.GetKey(KeyCode.Space) && isCharging)
        {
            currentPower = Mathf.Clamp01(currentPower + chargeSpeed * Time.deltaTime);
        }

        if (Input.GetKeyUp(KeyCode.Space) && isCharging)
        {
            isCharging = false;
            Shoot();
        }
    }

    private void HandlePitchInput()
    {
        float pitchInput = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            pitchInput += 1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            pitchInput -= 1f;
        }

        pitchInput += Input.mouseScrollDelta.y;
        currentPitch += pitchInput * pitchSpeed * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
    }

    private void HandleAimInput()
    {
        if (aimTarget == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        aimTarget.Rotate(Vector3.up, mouseX * yawSpeed * Time.deltaTime, Space.World);
    }

    private void Shoot()
    {
        float power = Mathf.Lerp(2f, 18f, currentPower);
        Vector3 forward = aimTarget != null ? aimTarget.forward : Vector3.forward;

        OnShoot?.Invoke(power, currentPitch, forward);
    }
}
