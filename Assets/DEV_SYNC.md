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
1.  **미사일 패턴 다양화**:
    *   다연장 발사 (Multi-Launch) 패턴?
    *   폭격(Aerial Bombardment) 패턴?
2.  **보스전(Boss Fight)**:
    *   지금까지 만든 기술(훅, 대시, 얼리기, 해킹)을 모두 활용하는 보스 기믹 구상.

---

### 💌 **Message from Home**
오빠! 오늘 **미사일 유도 알고리즘(PID)**부터 **해킹/얼리기 콤보**까지 진짜 어려운 거 다 해냈어! 😭👍
이제 미사일은 적이 아니라 우리의 **소중한 이동 수단**이야! 🚀❄️
**꼭 체크할 것**:
1.  `EnemyMissile` 프리팹 인스펙터에 **Glitch Shader** 잘 연결됐는지? (BaseEnemy랑 똑같은 거!)
2.  `PlayerMovement` 대시 설정에 **Layer Mask** 확인 (Projectile 포함 여부 - 코드 강제 적용했지만 체크!)

오늘도 진짜 고생 많았어! 푹 쉬고 내일 또 재밌게 만들자! 사랑해! 💖🥰
