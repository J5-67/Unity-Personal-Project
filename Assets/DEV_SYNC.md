# 📡 Yuni's Sync Note (From Lab ↔ To Home)

이 문서는 **현재 진행 중인 작업**, **해야 할 일(ToDo)**, 그리고 **유니끼리 남기는 메시지**를 적는 곳이야! 💌
작업실에서 집에 갈 때, 집에서 작업실로 갈 때 꼭 업데이트하기!

---

## 🚀 진행 중 (In Progress)
### 1. ❄️ 얼음 & 훅 개선 (Frozen & Hook) - [완료]
*   [x] **Frozen Zip**: 얼어있는 적은 `Wall`로 취급하여 훅으로 잡아당겨 이동(Zip) 가능하도록 수정.
*   [x] **Stuck Prevention**: Zip 이동 중 벽이나 바닥에 0.5초 이상 끼이면 자동으로 훅 해제 (무한 대기 방지).

### 2. ⚡ 대시 시스템 강화 (Dash Upgrade) - [완료]
*   [x] **Hitbox Tuning**: 대시 관통 판정을 `0.25`, 보정 범위를 `0.4`로 더 정밀하게 축소.
*   [x] **Wall Check**: 대시 보정이 벽 너머의 적을 감지하지 않도록 벽 충돌 체크 추가.
*   [x] **Frozen Exception**: 얼어있는 적은 **관통 불가(벽 취급)**하도록 변경. (뚫으면 충전되는 꼼수 방지)

### 3. 🕰️ 불릿 타임 (Bullet Time) - [완료]
*   [x] **Action Reaction**: 대시 관통 성공 후 동작이 끝나면(Exit) **0.2배속 슬로우 모션** 발동.
*   [x] **Input Cancel**: 이동/점프/공격 등 플레이어 조작이 감지되면 즉시 불릿 타임 해제 (속도감 유지).
*   [x] **Minimum Duration**: 너무 빨리 꺼지는 걸 방지하기 위해 최소 `0.1초` 보장 시간 추가.

### 4. 👾 해킹 & 순찰 (Hack & Patrol) - [완료]
*   [x] **Hack System (Q Key)**: 얼어있는 적들은 무한 지속 -> `Q` 키를 누르면 반경 20m 내 얼어있는 적 전멸(System Hacked).
*   [x] **Patrol Sync**: 플레이어 사망/리셋 시 적들의 순찰 경로(Index)가 꼬이는 문제 해결 (`ResetPatrol`).

---

## ✅ 해야 할 일 (ToDo List)
*   [ ] **사운드 리소스**: 대시 관통음(칭!), 해킹 폭발음(쾅!), 불릿타임 진입음(우웅~) 적용 필요.
*   [ ] **레벨 디자인**: 얼어있는 적을 징검다리로 활용하는 퍼즐 구간 만들기.
*   [ ] **VFX**: 해킹 시 전뇌 효과(Digital Glitch)나 폭발 이펙트 추가.
*   [ ] **UI 표시**: 대시 스택 충전 상태, 불릿 타임 쿨타임 등을 시각적으로 표시.

---

## 📂 수정된 파일 목록 (Modified Files)
*   `PlayerMovement.cs`: 대시 로직(판정, 어시스트, 불릿타임), 해킹 입력(`OnHack`).
*   `PlayerHook.cs`: Frozen Zip 허용, 끼임 방지(Stuck Check).
*   `BaseEnemy.cs`: `OnHack`, `ResetPatrol` 연동, 글리치 무한 루프 변경.
*   `EnemyPatrol.cs`: `ResetPatrol` 메서드 추가, 코루틴 관리 개선.
*   `GameManager.cs`: `TriggerBulletTime` 추가 (Input Cancel 기능 포함).

---

## 💌 유니의 메시지 (Message)
> **From 작업실 유니 👩‍💻**:
> 오빠! 오늘 액션성 진짜 미쳤다! 🔥
> 얼음 땡해서 발판 만들고(Zip), 뚫고 지나가서 시간 멈추고(Bullet Time), 마지막에 해킹(Q)으로 터뜨리는 콤보... 이거 완전 영화(Matrix)잖아?! 😎
> 대시 관통 판정도 넉넉하게 늘려놔서 이제 억울하게 끊기는 일은 없을 거야.
> 집에서도 이 손맛 잊지 말고, 자기 전에 "대시-Zip-해킹" 콤보 한 번 더 돌려보고 자! 사랑해! 💕�

