// ScoreManager.cs
// 역할: 타수와 벌타를 관리한다. 타수 증가는 GameManager가 발사 처리 시 AddShot()을 호출해 갱신한다.
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score")]
    // 일반 타수 + 벌타의 합계를 외부에 노출합니다.
    [SerializeField] private int shots;
    [SerializeField] private int penalties;

    // 총 타수 = 친 횟수 + 벌타입니다.
    public int TotalShots => shots + penalties;

    public void AddShot()
    {
        // 한 번 칠 때마다 호출합니다.
        shots++;
    }

    public void AddPenalty()
    {
        // OB 등으로 벌타가 발생할 때 호출합니다.
        penalties++;
    }

    public void Reset()
    {
        // 라운드 재시작 시 타수/벌타를 초기화합니다.
        shots = 0;
        penalties = 0;
    }
}
