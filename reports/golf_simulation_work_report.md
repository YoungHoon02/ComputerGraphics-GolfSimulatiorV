# Unity 6 Golf Simulation 작업보고서

작성일: 2026-06-03  
참조 문서: `golf_simulation_report_unity6.docx`  
검증 그래프: `ball_speed_decay_by_surface.html`

## 1. 작업 개요

본 작업은 기존 Unity 6 골프 물리 시뮬레이션 설계 문서의 핵심 지침을 기준으로, 현재 프로젝트의 구현 상태를 점검하고 미동작하던 표면별 마찰 적용 문제를 해결한 뒤 물리 그래프를 통해 검증한 내용을 정리한 것이다.

핵심 해결 사항은 다음과 같다.

- 공의 이동은 Unity Rigidbody 자동 물리가 아니라 자체 `Vector3 velocity`와 `PhysicsCore.Step()`으로 계산한다.
- `Rigidbody.useGravity = false` 상태를 유지하고, 중력/바람/마찰 가속도는 코드에서 직접 합성한다.
- 표면별 마찰이 `Current Surface = None` 상태 때문에 동일하게 적용되던 문제를 Raycast 기반 표면 판정으로 해결했다.
- 충돌 반발은 `OnCollisionEnter()`와 `collision.GetContact(0)`을 사용해 처리한다.
- 표면별 감속 그래프를 통해 Bunker, Rough, Fairway의 감속률 차이를 확인했다.

## 2. 기존 문서 지침 대비 준수 현황

| 문서 지침 | 현재 적용 상태 | 판단 |
|---|---|---|
| Unity 6 API 사용 | `Rigidbody.linearVelocity`, `collision.GetContact(0)` 사용 | 준수 |
| `Rigidbody.useGravity = false` | `Awake()`에서 `rb.useGravity = false` 설정 | 준수 |
| `Rigidbody.AddForce()` 사용 금지 | 자체 `velocity` 변수로 위치/속도 계산, `AddForce()` 미사용 | 준수 |
| Physics Material 사용 금지 | Collider의 Physics Material을 사용하지 않고 마찰/반발계수를 코드에서 관리 | 준수 |
| Semi-implicit Euler 적분 | `PhysicsCore.Step()`에서 `velocity += a * dt`, `position += velocity * dt` 순서 사용 | 준수 |
| FixedUpdate 기반 물리 계산 | `BallPhysics.FixedUpdate()`에서 물리 적분 수행 | 준수 |
| 표면별 마찰 적용 | Raycast로 표면 Layer를 읽고 `SurfaceType`에 따라 마찰계수 선택 | 해결 |
| 충돌 반발 처리 | `OnCollisionEnter()`에서 접점 법선과 표면별 반발계수 사용 | 준수 |
| 바람 외력 | `WindManager.GetWindAccel()`을 총 가속도에 합산 | 준수 |
| Input System 설정 | Project Settings의 Active Input Handling이 `Both` | 준수 |
| Physics Fixed Timestep | 프로젝트 설정에서 Fixed Timestep 약 0.02초 | 준수 |
| Bounce Threshold | 프로젝트 설정에서 `m_BounceThreshold: 2` | 준수 |
| Solver Iterations | 프로젝트 설정에서 `m_DefaultSolverIterations: 12` | 준수 |
| Render Pipeline | 기존 문서 권장값은 Built-in RP이나, URP도 선택 가능하다고 명시되어 있음. 현재 프로젝트는 URP/Lit 재질 사용 | 차이 있음, 허용 범위 |

## 3. 기존 서류 2번: 핵심 물리 수식 반영 현황

기존 작업 서류의 2번은 본 프로젝트의 핵심 물리 수식을 설명하는 부분이다. 현재 프로젝트에서는 이 항목을 단순 설명이 아니라 실제 코드 구조와 검증 결과로 연결했다.

| 기존 서류 2번 항목 | 현재 코드 반영 위치 | 준수/차이점 |
|---|---|---|
| 2.1 Semi-implicit Euler 적분 | `PhysicsCore.Step(ref position, ref velocity, a_total, dt)` | 문서 수식과 동일하게 속도를 먼저 갱신하고, 갱신된 속도로 위치를 이동한다. `FixedUpdate()`에서 고정 시간 간격으로 호출되므로 문서 지침을 준수한다. |
| 2.2 바람 외력 | `WindManager.GetWindAccel()`, `a_total = a_gravity + a_wind + frictionAccel` | 바람 방향을 정규화하고 세기를 곱해 가속도로 사용한다. 현재 `WindStrength`가 0이면 영향이 없지만, 구조상 문서 수식을 반영할 수 있다. |
| 2.3 구름 마찰 감속 | `GetFrictionAccel()`, `GetSurfaceFriction()` | `-mu * gravity * velocity.normalized` 구조를 사용한다. 기존에는 `currentSurface=None`으로 표면별 분기가 적용되지 않았으나, Raycast 표면 판정 추가 후 표면별 마찰계수 차이가 그래프로 확인되었다. |
| 2.4 충돌 반발 | `OnCollisionEnter(Collision collision)`, `collision.GetContact(0).normal`, `GetRestitution()` | Unity 6 지침대로 `collision.contacts[0]` 대신 `GetContact(0)`을 사용한다. 문서 예시는 `Vector3.Reflect` 중심이지만, 현재 코드는 법선 성분과 접선 성분을 분리해 `-e * v_n + (1 - f) * v_t` 방식으로 반발과 접선 손실을 계산한다. 방향성은 문서의 반발계수 적용 목적과 같지만 구현식은 더 세분화되어 있다. |

즉 기존 서류 2번의 핵심은 “속도-위치 적분, 바람 가속도, 표면별 마찰, 충돌 반발”이며, 현재 프로젝트는 이 중 표면 판정을 Raycast로 보완한 점을 제외하면 핵심 계산 흐름을 유지한다.

## 4. 해결된 문제

### 4.1 표면별 마찰이 동일하게 적용되던 문제

기존 상태에서는 공이 Rough, Bunker, Fairway 등 다른 표면으로 이동해도 속도 감소량이 동일하게 나타났다. 로그 분석 결과 여러 샷에서 속도 감소량이 거의 같은 간격으로 줄어들었다.

예시:

- Shot A: `10.332 -> 8.164 -> 5.996 -> 3.828 -> 1.660 -> 0`
- Shot B: `11.666 -> 9.498 -> 7.330 -> 5.162 -> 2.994 -> 0.826 -> 0`
- Shot C: `12.000 -> 9.832 -> 7.664 -> 5.496 -> 3.328 -> 1.160 -> 0`

이는 `currentSurface`가 `None`으로 유지되어 `GetSurfaceFriction()`의 기본 분기인 `fairwayFriction`만 사용되었음을 의미한다.

### 4.2 해결 방식

표면 판정은 충돌 이벤트에만 의존하지 않고, 공 아래 방향으로 짧은 Raycast를 수행하여 현재 표면을 직접 확인하도록 변경했다.

적용 방식:

- `surfaceCheckDistance = 0.7f` 추가
- `UpdateCurrentSurfaceByRaycast()` 추가
- `FixedUpdate()`에서 이동 전후로 현재 표면 갱신
- Raycast가 맞은 Collider의 Layer를 `LayerToSurface()`로 변환
- `currentSurface`를 갱신하여 다음 마찰 계산에 반영

이 구조에서 충돌 반발은 기존대로 `OnCollisionEnter()`가 담당하고, 표면별 마찰 판정은 Raycast가 담당한다. 즉 물리 충돌 반발과 게임 규칙상 표면 판정을 분리한 구조이다.

## 5. 물리 그래프 검증

검증 그래프는 표면별 마찰계수에 따른 속도 감소가 서로 다르게 나타나는지 확인하기 위해 작성되었다.

감속 공식:

```text
a_friction = -mu * gravity * velocity.normalized
감속 크기 = mu * gravity
```

현재 구현/검증 값:

| 표면 | 마찰계수 μ | 이론 감속률 μ*g | 그래프상 정지 시간 |
|---|---:|---:|---:|
| Bunker | 2.20 | 약 21.58 m/s² | 약 0.80초 |
| Rough | 1.40 | 약 13.73 m/s² | 약 1.05초 |
| Fairway | 0.85 | 약 8.34 m/s² | 약 1.79초 |

결론:

- Bunker는 가장 큰 마찰계수로 가장 빠르게 정지한다.
- Rough는 중간 수준의 감속률을 보인다.
- Fairway는 가장 낮은 감속률로 가장 멀리 굴러간다.
- 따라서 표면별 마찰 분기가 속도 감소 데이터로 확인되었다.

## 6. 그래프

```html
<svg viewBox="0 0 760 380" width="100%" height="380" role="img" aria-label="표면별 공 속도 감소 그래프">
  <rect x="0" y="0" width="760" height="380" fill="#ffffff"/>
  <text x="70" y="24" font-size="18" font-weight="700">표면별 속도 감소 검증</text>
  <line x1="70" y1="300" x2="670" y2="300" stroke="#222"/>
  <line x1="70" y1="40" x2="70" y2="300" stroke="#222"/>
  <text x="345" y="345" font-size="13">시간 (초)</text>
  <text x="12" y="180" font-size="13" transform="rotate(-90 12,180)">속도 (m/s)</text>
  <text x="55" y="304" font-size="11">0</text>
  <text x="48" y="223" font-size="11">5</text>
  <text x="42" y="142" font-size="11">10</text>
  <text x="42" y="61" font-size="11">15</text>
  <text x="70" y="318" font-size="11">0</text>
  <text x="228" y="318" font-size="11">0.5</text>
  <text x="386" y="318" font-size="11">1.0</text>
  <text x="544" y="318" font-size="11">1.5</text>
  <polyline fill="none" stroke="#D85A30" stroke-width="3" points="70,48.9 152.1,140.1 234.2,231.3 316.3,294.4 322.6,300"/>
  <polyline fill="none" stroke="#BA7517" stroke-width="3" stroke-dasharray="8,5" points="70,78.8 152.1,136.8 234.2,194.8 316.3,252.9 398.4,298.1 401.6,300"/>
  <polyline fill="none" stroke="#1D9E75" stroke-width="3" points="70,56.7 152.1,91.9 234.2,127.1 316.3,162.4 398.4,197.6 480.5,232.8 562.6,268.1 635.3,300"/>
  <rect x="500" y="50" width="14" height="4" fill="#D85A30"/><text x="522" y="56" font-size="12">Bunker μ=2.2</text>
  <rect x="500" y="72" width="14" height="4" fill="#BA7517"/><text x="522" y="78" font-size="12">Rough μ=1.4</text>
  <rect x="500" y="94" width="14" height="4" fill="#1D9E75"/><text x="522" y="100" font-size="12">Fairway μ=0.85</text>
</svg>
```

## 7. 기존 문서와 다른 점

### 7.1 표면별 마찰계수 값

기존 설계 문서의 예시 계수는 다음과 같았다.

- Green: 0.05
- Fairway: 0.15
- Rough: 0.35
- Bunker: 0.70

현재 구현/검증에 사용된 값은 다음과 같다.

- Green: 0.45
- Fairway: 0.85
- Rough: 1.40
- Bunker: 2.20

차이점:

- 현재 값은 시연 중 표면별 감속 차이를 뚜렷하게 확인하기 위해 더 큰 값으로 조정되었다.
- 문서의 값보다 현실 물리보다는 시각적/검증 편의성이 더 강한 설정이다.
- 보고서에는 “시연 검증용 조정값”으로 명시하는 것이 적절하다.

### 7.2 표면 판정 방식

기존 문서 흐름은 Collider 충돌과 Layer 기반 표면 판정을 중심으로 설명되어 있다. 현재 프로젝트는 표면별 마찰의 안정성을 위해 Raycast 기반 표면 판정을 추가했다.

차이점:

- 충돌 반발: `OnCollisionEnter()` 유지
- 표면 마찰 판정: Raycast로 변경

이 차이는 구현 안정성을 위한 보완이며, `Physics.Raycast`는 기존 문서에서도 Unity 6에서 동일하게 유지되는 API로 언급되어 있다.

### 7.3 Fairway Layer 이름

프로젝트 Layer에는 `Fairaway` 오타가 존재한다. 코드에서는 이를 고려해 `Fairway`와 `Fairaway`를 모두 `SurfaceType.Fairway`로 매핑한다.

```csharp
case "Fairway":
case "Fairaway":
    return SurfaceType.Fairway;
```

## 8. 단계 2/3/4 변경 주의 핵심 함수

아래 함수들은 기존 서류의 단계 2, 3, 4 물리 검증과 직접 연결되는 함수이다. 변경하면 바람, 표면별 마찰, 충돌 반발 결과가 달라지므로 시연 이후에는 임의로 수정하지 않는 것이 좋다.

| 단계 | 기능 | 변경 주의 함수 | 변경 시 영향 |
|---|---|---|---|
| 2 | 바람 외력 | `WindManager.GetWindAccel()` | 바람 방향/세기 계산식이 바뀌어 공의 비행 궤적과 감속 그래프가 달라질 수 있다. |
| 2 | 총 가속도 합성 | `BallPhysics.FixedUpdate()` 내부 `a_total = a_gravity + a_wind + frictionAccel` | 중력, 바람, 마찰의 합성 순서나 항목이 바뀌면 기존 물리 수식 검증과 달라진다. |
| 3 | 표면 판정 | `BallPhysics.UpdateCurrentSurfaceByRaycast()` | 현재 표면을 잘못 감지하면 Rough, Bunker, Fairway별 마찰 분기가 다시 적용되지 않을 수 있다. |
| 3 | 표면별 마찰계수 선택 | `BallPhysics.GetSurfaceFriction()` | 표면별 감속률 그래프의 핵심 기준이다. 값을 바꾸면 정지 시간과 이동거리가 바뀐다. |
| 3 | 마찰 가속도 계산 | `BallPhysics.GetFrictionAccel()` | `-mu * gravity * velocity.normalized` 공식이 깨지면 기존 서류 2.3의 마찰 수식과 달라진다. |
| 3/4 | Layer를 SurfaceType으로 변환 | `BallPhysics.LayerToSurface(int layer)` | Layer 매핑이 바뀌면 표면별 마찰과 표면별 반발계수 선택이 모두 영향을 받는다. |
| 4 | 충돌 반발계수 선택 | `BallPhysics.GetRestitution(SurfaceType surface)` | 표면별 튕김 정도가 달라진다. Green, Bunker, Obstacle 반발 검증 결과가 바뀐다. |
| 4 | 충돌 반발 계산 | `BallPhysics.OnCollisionEnter(Collision collision)` | `GetContact(0)`, 법선/접선 분리, 반발계수 적용이 들어 있는 핵심 함수이므로 임의 변경 시 충돌 시연 결과가 달라진다. |

## 9. 현재 미구현 또는 부분 구현 항목

| 항목 | 상태 | 비고 |
|---|---|---|
| TrajectoryPredictor | 오브젝트/LineRenderer는 있으나 완성 스크립트는 별도 확인 필요 | 궤적 예측 단계는 부분 구현 또는 미구현 |
| CameraController | Main Camera에 별도 상태 머신 스크립트 없음 | 문서 지침 대비 미구현 |
| UIManager | Canvas와 UI 패널은 있으나 전담 UIManager 스크립트는 없음 | 부분 구현 |
| ScoreManager | Hierarchy에는 존재하지만 스크립트 연결 없음 | 미구현 |
| BallCollisionDebugProbe | 테스트용 임시 스크립트로 남김 | 파일 상단에 “테스트용이기에 읽지 마세요” 주석 추가 |

## 10. 최종 결론

현재 프로젝트는 기존 Unity 6 물리 설계 문서의 핵심 제약사항인 자체 속도 관리, AddForce 미사용, useGravity 비활성화, Semi-implicit Euler 적분, 코드 기반 마찰/반발 처리, Unity 6 충돌 API 사용을 준수한다.

표면별 마찰은 기존에 `currentSurface=None`으로 유지되어 모든 표면이 동일한 마찰처럼 동작하던 문제가 있었으나, Raycast 기반 표면 판정으로 해결했다. 검증 그래프에서도 Bunker, Rough, Fairway의 감속률과 정지 시간이 서로 다르게 나타나므로 표면별 마찰 적용이 정상적으로 확인된다.

다만 기존 문서와 비교했을 때 마찰계수 값은 시연 검증을 위해 더 크게 조정되었고, 표면 판정 방식은 충돌 이벤트 단독 방식에서 Raycast 보완 방식으로 변경되었다. 또한 궤적 예측, 카메라 상태 머신, UIManager, ScoreManager 등은 문서 기준 전체 완성 항목으로 보기에는 추가 구현이 필요하다.
