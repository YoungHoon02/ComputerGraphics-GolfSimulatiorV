using UnityEngine;

public static class PhysicsCore
{
    // Semi-implicit Euler 적분 1스텝입니다.
    // 속도를 먼저 갱신하고, 갱신된 속도로 위치를 이동시키는 방식입니다.
    public static void Step(ref Vector3 position, ref Vector3 velocity, Vector3 acceleration, float dt)
    {
        velocity += acceleration * dt;
        position += velocity * dt;
    }

    // 발사 파워와 발사 각도를 초기 속도 벡터로 변환합니다.
    public static Vector3 CalculateLaunchVelocity(float power, float pitch, Vector3 forward)
    {
        float pitchRad = pitch * Mathf.Deg2Rad;

        // 카메라/공의 forward가 살짝 위아래로 기울어져 있어도 XZ 평면 방향만 사용합니다.
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 direction = flatForward * Mathf.Cos(pitchRad) + Vector3.up * Mathf.Sin(pitchRad);

        return direction.normalized * power;
    }
}
