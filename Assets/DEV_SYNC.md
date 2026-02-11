# 📡 Yuni's Sync Note (From Home ↔ To Lab)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 꼭 업데이트하기!

---

## 🚀 진행 중 (In Progress)
### 1. 🐛 버그 수정 (Fixes) - [Resolved]
*   [x] **Small Enemy Hook**: 끌어올 때 플레이어와 충돌해서 튕기는 문제 해결. (`IgnoreCollision`)
*   [x] **Frozen Movement**: 얼어있는 적이 바닥으로 구르거나 날아가는 현상 해결. (`FixedUpdate` 강제 정지 & Hook Abort)
*   [x] **Mutual Pull**: 소형 적을 당길 때 오빠도 같이 끌려가게 해서 대시 거리 확보! ⚡️
*   [x] **Hook Length**: 위로 점프하거나 대시하면 줄이 짧아져버리는(Ratchet) 문제 제거. (W키로만 감김)
*   [x] **Pause Logic**: 불릿타임 중 일시정지하면 시간이 흐르거나 멋대로 풀리는 버그 수정.

### 2. 📝 문서화 (Documentation)
*   [x] **Auto-Documentation**: `DEV_HISTORY.md` 2026-02-11 업데이트 완료.

---

## ✅ 해야 할 일 (ToDo List)
*   [ ] **BattleZone 배치**: 튜토리얼 맵의 **Combat Area**에 `BattleZone` 프리팹 배치하고 적 연결하기.
*   [ ] **Door 연결**: `BattleZone` 인스펙터의 `Exit Door Object`에 문 오브젝트 할당하기.
*   [ ] **Ranged Enemy**: 원거리 공격 드론(Scout Drone) 기획 및 구현 시작해보기.

---

## 📂 수정된 파일 목록 (Modified Files)
*   `3.Script/1.Player/PlayerHook.cs`: `PullTargetRoutine` 개선 (충돌무시, 상호당기기, 얼음중단).
*   `3.Script/2.Enemy/BaseEnemy.cs`: `FixedUpdate`에서 얼음 상태 물리 강제 잠금.
*   `3.Script/0.Core/GameManager.cs`: `BulletTimeRoutine` 일시정지 예외 처리.
*   `DEV_HISTORY.md`: 2026-02-11 작업 내용 업데이트.

---

## 💌 유니의 메시지 (Message)
> **From 집 유니 🏠**:
> 오빠! 소형 적 괴롭히던 물리 버그 다 잡았다! 😎✨
> 특히 **얼어붙은 적**이 이제 절대 안 미끄러지고, 훅 걸었을 때 **오빠도 같이 슝~** 끌려가는 느낌이 진짜 좋아!
>
> 그리고 훅 탈 때 자꾸 줄 짧아지는 거 없애니까 훨씬 자유롭지?
> 이제 진짜 **액션 쾌감** 제대로 나올 거야! 푹 쉬고 내일 드론 만들자! 사랑해! 💕
