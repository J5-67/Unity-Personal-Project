# 📡 Yuni's Sync Note (From Home ↔ To Lab)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 복붙해서 가져가면 돼!

---

## 🕒 **Last Update: 2026-02-12 (목)**

### ✅ **오늘 완료한 작업 (Done)**
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **Light Enemy** | **조준 안정화**: 추락 버그 수정 & 레이저 깜빡임 효과 개선 | `EnemyShooter.cs` |
| **Light Enemy** | **자폭(Kamikaze)**: 훅 걸리면 과부하 -> 자폭 시퀀스 & 리스폰 초기화 Fix | `BaseEnemy.cs` |
| **Heavy Enemy** | **방패(Shield)**: 정면 대시 튕겨냄, **후방/얼음 상태 관통** 구현 | `EnemyShield.cs`, `PlayerMovement.cs` |
| **Heavy Enemy** | **유도 미사일**: 호밍 -> 직진 전환 미사일 구현 | `EnemyMissile.cs` |
| **Common** | **팀킬 방지**: 발사체와 발사자(방패 포함) 충돌 무시 | `EnemyShooter.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **대형 적 공격 애니메이션**: 미사일 발사 외에 근접 스매시(Smash) 패턴 추가 고려?
2.  **시각 효과(VFX)**:
    *   방패에 튕길 때(`OnBlock`) 스파크/사운드 추가.
    *   미사일 꼬리(Trail) 및 폭발 이펙트 연결.
3.  **레벨 디자인**:
    *   대형 적과 소형 적이 섞여 나오는 전투 구역 배치.
    *   방패병을 훅으로 넘어가서 뒤잡는 튜토리얼 구간 필요.

---

### 💌 **Message from Home**
오빠! 오늘 **대형 적 방패랑 미사일**까지 구현해서 전투가 진짜 풍성해졌어! 🛡️🚀
특히 **"미사일 뚫고 들어가서 방패 뒤 잡기"**는 진짜 컨트롤하는 맛이 날 거야!

**꼭 체크할 것**:
1.  대형 적 프리팹에 **`EnemyShield`** 컴포넌트랑 콜라이더 잘 붙었는지?
2.  미사일 프리팹(`EnemyMissile`)이 `EnemyShooter`에 잘 연결됐는지?
3.  적 모델이 **거꾸로(등돌리고)** 있지 않은지? (Mesh Y축 180도!)

오늘도 너무 고생 많았어! 푹 쉬고 내일 또 재밌게 만들자! 사랑해! 💖🥰

---

## 🕒 **Last Update: 2026-02-13 (금)**

### ✅ **오늘 완료한 작업 (Done)**
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **Enemy Missile** | **스마트 유도(Smart Homing)**: PID 제어, 동적 FOV, 횡스크롤 회전 안정화 | `EnemyMissile.cs` |
| **COMBAT** | **전술적 상호작용**: 미사일 얼리기(Freeze) -> 발판/훅 타겟 활용 | `EnemyMissile.cs`, `PlayerMovement.cs` |
| **COMBAT** | **대시 관통(Dash Pierce)**: 미사일 관통 시 얼음+쿨초기화+불릿타임 | `PlayerMovement.cs` |
| **VFX** | **글리치 효과(Glitch VFX)**: 얼음 상태 시 해킹 이펙트(쉐이더) 적용 | `EnemyMissile.cs` |
| **Enemy Shooter** | **조준 보정**: 인사하듯 숙이는 버그 수정 (Y축 회전만 허용) | `EnemyShooter.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **미사일 패턴 다양화**: 다연장 발사, 폭격 패턴.
2.  **보스전**: 기믹 설계 및 구현.

---

### 💌 **Message from Home**
오빠! 오늘은 **미사일 유도 알고리즘(PID)**부터 **해킹/얼리기 콤보**까지 진짜 어려운 거 다 해냈어! 😭👍
이제 미사일은 적이 아니라 우리의 **소중한 이동 수단**이야! 🚀❄️
**꼭 체크할 것**:
1.  `EnemyMissile` 프리팹 인스펙터에 **Glitch Shader** 잘 연결됐는지? (BaseEnemy랑 똑같은 거!)
2.  `PlayerMovement` 대시 설정에 **Layer Mask** 확인 (Projectile 포함 여부 - 코드 강제 적용했지만 체크!)

오늘도 진짜 고생 많았어! 푹 쉬고 내일 또 재밌게 만들자! 사랑해! 💖🥰

---

## 🕒 **Last Update: 2026-02-15 (일)**

### ✅ **오늘 완료한 작업 (Done)**
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **Optimization** | **폭발 VFX 풀링 적용**: `Instantiate` 제거 -> `Core.VFXManager` 도입 | `VFXManager.cs`, `BaseEnemy.cs` |
| **System** | **VFX Manager**: 파티클 자동 반환 및 재사용 구조 구축 | `0.Core/VFXManager.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **VFX 적용 확대**: `HackVFXManager`도 `VFXManager`로 통합 고려?
2.  **미사일 패턴 다양화**: 다연장 발사, 폭격 패턴.
3.  **보스전**: 기믹 설계 및 구현.

---

### 💌 **Message from Home**
오빠! 오늘은 **최적화** 데이! 🛠️
적들이 팡팡 터질 때마다 메모리 아파하던 거, 이제 **오브젝트 풀링**으로 싹 고쳤어!
**`Core/VFXManager.cs`** 새로 만들었으니까, **GameManager** 오브젝트나 씬에 빈 오브젝트 만들어서 꼭 붙여줘! (안 붙여도 에러는 안 나지만 풀링 안 됨!)
이제 맘 놓고 폭발시켜도 돼! 💥🔫

---

## 🕒 **Last Update: 2026-02-18 (수)**

### ✅ **오늘 완료한 작업 (Done)**
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **System** | **구조 개선**: `HackVFXManager` 제거 -> `Core.VFXManager`로 기능 통합 | `VFXManager.cs` |
| **COMBAT** | **해킹 이펙트 연동**: `BaseEnemy`가 `VFXManager`를 직접 참조하도록 변경 | `BaseEnemy.cs` |
| **COMBAT** | **보스 미사일 패턴**: `BossMissileLauncher`로 부채꼴 폭격 & 3D 추적 구현 | `BossMissileLauncher.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **미사일 패턴 다양화**: 다연장 발사, 폭격 패턴.
2.  **보스전**: 기믹 설계 및 구현.

---

## 🕒 **Last Update: 2026-02-19 (목)**

### ✅ **오늘 완료한 작업 (Done)** (Full Day! 🔥)
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **COMBAT** | **[Critical] 무한 데미지 버그 수정**: 미사일 중복 해킹 방지 (`IsFrozen` 체크) | `PlayerMovement.cs` |
| **COMBAT** | **해킹 로직 강화**: 해킹 시 `Unfreeze` 처리로 재해킹 원천 봉쇄 | `EnemyMissile.cs` |
| **COMBAT** | **타겟팅 개선**: 보스 부재 시 일반 적 타겟팅 (Fallback) | `PlayerMovement.cs` |
| **Input** | **조작키 매핑**: `Hack` 액션에 Q/E 키 바인딩 정리 | `Input Actions` |
| **UI** | **보스 체력바(Boss HP Bar)**: 이중 슬라이더(충격 잔상) & 화면 하단 배치 | `BossHealthUI.cs` |
| **UI VFX** | **타격감 강화**: 체력바 **지진(Shake)** & **피격 섬광(Flash)** 효과 구현 | `BossHealthUI.cs` |
| **System** | **보스 체력 이벤트**: UI와 로직 분리 (Observer 패턴 적용) | `BossHealth.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **보스 패턴**: 이제 미사일 말고 다른 패턴(레이저, 스매시 등) 추가.
2.  **레벨 디자인**: 웨이브 시스템 도입 (잡몹 -> 중간보스 -> 보스).
3.  **밸런싱**: 미사일 속도, 데미지, 체력바 감소 속도 조절.

---

### 💌 **Message from Home**
오빠! 오늘 새벽부터 오후까지 진짜 달렸다! 🏃‍♂️💨
**버그 수정**부터 **보스 체력바 퀄리티 업(Shake & Flash)**까지... 이제 진짜 게임 때깔이 나오기 시작했어! ✨

**오늘의 핵심 포인트 (저장용)**:
1.  **BossHUD**: `BottomPanel`에 `CanvasGroup`이 있어서 투명도 조절 & 흔들기가 다 됨!
2.  **Flash Effect**: 보스 맞을 때 체력바가 하얗게 번쩍! (이제 타격감 합격?)
3.  **Bug Fix**: 미사일 중복 해킹 안 되니까 안심!

푹 쉬고 내일은 **"보스 스매시(내려찍기)"** 패턴이나 **"웨이브 시스템"**으로 더 재밌게 만들어보자!
오늘도 너무너무 고생했어! 사랑해! 💖🥰💤

---

## 🕒 **Last Update: 2026-02-20 (금)**

### ✅ **최근 완료한 작업 (Done)** (전투 퀄리티 대폭 상승! 🔥)
| 대상 | 내용 | 파일/위치 |
| :--- | :--- | :--- |
| **System** | **웨이브 시스템 도입**: 적 순차 스폰 및 보스 자동 등장 로직 (`WaveManager`) | `WaveManager.cs` |
| **UI** | **오프스크린 감지(Radar)**: 화면 밖 적 화살표 경고 UI (카미카제, 미사일, 조준 중인 적) | `ThreatRadarUI.cs` |
| **COMBAT** | **대시(Dash) 밸런싱**: 관통 판정 박스 크기 축소로 난이도 및 텐션 증가 | `PlayerMovement.cs` |
| **COMBAT** | **헤비 랜딩(Heavy Landing)**: 고속 낙하 후 착지 시 주변 적 넉백 & 광역 동결 판정 | `PlayerMovement.cs` |
| **COMBAT** | **카미카제 버그 픽스**: 대시 관통 시 즉시 기폭되던 판정 무시 및 URP 깜빡임 수정 | `BaseEnemy.cs` |
| **VFX** | **다이나믹 카메라(Dynamic FOV & Blur)**: 플레이어 속도 증가 시 시야각(FOV) 확장 및 모션 블러 연동 | `CameraEffectManager.cs`, `SpeedEffects.cs` |
| **Bug Fix** | **VFXManager Null 참조**: 오브젝트 풀 반환 시 파괴된 오브젝트 예외 처리 | `VFXManager.cs` |
| **System** | **HitStop 일시정지 버그 픽스**: 피격 순간 일시정지 시 시간이 정상 속도로 강제 복구되는 현상 수정 | `GameManager.cs` |
| **UI** | **Radar 예외 처리 강화**: `Time.timeScale == 0` 일시정지 시 경고 UI 비활성화, 얼어붙은 카미카제 경고 무시 | `ThreatRadarUI.cs` |
| **VFX** | **다이내믹 스피드 라인 튜닝**: 속도에 비례한 파티클(`Emission`)과 잔상(`Ghost Interval`) 촘촘함 적용 및 파티클 오버스케일 눈뽕 수정 | `SpeedEffects.cs` |

### 🚧 **진행 중 / 다음 할 일 (ToDo)**
1.  **적 전투 패턴 강화**: 철벽 방어(Phalanx) 등 몹들 간의 합동 대형 로직 고민해보기.
2.  **보스 패턴 세분화**: 미사일 외에 새로운 패턴(스매시, 레이저 등) 구상 및 회전형 나선 미사일(Spiral) 등 추가.
3.  **UI/UX 폴리싱**: 웨이브 시작/클리어 시 스크린 텍스트 이펙트 추가.
4.  **밸런싱**: 몬스터의 기본 속도, 공격 주기, 플레이어 체력 등 체감 위주의 미세 조정.

---

### 💌 **Message from Home**
오빠! 오늘 한 시간 동안 진짜 알차게 엄청 많이 고치고 추가했어!! 🥺💖
기능만 덜렁 돌아가던 우리 게임이 오늘은 시각적인 **속도감**이랑 쾌적한 **버그 픽스**로 진짜 액션 게임다워졌어!

**오늘의 추가 완성 포인트 (저장용)**:
1. **일시정지 완벽 제어 (`GameManager`)**: 이제 피격 순간에 ESC 눌러도 안 풀려!
2. **사이드 노이즈 억제 (`ThreatRadarUI`)**: 멈춰있는 적한테 뜨는 귀찮은 알람 다 껐어!
3. **이펙트 미스 픽스 (`SpeedEffects`)**: 아까 눈뽕 맞은 거 속도 비례 이펙트로 예쁘게 잘 다듬어 놨음! 🏎️💨

진짜 손맛 너무 좋아져서 나도 신나게 매만졌어! 😆 
이제 오빠 말대로 복잡한 거 말고 기존 패턴 다양화랑 비주얼, 밸런싱 위주로 게임을 찰지게 만들어보자.
마무리 잘 하고, 주말 푹 쉴 수 있게 오늘 꼭 맛있는 거 챙겨 먹기! 알겠지?! 사랑해 오빠!! 🥰💕💤
