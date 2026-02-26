# 📔 Yuni & Oppa's Development History

이 문서는 프로젝트의 모든 **개발 기록**을 날짜별로 누적해서 정리하는 곳이야! 📜✍️

---

## 📅 2026-01-30
### 1. 💬 대화 시스템 (Dialogue System) - [완료]
*   **CSV 연동**: 엑셀로 대본을 관리하고 유니티로 불러오는 기능 완성!
*   **타자기 효과 (TypewriterEffect)**:
    *   한 글자씩 써지는 연출 구현.
    *   **사운드 추가**: 글자가 써질 때마다 `Blip` 소리 재생 (빈도, 피치 조절 가능).
    *   **화자별 목소리**: `DialogueTester`의 `PortraitInfo`에 `AudioClip`을 추가해서, 캐릭터마다 다른 타자 소리를 낼 수 있게 업그레이드! 🎤
*   **버그 수정**: `TypewriterEffect`에서 첫 글자가 무시되거나 소리가 안 나는 문제 해결 (로직 개선).

### 2. 🖥️ UI 작업 (SlimUI) - [진행 중]
*   **SlimUI 에셋 도입**: 모던한 메뉴 UI 적용.
*   **New Input System 마이그레이션**:
    *   Legacy `Input.GetKeyDown` 코드를 `UnityEngine.InputSystem`의 `Keyboard.current`로 전면 교체 완료! 🛠️
*   **트러블슈팅**:
    *   **드롭다운 짤림 문제**: `Dropdown` -> `Template` -> `Viewport`의 앵커 및 크기 설정 문제 확인.

---

## 📅 2026-01-31
### 1. 🖥️ UI - 메인 메뉴 & 설정 (Settings) - [완료]
*   **메인 메뉴 구조화**:
    *   `MainMenuController` 개선: Play/Settings 버튼과 서브 메뉴(Start/Settings) 연결.
    *   **New Game**: '1.GameTest' 씬 로드 기능 구현.
*   **설정(Settings) 시스템 구축**:
    *   `SettingsMenuController` 제작.
    *   **설정 저장**: `PlayerPrefs`를 활용해 Mouse Sensitivity, Audio Volume, Graphic 설정 저장/로드.
    *   **양방향 동기화**: Slider ↔ InputField 값 연동 (입력 시 Placeholder 업데이트 방식 적용).
    *   **그래픽 설정**: FullScreen / Windowed 모드 전환 Dropdown 구현.

### 2. ⏯️ 인게임 UI (Pause Menu) - [완료]
*   **GameManager & PauseUI**:
    *   **일시정지**: `ESC` 키로 `Time.timeScale` 조절 및 메뉴 호출.
    *   **Singleton & Input**: `GameManager`를 싱글톤으로 구성하고, New Input System의 `Pause` 액션 연결.
    *   **버그 수정**: 씬 전환 시 `GameManager` 중복 파괴 과정에서 `NullReference` 발생하는 **초기화 순서 문제 해결** (OnEnable/Disable null 체크).

### 3. 🤐 대화 중 행동 제어 (Dialogue Input Blocking) - [완료]
*   **GameManager**: `IsDialogueActive` 상태 관리 추가.
*   **DialogueUI**: 대화창 `Show`/`Hide` 시 `GameManager`에 상태 전달.
*   **PlayerMovement**:
    *   `IsDialogueActive` 상태일 때 **이동, 점프, 대시** 입력 완전 무시.
    *   물리 연산에서도 이동 벡터를 0으로 강제하여 미끄러짐 방지.

### 4. 🧶 훅 & 조준선 비주얼 개선 (Hook Visuals) - [완료]
*   **타겟별 시각적 구별**:
    *   **벽/허공/얼음**: 정적인 점선 (`-`, Dash) + 느린 흐름.
    *   **Light Enemy (당겨옴)**: 플레이어 쪽으로 흐르는 초록색 역방향 화살표 (`<`, Pull).
    *   **Heavy Enemy (날아감)**: 적 쪽으로 빠르게 흐르는 빨간색 정방향 화살표 (`>`, Zip).
*   **애니메이션 & 밀도 최적화**:
    *   **Stretch 모드 도입**: 거리에 상관없이 일정한 무늬 간격을 유지하도록 `LineTextureMode.Stretch`와 스크립트 기반 Tiling 계산 적용.
    *   **밀도 분리 제어**: 점선(`Dash Tiling`)과 화살표(`Arrow Tiling`)의 밀도를 각각 조절 가능하도록 변수 분리.
    *   **쉐이더 교체**: `Legacy Shaders/Particles/Alpha Blended`를 사용하여 색상(`TintColor`)과 UV 애니메이션 모두 지원.

---

## 📅 2026-02-01
### 1. 🎧 오디오 시스템 (Audio System) - [완료]
*   **AudioManager (Singleton)**:
    *   **마스터/BGM/SFX 볼륨 관리**: `PlayerPrefs` 저장값과 연동하여 볼륨 개별 조절.
    *   **싱글톤 패턴**: 씬 전환 시에도 파괴되지 않고 배경음 유지.
*   **SceneAudioController**:
    *   **씬별 BGM 자동 재생**: 각 씬에 배치되어 `Start()`에서 해당 씬의 BGM 재생 요청.
    *   **구조 분리**: `AudioManager`(영구적)와 `SceneAudioController`(씬별)를 분리하여 초기화 충돌 및 사운드 끊김 문제 해결.
*   **Settings UI 연동**:
    *   `SettingsMenuController` 슬라이더 조정 시 `AudioManager`를 통해 실시간 볼륨 반영.
*   **플레이어 SFX**:
    *   훅 발사 시(`PlayerHook`) `AudioManager`를 통해 효과음 재생 구현.

---

## 📅 2026-02-02
### 1. ❤️ 플레이어 체력 & 부활 (Player Health & Respawn) - [완료]
*   **PlayerHealth.cs**:
    *   **체력 시스템**: 최대 3칸, 피격 시 1칸 감소. 0이 되면 사망.
    *   **체크포인트(Checkpoint)**: 깃발(Trigger)을 지나면 해당 위치 저장.
    *   **데드존(DeadZone)**: 구덩이에 빠지면 데미지를 입고 마지막 체크포인트로 복귀.
*   **적 부활 시스템 (Enemy Respawn)**:
    *   **이벤트 패턴**: `GameManager.OnPlayerRespawn` 이벤트를 통해 플레이어 부활 시 적들도 `SetActive(true)`로 부활 및 초기화! 🧟‍♂️

### 2. 🥊 전투 타격감 폴리싱 (Combat Polish) - [완료]
*   **히트 스탑 (Hit Stop)**: 피격 순간 `Time.timeScale`을 잠시 멈춰서 "억!" 하는 느낌 구현.
*   **카메라 쉐이크 (Camera Shake)**: **Cinemachine Impulse**를 활용해 피격 시 화면 지진 효과 구현! 📸🌋
    *   (Legacy Noise Profile + 9999 Radius 설정으로 확실한 타격감 확보)
*   **버그 수정**:
    *   **잔여 운동량 제거**: 부활 직후 튕겨나가는 문제 해결 (Velocity = 0).
    *   **훅 회수**: 죽었을 때 훅이 연결된 상태면 고무줄처럼 튕기는 문제 해결 (`StopHook` 호출).
### 3. ❤️ 체력 UI & 무적 시스템 (Health UI & Invincibility) - [완료]
*   **HealthUI System**:
    *   **하트 UI**: 평소엔 숨겨져 있다가(Alpha 0), 피격 시 등장(Alpha 1) 후 3초 뒤 서서히 사라지는 페이드 효과 구현.
    *   **스프라이트 교체**: 체력 상태(1~4칸)에 따라 하트 이미지 실시간 교체 (인덱스 계산 로직 최적화).
*   **무적 시스템 (Invincibility)**:
    *   **무적 판정**: 피격 시 1초간 `_isInvincible` 상태로 진입하여 추가 데미지 무시.
    *   **깜빡임 효과 (Blinking)**: `SpriteRenderer`를 0.1초 간격으로 껐다 켰다 하여 시각적 피드백 제공.
    *   **안전 장치**: 사망하거나 무트가 끝날 때 렌더러가 꺼진 채로 남지 않도록 강제 활성화 처리.

### 4. 🌄 배경 연출 (Parallax Background) - [완료]
*   **ParallaxEffect.cs**: 카메라 이동에 따라 배경이 다른 속도로 움직이는 원근감 효과 구현.
*   **Infinite Scrolling**: 배경이 끊기지 않고 무한 반복되도록 위치 리셋 로직 추가.
*   **축 대응 (Axis Handling)**: 카메라 회전(Y=90)에 맞춰 Z축을 가로 이동으로 인식하도록 커스텀.

---

## 📅 2026-02-03
### 1. ⏩ UI/UX 개선 (Skip & Cursor) - [완료]
*   **대화 스킵 (Dialogue Skip)**: 대화 중 `ESC` 키 입력 시 `OnSkipDialogue` 이벤트 발동 -> 대화창 즉시 종료.
*   **가상 커서 최적화**:
    *   **커서 숨김/잠금 로직 개선**: 대화가 끝나거나 스킵될 때, `GameManager`에서 확실하게 커서를 숨기고 잠그도록(`Locked`) 수정 (에디터/빌드 동작 차이 보완).
    *   **입력 예외 처리**: `PlayerInput`이 없을 때도 커서 잠금 코드가 실행되도록 안전장치 추가.

### 2. 🧩 퍼즐 & 상호작용 (Switch & Door) - [완료]
*   **시스템 구조화**: `IInteractable` 인터페이스를 도입하여 훅, 총알 등 다양한 수단으로 상호작용 가능하도록 설계.
*   **Door & Switch**:
    *   **Switch**: 타격 시 문(`Door`)에 신호를 보내 열거나 닫음 (`Toggle` 옵션). 3D `MeshRenderer`와 2D `SpriteRenderer` 모두 지원하도록 확장.
    *   **Door**: `Coroutine`을 사용해 부드럽게 열리고 닫히는 애니메이션 구현. `OnDrawGizmos`로 에디터에서 이동 경로 미리보기 기능 추가.
*   **PlayerHook 연동**: 훅이 물체에 닿았을 때 `IInteractable` 컴포넌트가 있으면 즉시 상호작용하고 회수되도록 로직 추가.

### 3. 🩸 함정 시스템 (Trap System) - [완료]
*   **TrapBase**: 플레이어와 충돌(`OnTriggerEnter`) 시 데미지 및 넉백을 주는 기본 함정 클래스 작성.
*   **함정 종류**:
    *   **Spike (가시)**: 고정형 함정.
    *   **Moving Saw (회전 톱날)**: `Mathf.PingPong`을 활용해 설정된 경로를 왕복 이동하며 회전하는 동적 위협 요소 구현.

### 4. 💾 저장 시스템 (Save System) - [완료]
*   **JSON 데이터 저장**:
    *   `SaveSystem`: `Application.dataPath` 기반으로 `SaveData/save.json` 파일 생성 및 읽기/쓰기 구현.
    *   **자동 저장**: 포탈 이동 시(`Portal.cs`) 현재 도착한 씬 이름(`currentStageSceneName`)을 저장.
*   **이어하기 (Continue)**:
    *   메인 메뉴의 `Continue` 버튼을 누르면 마지막으로 저장된 씬을 불러와서 게임 재개.
    *   씬 이름 기반 저장 방식으로 변경하여 유연한 스테이지/레벨 관리 지원.
    
### 5. ⚡ 대시 개선 (Dash Pierce) - [완료]
*   **적 관통 보정 (Dash Penetration)**:
    *   대시 거리가 살짝 부족해서 적 내부에 멈추는 경우, 피격 판정을 받고 튕겨나가는 문제 해결.
    *   **Overlap Check**: 대시 종료 시점에 적과 겹쳐있다면(Collision Check), 최대 0.5초까지 대시 상태(무적+고속이동)를 유지하며 관통.
    *   안전 장치(`maxExtensionTime`)를 두어 무한 루프 방지.
    *   **Frozen Exception**: 얼어있는 적(`IsFrozen`)은 벽(Wall)으로 취급되어 관통(Penetration), 스택 충전, 불릿 타임 발동 대상에서 제외됨.
    *   **Dash Assist (Magnet)**: 대시 끝부분에서 적이 아슬아슬하게 닿지 않을 경우(약 2m), 자동으로 대시를 연장하여 적중시키는 보정 기능 추가. (플레이어 편의성 강화)
    *   **Hitbox Tuning**: 관통 판정 범위를 0.8 -> 0.6으로 재조정. (0.8은 너무 넓어서 스치기만 해도 어는 문제 발생 -> 최적화)
*   **관통 보상 (Dash Reset)**:
    *   적을 관통하면 대시 스택 1회 즉시 충전 (Genji Style).
    *   1번의 대시 동작에서 중복 충전되지 않도록 1회 제한 (`hasRecharged`).
    
### 6. ❄️ 훅 & 프로즌 (Frozen Zip) - [완료]
*   **Frozen Zip 개선**:
    *   기존에는 얼어있는 적에게 훅을 걸어도 '적(Enemy)'으로 인식되어 점프 키를 이용한 'Zip(당겨지기)'이 불가능했음.
    *   `PullSelfRoutine`에서 타겟이 적(`BaseEnemy`)이라도 **얼어있는 상태(`IsFrozen`)라면 Zip이 가능하도록 예외 처리** 추가.
    *   기존에는 얼어있는 적에게 훅을 걸어도 '적(Enemy)'으로 인식되어 점프 키를 이용한 'Zip(당겨지기)'이 불가능했음.
    *   `PullSelfRoutine`에서 타겟이 적(`BaseEnemy`)이라도 **얼어있는 상태(`IsFrozen`)라면 Zip이 가능하도록 예외 처리** 추가.
    *   이제 얼어있는 적을 징검다리 삼아 빠르게 이동 가능! 🚀

### 7. 🕰️ 불릿 타임 (Bullet Time) - [완료]
*   **대시 관통 효과**:
    *   대시로 적을 관통하여 스택을 비축하는 순간, **일시적으로 시간이 느려지는 불릿 타임(Bullet Time)** 효과 발동.
    *   **Timing Fix**: **관통 즉시 발동**되도록 타이밍 조절. (타격감 UP)
    *   **Cancel On Input**: 불릿 타임 중 플레이어가 이동/점프/공격 등 **새로운 조작을 시작하면 즉시 해제**되어 속도감 유지. 
        *   **Hotfix**: 대시(우클릭) 등 키를 계속 꾹 누르고 있을 때 바로 불릿 타임이 해제되던 현상을 `IsPressed()` -> `WasPressedThisFrame()`으로 수정하여 **현재 누르고 있는 버튼은 무시하고 새로 눌렀을 때만 해제**되도록 예외 처리 완벽 도입.
    *   **Settings**: `dashBulletTimeScale`(0.2배속), `dashBulletTimeDuration`(0.5초) 등 Inspector에서 조절 가능.
    *   이제 연속 대시할 때 다음 타겟을 노리기 훨씬 쉬워짐! (스타일리쉬 액션 UP!)

### 8. 👾 해킹 시스템 (Hack System) - [완료]
*   **Hack Mechanic (Q Key)**:
    *   **Change**: 적이 얼어있는 상태(`Frozen`)가 되면 더 이상 자동으로 파괴되지 않고 **무한히 유지**됨.
    *   **Execution**: 플레이어가 `Hack` 키(Q)를 누르면 주변(20m)의 모든 얼어있는 적들이 일제히 **해킹되어 파괴(비활성화)**됨.
    *   **Tactical Use**: 적들을 얼려서 발판으로 쓰다가, 필요할 때 일망타진하는 전략적 플레이 가능! 💥

### 9. 🤖 순찰 동기화 (Patrol Sync Fix) - [완료]
*   **Patrol Desync Issue**: 훅으로 적을 끌고 다니다가 플레이어가 죽어서 리셋될 때, 적은 위치만 복구되고 순찰 경로(Index)가 꼬여서 엉뚱한 곳으로 이동하는 문제 발생.
*   **ResetPatrol**: `ResetEnemy` 호출 시 `EnemyPatrol.ResetPatrol()`을 수행하여 **순찰 인덱스와 대기 상태를 초기화(0번 지점부터 다시 시작)**하도록 수정.

### 10. 🧱 Zip 끼임 방지 (Stuck Prevention) - [완료]
*   **Issue**: 벽 너머의 타겟이나 플랫폼 아래에서 Zip을 시도하면, 벽에 막혀서 이동하지 못하고 줄을 끊지 않으면 움직일 수 없는 상태가 됨.
*   **Stuck Detection**: Zip 동작 중 0.5초 동안 플레이어의 이동 거리가 0.01m 미만일 경우, **어딘가에 끼인 것으로 판단하여 자동으로 훅을 해제**하도록 수정. (쿨하게 놓아줌)

### 11. 🛡️ 대시 판정 정밀 교정 (Dash Precision Tweak) - [완료]
*   **Issue**: 대시 관통 판정과 보정(자석) 범위가 너무 넓어서 적과 충돌하지 않았는데도 시간이 느려지거나(Bullet Time), 벽 너머의 적 때문에 제자리 정지 현상 발생.
*   **Radii Tuning**:
    - **Penetration**: `0.4f` -> `0.25f` (확실히 겹쳐야 관통/동결).
    - **Assist**: `0.8f` -> `0.4f` (보정 범위 축소).
    - **Distance**: `2.0f` -> `1.5f` (자석 유효 거리 단축).
*   **Wall Exclusion**: 대시 보정 시 벽(`wallLayer`) 체크를 추가하여, 벽 뒤에 있는 적 때문에 대시가 불필요하게 연장되는 문제 해결.

---

## 📅 2026-02-08
### 1. 💥 해킹 비주얼 강화 (Hack VFX) - [완료]
*   **HackVFXManager**:
    *   해킹 이펙트(`ParticleSystem`)를 전담 관리하는 **싱글톤 & 오브젝트 풀링** 시스템 구축.
    *   **Object Pooling**: 파티클 생성/파괴 비용 절감을 위해 풀링 적용 (`poolSize`: 10).
*   **전뇌 폭발 (Digital Explosion)**:
    *   `BaseEnemy`: `OnHack` 시 `PlayHackEffect`를 호출하여 적 위치에서 파티클 폭발.
    *   **Screen Shake**: 해킹 성공 시 `CameraShake` 강도를 높여(`2.0f`) 강력한 타격감 전달.
    
### 2. ✂️ 훅 물리 안정화 (Rope Break Check) - [완료]
*   **Occlusion Check 도입**: 훅이 연결된 상태(`PullSelfRoutine`)에서 줄이 벽이나 바닥을 뚫고 지나가는 경우(Linecast Hit), 즉시 훅을 해제하도록 수정.
*   **Anti-Clipping**: 이를 통해 플레이어가 천장이나 바닥을 뚫고 텔레포트하는 물리 버그(Tunneling)를 원천 차단.

### 3. 🧗‍♀️ 훅 액션 심화 (Hook Physics V2) - [완료]
*   **줄 길이 동기화**:
    *   **W Key (Winch Up)**: `climbSpeed` 속도로 줄을 감아 올리면서, `MovePosition`을 통해 단단하게(0.01f 오차) 플레이어를 당김.
    *   **S Key (Winch Down, The Elevator)**: `climbSpeed` 속도로 줄을 풀면서, **속도 제한(Velocity Limit)** 방식을 적용하여 뚝뚝 끊김(Stuttering) 없이 아주 부드럽게 하강.
    *   **Ratchet System**: 평소(입력 없음)에는 줄이 늘어나는 것을 강제로 막고, 줄보다 안쪽으로 들어오면 자동으로 줄 길이를 단축시켜 항상 팽팽함 유지.
*   **물리 엔진 최적화**:
    *   **Velocity Correction**: 줄 밖으로 나가는 속도 성분만 정밀하게 제거하여, 그네 탈 때 줄이 늘어나는 느낌을 완벽 차단.
    *   **Hybrid Constraint**: 상황(W/S/Idle)에 따라 '위치 강제(Position)'와 '속도 제한(Velocity)' 방식을 유연하게 스위칭하여 최상의 조작감(Hand Feel) 확보.

---

## 📅 2026-02-09
### 1. 🪝 훅(Winch) 메카니즘 고도화
*   **Winch Down (S Key) 안정화**:
    *   최대 길이(`maxDistance`) 도달 시 **덜덜거림(Jittering)** 및 **무한 추락** 버그 수정.
    *   줄 끝에서는 **Rope Solver**가 엄격하게 작동하도록 하여 **늘어짐(Rubber Banding)** 방지.
*   **Winch Down 속도 밸런싱**:
    *   **문제**: `S` 키 하강 시 중력 가속도가 더해져 너무 빨라짐.
    *   **해결 시도 1 (Rollback)**: `Drag`를 높였으나 스윙 속도까지 느려져 폐기.
    *   **해결 시도 2 (Current)**: `Rope Solver` 내부에서 하강 속도(`limitSpeed`)를 `climbSpeed * 0.5`로 제한.
    *   **[TODO]**: 여전히 중력과 윈치 속도 간의 자연스러운 느낌 조율 필요. `PlayerHook.cs`의 `climbSpeed * 0.5f` 계수 튜닝 요망.

### 2. 🎨 비주얼 폴리싱 & 레벨 디자인 툴 (Visual Polish & Tools)
*   **Dynamic Camera (Look Ahead)**:
    *   **Aim Shift**: 기존의 속도 기반 시야 이동(Dynamic LookAhead)이 덜컹거리는 문제 해결을 위해, **마우스 커서(조준) 방향**으로 부드럽게 이동하는 방식으로 변경.
    *   **Follow Offset Control**: `CinemachineFollow` 컴포넌트의 오프셋 값을 직접 제어하여, 조준 방향으로 미리 시야를 확보해줌. (훅 조준이 훨씬 편해짐!)
*   **Speed Effects (Juice)**:
    *   **Ghost Trail**: 고속 이동 시 플레이어 뒤에 잔상이 남도록 자동 활성화.
    *   **Speed Line (집중선)**: Main Camera 하위에 `Stretched Billboard` 파티클을 Local Space로 배치하여, 만화처럼 화면이 빨려 들어가는 연출 구현.
*   **Level Builder Tool**:
    *   **Grid Snapper**: `[ExecuteInEditMode]`를 활용하여, 에디터에서 오브젝트 이동 시 **1m 그리드**에 자동으로 딱딱 붙도록 스냅 기능 구현. (레벨 디자인 속도 3배 증가 예상 🚀)

### 3. 🗺️ 튜토리얼 맵 설계 (Level Design)
*   **Concept**: "Training Ground 01" - 어두운 사이버펑크 폐허.
*   **Key Sections**:
    1.  **Basic Move**: 점프 & 벽타기.
    2.  **Hook Gap**: 천장 훅 걸고 건너기.
    3.  **Winch Tunnel (핵심)**: 좁은 수직 통로에 가시(Spike) 배치. 훅을 걸고 **S키(Winch Down)**로 천천히 하강하며 장애물 피하기 학습.
    4.  **Swing**: 연속 스윙.
    5.  **Combat Area**: 대시(Freeze) & 해킹(Hack) 콤보 연습.
*   **Note**: 스파이크(Spike)는 충돌 시 플레이어를 밀어내는(Knockback) 방향이 충돌 지점의 법선 벡터(Normal)를 따르도록 개선됨.

---

## 📅 2026-02-10
### 1. ⚔️ 전투 및 레벨 시스템 고도화 (Combat & Level Polish)
*   **Room Clear Mechanics (BattleZone)**:
    *   **Door Integration**: 적 전멸 시 방문이 자동으로 열리는 시스템 구현 (`Interaction.Door` 스크립트 연동).
    *   **Inspector Reference Fix**: `Door` 스크립트 연결이 Play Mode에서 끊기는 문제 해결을 위해 `GameObject` 참조 방식으로 변경 및 `Start`에서 컴포넌트 캐싱.
    *   **Logic Optimization**: `OnDeath` 이벤트를 활용하여 매 프레임 검사 없이 효율적으로 클리어 체크.
*   **Trap Logic Fix**:
    *   **Spike Knockback**: 가시(Spike)가 플레이어를 밀어낼 때, 항상 위(Up)가 아니라 **충돌 면의 반대 방향**으로 정확하게 밀어내도록 `Collider.ClosestPoint` 로직 적용.

### 2. 🎯 조준 및 훅 메카니즘 개선 (Aim Assist & Homing)
*   **Homing Hook (유도 훅)**:
    *   **Smart Targeting**: 조준선(PlayerAim)이 적을 인식했을 때, 훅 발사 시 마우스 위치가 아니라 **해당 적의 중심**으로 유도되어 발사되도록 개선. (빗나감 방지)
    *   **Static Object Filtering**: 벽이나 바닥 같은 거대한 정적 오브젝트는 유도(Lock-on) 대상에서 제외하여, 훅이 의도치 않게 벽의 중심점(Pivot)으로 날아가는 현상 수정.

### 3. 💬 다이얼로그 가독성 (Dialogue UI)
*   **Line Break**: 입력 데이터의 `\n` 이스케이프 문자를 실제 줄바꿈으로 파싱하는 로직 추가 (`String.Replace`).

---

## 📅 2026-02-11
### 1. 🪝 소형 적 상호작용 개선 (Small Enemy Hook Fixes)
*   **Physics Explosion Fix**:
    *   **Collision Ignore**: 적을 끌어올 때(`PullTargetRoutine`) 플레이어와 충돌하여 서로 튕겨나가는 현상을 방지하기 위해, 당기는 동안 `Physics.IgnoreCollision` 적용.
*   **Frozen Stability (얼음 상태 고정)**:
    *   **Double Lock**: 적이 얼었을 때(`IsFrozen`) 훅이 계속 당기거나 물리 엔진의 잔여 힘으로 인해 바닥으로 구르거나 날아가는 버그 수정.
    *   **Strict KinematicCheck**: `BaseEnemy`의 `FixedUpdate`에서 얼음 상태면 매 프레임 속도를 0으로 초기화하고 물리(Kinematic)를 강제로 잠금.
    *   **Hook Abort**: 적이 끌려오는 도중 얼어버리면 즉시 훅을 중단(`StopHook`)하여 물리 충돌 방지.
*   **Mutual Pull (상호 당기기)**:
    *   **Approach Logic**: 소형 적을 당길 때 플레이어도 15f 속도로 적을 향해 살짝 끌려가도록 수정.
    *   **Effect**: 이로 인해 대시 관통(Dash Penetration) 거리가 부족해도 쉽게 적을 뚫고 지나갈 수 있게 됨 (타격감 & 조작감 향상). ⚡️

### 2. 🐛 버그 수정 및 최적화 (Bug Fix & Polish)
*   **Hook Ratchet Removed**:
    *   **Problem**: 천장에 매달린 채 위로 대시하거나 넉백당하면 줄 길이가 자동으로 짧아져(Ratchet), 다시 내려오지 못하고 턱 걸리는 문제.
    *   **Solution**: 자동 감기 로직을 삭제하고, 오직 **W키(줄 감기)**를 누를 때만 줄이 짧아지도록 변경. 이제 자유롭게 튀어 올랐다 내려올 수 있음.
*   **Bullet Time + Pause**:
    *   **TimeScale Logic**: 일시정지(`Pause`) 상태에서도 불릿타임 타이머가 흘러가거나, 불릿타임 종료 시 강제로 `TimeScale=1`로 만들어 일시정지가 풀리는 치명적 버그 수정.


---

## 📅 2026-02-12
### 1. 🚀 적 패턴 고도화 (Advanced Enemy Patterns) - [완료]
*   **소형 적 (Light Enemy) - 레이저 사수 (Shooter)**:
    *   **Aim Stability**: 조준 중 추락하는 물리 버그 해결 (`Kinematic` forced).
    *   **Visual Cue (Blinking)**: 조준 레이저가 발사 직전에 점멸 속도가 빨라지도록 개선 (긴장감 조성).
    *   **Hive Mind Fix**: 모든 적이 동시에 사격하는 현상 방지를 위해 시작 딜레이 랜덤화.
    *   **Kamikaze (자폭)**: 훅에 걸린 적이 일정 시간 후 플레이어를 추적하며 자폭하는 패턴 추가.
*   **대형 적 (Heavy Enemy) - 방패병 (Shield & Missile)**:
    *   **Reflector Shield**: 정면 대시를 튕겨내는(Bounce) 방패 시스템 구현. 배후 공격이나 얼음 상태일 때는 관통 가능.
    *   **Homing Missile**: 초반 유도 -> 후반 직진하는 스마트 미사일 패턴 구현.
    *   **Self-Hit Fix**: 미사일이 발사자나 방패에 충돌하지 않도록 예외 처리.
    *   **Model Fix**: 적 모델의 메쉬 방향(Y축 180도) 수정 가이드.

---

## 📅 2026-02-13
### 1. 🚀 스마트 유도 미사일 (Smart Homing Missile) - [완료]
*   **PID 제어기 도입**:
    *   `EnemyMissile.cs`에 비례-적분-미분(PID) 회전 로직 적용하여 부드러운 유도 구현. 🎯
*   **횡스크롤 회전 안정화**:
    *   `Vector3.Cross`를 이용한 회전 방향(부호) 판별 로직 추가.
    *   Y축(Yaw)과 Z축(Roll) 회전을 제어하여 2.5D 환경에서 꼬임 없는 비행 궤적 완성.
    *   후방 발사 시 미사일이 강제로 전방을 보는 버그 수정 (Y축 회전 유지).
*   **동적 추적 각도(FOV) 시스템**:
    *   시간이 지날수록 추적 가능 각도가 좁아짐 (Wide -> Narrow).
    *   플레이어가 회피에 성공하면 미사일이 유도를 포기하고 직진하도록 밸런싱.

### 2. ⚡ 전술적 상호작용 (Combat Tactics) - [완료]
*   **미사일 정지(Freeze) 기능**:
    *   플레이어의 해킹/대시 공격 시 미사일이 공중에 정지. ❄️
    *   정지된 미사일은 `Layer: Wall`, `Tag: Wall`로 변경되어 훅(Grappling Hook) 사용 가능 (발판화).
    *   정지 상태에서는 자동 파괴(Time Destroy)가 취소되어 영구 유지됨.
*   **대시 관통(Dash Pierce)**:
    *   `PlayerMovement`에서 대시 중 `Projectile` 레이어 충돌 무시 로직 강화.
    *   대시로 미사일을 관통하면 미사일이 얼고 대시 쿨타임이 즉시 충전됨. 🏃‍♂️💨

### 3. 👾 시각 효과 (Visuals) - [완료]
*   **EnemyShooter 조준 개선**:
    *   적이 플레이어 조준 시 불필요한 X축 회전(인사하듯 숙임) 제거하고 Y축(수평) 회전만 허용.
*   **해킹/글리치 VFX**:
    *   미사일이 얼었을 때 `BaseEnemy`와 동일한 `Glitch Shader` 효과 적용. ✨
    *   머티리얼 교체 및 쉐이더 프로퍼티 애니메이션 코루틴 구현.

---

## 📅 2026-02-15
### 1. 🛠️ 최적화 시스템 구축 (Optimization System) - [완료]
*   **VFX Manager (Object Pooling)**:
    *   **문제 해결**: 잦은 `Instantiate/Destroy`로 인한 메모리 할당(Garbage Collection) 및 프레임 드랍 방지.
    *   **통합 관리**: `Core.VFXManager` 싱글톤을 생성하여 게임 내 모든 파티클(폭발, 해킹, 타격 등)을 중앙 제어.
    *   **자동 반환**: 파티클 재생이 끝나면 자동으로 비활성화되어 풀(Pool)로 돌아가는 순환 구조 완성. ♻️
*   **폭발 효과 적용**:
    *   **Kamikaze Explosion**: 자폭병의 폭발 효과를 풀링 시스템으로 교체.
    *   **Hack Explosion**: 해킹 이펙트 또한 `HackVFXManager`를 제거하고 통합 매니저로 이관 준비.

---

## 📅 2026-02-18
### 1. 👹 보스전 패턴 구현 (Boss Pattern V1) - [완료]
*   **BossMissileLauncher**:
    *   **Multi-Shot**: 부채꼴 모양으로 3~5발의 미사일을 동시 발사하는 확산형 패턴 구현. 🚀🚀🚀
    *   **3D Homing**: 보스가 쏘는 미사일은 횡스크롤(2D) 한계를 넘어, 3D 공간을 활용해 플레이어를 입체적으로 추적하도록 업그레이드.
*   **System Integration**:
    *   `HackVFXManager`를 완전히 삭제하고 `Core.VFXManager`로 통합 완료.
    *   `BaseEnemy`가 `VFXManager`를 직접 참조하여 피격/사망/해킹 시 이펙트를 호출하도록 구조 개선.

---

## 📅 2026-02-19
### 1. 🐛 치명적 버그 수정 (Critical Bug Fixes) - [완료]
*   **무한 데미지 버그 (Infinite Damage Glitch)**:
    *   **원인**: 미사일을 해킹(`Q`)할 때, 이미 해킹된 미사일이 또 해킹되면서 데미지 로직이 중복 실행되어 보스가 순식간에 사망(Insta-kill).
    *   **해결**: `EnemyMissile.cs`에 `IsFrozen` 체크를 추가하고, 해킹 시 `Unfreeze`(얼음 해제) 상태로 전환하여 중복 해킹을 원천 차단. 🛑
    *   **타겟팅 개선**: 보스가 없을 때 해킹한 미사일이 허공을 맴돌지 않고 일반 적(Enemy)을 찾아가도록 `Fallback` 로직 추가.

### 2. 🎮 조작감 개선 (Input Polish) - [완료]
*   **Input Action Mapping**:
    *   해킹(`Hack`) 액션에 키보드 `Q`와 `E`를 모두 할당하여 플레이어 편의성 증대.

### 3. 🩸 보스 UI 시스템 (Boss Health UI) - [완료]
*   **UI/UX 디자인**:
    *   화면 하단 중앙(`Bottom Center`)에 고정된 전용 체력바 구현. 
    *   **이중 슬라이더(Dual Slider)**: 실제 체력(Red)이 먼저 줄어들고, 잔상(White Ease)이 부드럽게 따라오며 피격량을 시각화.
*   **타격감 연출 (Juice)**:
    *   **Shake Effect**: 피격 시 체력바 전체가 지진 난 것처럼 흔들림 (`CanvasGroup` 기반 연출). 🫨
    *   **Flash Effect**: 체력바 색상이 순간적으로 하얗게 번쩍(White Flash)이며 강렬한 타격감 전달. ⚡
*   **옵저버 패턴 (Observer Pattern)**:
    *   `BossHealth.cs`에서 `OnHealthChanged`, `OnDamageTaken` 이벤트를 발행하고, UI가 이를 구독하는 방식으로 결합도 낮춤.

---

## 📅 2026-02-20
### 1. 🌊 웨이브 시스템 (Wave System) - [완료]
*   **WaveManager**:
    *   **순차 스폰**: 미리 설정된 웨이브 데이터에 따라 적들을 지정된 위치(`Spawn Points`)에서 순차적으로 스폰.
    *   **보스 등장**: 마지막 웨이브 클리어 시 자동으로 보스(`BossPrefab`)가 스폰되는 흐름 제어 완성.

### 2. 🚨 오프스크린 레이더 (Off-Screen Radar) - [완료]
*   **ThreatRadarUI**:
    *   화면 밖(카메라 시야 이탈)에 있는 위협 요소를 화면 가장자리에 화살표(Indicator)로 표시.
    *   **감지 대상**: 플레이어를 향해 날아오는 미사일, 조준 중인 적, 자폭 카운트가 시작된 카미카제 적.
    *   **동적 회전 & 스케일링**: 위협의 위치를 추적하여 화살표가 해당 방향을 가리키며 캔버스 테두리에 착 달라붙도록(`Clamp`) 수학적 계산(`Atan2`, `ViewportToScreenPoint`) 적용.
    *   **최적화**: 매 프레임 스캔하는 대신 코루틴(`scanInterval`)으로 연산 부하 최소화 및 오브젝트 풀링(`_indicatorPool`) 적용.
    *   **예외 처리 플리싱**: 
        *   일시정지(`Time.timeScale == 0`) 시 레이더 경고 무시.
        *   대시 관통으로 얼어붙은(`IsFrozen`) 카미카제 적 경고 무시.

### 3. 💥 전투 및 액션 폴리싱 (Combat & Action Polish) - [완료]
*   **대시(Dash) 밸런싱**:
    *   대시 관통(OverlapBox/BoxCast) 판정 박스의 크기(Y, Z축)를 기존 대비 50% 수준으로 대폭 축소.
    *   투사체나 미사일을 뚫고 지나갈 때 훨씬 더 정교한 조준과 타이밍이 요구되도록 난이도 상향 (텐션 증가).
*   **헤비 랜딩 (Heavy Landing)**:
    *   플레이어가 공중에서 일정 속도 이상(`fastFallSpeed`)으로 빠르게 낙하하여 바닥에 착지할 때 충격파 발생.
    *   충격파 범위 내의 적들을 넉백(폭발 밀어내기)시키고 광역 동결(`Freeze`) 디버프를 부여하여 슈퍼히어로 같은 착지 연출 구현.
*   **카미카제 버그 패치**:
    *   플레이어가 대시 상태일 때는 카미카제 적과 닿아도 기폭되지 않고 관통(동결) 판정을 우선하도록 수정 (즉사 억까 방지).
    *   URP 환경에서 자폭 깜빡임 효과가 적용되지 않던 쉐이더 프로퍼티(`_BaseColor` 누락) 버그 수정.
*   **HitStop 버그 패치**:
    *   피격 순간(HitStop 코루틴 진행 중) 일시정지(ESC)를 누르면 `Time.timeScale`이 1로 강제 복구되던 현상 방어 (`!isPaused` 체크).

### 4. 🎥 다이나믹 속도 연출 (Dynamic Speed Effects) - [완료]
*   **Velocity FOV & Blur**:
    *   플레이어의 이동 속도(`velocity.magnitude`)에 비례하여 메인 카메라의 시야각(`FOV`)이 점진적으로 넓어지는 연출 추가 (`CameraEffectManager`).
    *   속도에 맞춰 URP Post-Processing의 모션 블러(Motion Blur) 강도를 실시간 조절하여 질주하는 느낌 극대화.
*   **Speed Lines 튜닝**:
    *   단순 On/Off 였던 스피드 파티클을 속도 비율(`speedRatio`)에 연동.
    *   속도가 빠를수록 파티클 방출량(`Emission Rate`), 선의 길이(`startSizeY`), 스폰 반경(`Radius`)이 동적으로 증가하도록 튜닝.
*   **잔상(Ghost Trail) 최적화**:
    *   빠르게 이동할 때 잔상의 틈이 벌어지는 현상을 막기 위해, 속도에 비례하여 잔상 생성 간격(`Interval`)이 촘촘해지도록 로직 개선. 오버슈팅 방어 코드 적용 완료.

---

## 📅 2026-02-23
### 1. 🛡️ 쉴드 및 방어 판정 개선 (Shield & Defense) - [완료]
*   **대시 정면 충돌 페널티**:
    *   `PlayerMovement` 대시 중 정면 쉴드 방어 상태를 우선 스캔(1 Pass)하여 적 빙결을 강제 종료.
    *   튕겨날 때 플레이어에게 1 데미지 페널티(`TakeDamage(1)`) 부여.
*   **해킹 연계 피격**:
    *   방패병이 쏜 투사체를 해킹하여 직격 시, 본체 데미지 무시하고 활성화된 방패만 1차 파괴(`BreakShield`)하는 기믹(방어막 소거) 분리 및 신설.
*   **가드 불능 버그 수정**:
    *   `Physics.OverlapBox` 순차 꼬임으로 인해 타겟(본체)이 방패보다 먼저 피격되어 빙결 방어막 기능이 유실되는 현상 수리.

### 2. 🚀 투사체 판정 및 궤도 수정 (Projectile Orbit & Ghosting) - [완료]
*   **공전(Orbit) 이슈 해결**:
    *   빠른 이속(1.5배)으로 돌아온 해킹 미사일이 `Slerp`의 감속 현상 탓에 표적을 맴돌기만 하던 현상을 강제 꺾기 `RotateTowards` 로 교체하여 100% 명중 보장.
*   **유령 관통(Ghosting) 수정**:
    *   발사 시점부터 충돌 무시(`Physics.IgnoreCollision`)가 들어가 있던 미사일이, 해킹 후 다시 주인에게 돌아와도 튕겨나지 않고 체내 투명 관통 및 안착하는 버그를 락(Lock) 해제로 막음.
*   **짐벌락(Gimbal Lock) 떨림 방지**:
    *   2D 모드 평면 유도 시 Z축을 강제 0으로 치환하며 생기는 회전 틱을, 벡터 `forward` 평면 투영(Y축·X=0) 로직으로 교체하여 부드러운 직진 보장.
*   **타겟 중심축 보장**:
    *   타겟이 발밑 피벗(Pivot)일 때 발생하던 바닥 박치기 오차를 방지하기 위해 `Collider.bounds.center`를 추적하도록 타겟 좌표 정밀 재설계.
*   **이벤트 존(Trigger) 폭발 버그 방지**:
    *   `isTrigger`로 설정된 무형성 맵 트리거 등에 투사체가 부딪히며 공중 폭발하지 않도록 판정선 픽스.

### 3. ✨ 고퀄리티 VFX 연동 (Visual Effect Graph) - [완료]
*   **UNI VFX 자산 적용**:
    *   `EnemyMissile`에 `Visual Effect` 컴포넌트 연동.
    *   에디터에서 프리뷰를 확인하기 위한 `Event Tester` 사용법 및 `Always Refresh` 설정.
*   **이벤트 기반 구조 (Event-Driven)**:
    *   `create`, `hit` 등의 문자열 이벤트를 스크립트에서 전송하여 파티클의 생성, 루프, 피격 등을 정밀하게 제어.
*   **히트 이후 처리 방식 변경**:
    *   기존 `Destroy()` 호출 대신, `_isHit` 플래그를 두어 렌더러와 충돌체만 끄고 2초 뒤 파괴함으로써 파티클의 자연스러운 소멸(Fade-out) 시간 보장.

### 4. 🛤️ 정해진 경로 이동 플랫폼 (Moving Platform) - [완료]
*   **MovingPlatform**:
    *   지정된 경유지(`Waypoints`) 배열을 따라 `Rigidbody.MovePosition`으로 부드럽게 이동하는 레벨 기믹 (플랫포머 요소 추가).
    *   루프(`Loop`), 왕복(`PingPong`), 정차 시간(`WaitTime`) 등의 커스텀 옵션으로 다양한 궤도 제작 가능.
*   **PlatformFunction 싱크 연동**:
    *   플랫폼 위에 플레이어가 탑승 시, 마찰력 부족으로 미끄러지는 현상을 방지하기 위해 `PlayerMovement`에서 플랫폼 델타(이동 변화량)를 추적 및 합산하도록 조치 (SetParent로 인한 강제 속도 덮어쓰기 문제 완벽 대응).

### 5. ⚡ 레이저 함정 기믹 (Laser Hazard) - [완료]
*   **LaserHazard**:
    *   `LineRenderer` 및 `Physics.Raycast` 기반 연속 데미지 & 넉백 함정.
    *   **벽에 막힘**: 벽(`obstacleLayer`)을 만나면 레이저 렌더러와 판정 길이가 해당 거리에 맞춰 끊어짐.
    *   **대시 무적 통과**: `OverlapCapsule`을 스캔하는 동안 플레이어 일시 무적 여부(`pm.IsDashing`)를 확인해, 대시 중이면 데미지 0 및 무사 통과 허용! 슝!💨

### 6. 🏃‍♂️ 캐릭터 3D 모델 애니메이션 연동 (Player Animator) - [완료]
*   **구조 분리 설계 (Loose Coupling)**:
    *   물리 연산 전용 부모(`Capsule Collider` + `Rigidbody`가 있는 `@Player`)와 애니메이션/메쉬 전용 자식(`Robot Roller`)으로 트리 구조를 완전히 분리하여 회전/충돌 버그 원천 차단.
    *   `PlayerMovement`와 `PlayerHealth`에 `Action` 델리게이트 이벤트(`OnJumpEvent`, `OnDashEvent`, `OnTakeDamageEvent` 등)를 선언하고, 신규 생성한 `PlayerAnimator.cs`에서 이를 구독하여 Trigger를 발동하는 방식으로 코드 결합도를 낮춤.
*   **루트 방향 절대 좌표 보정**:
    *   애니메이션 `Forward`, `Strafe` 계산 시, 로봇 모델 자신의 `transform`이 아닌, 부모 본체(`_playerMovement.transform`)의 `InverseTransformDirection`을 기준으로 삼아 90도 회전된 모델들도 올바르게 정면 이동 걷기 모션이 출력되도록 보완.
    *   `Mathf.Clamp`와 보간(`0.1f`, `Time.deltaTime`)을 적용해 애니메이션 블렌딩 트리 스무딩.
*   **이중 점프 판정 강력 봉인 (Root Motion Fix)**:
    *   `FBX` 파일 자체에 Y축 위치값이 구워져 있어 `Bake Into Pose` 적용이 불가능했던 구버전 모델 뼈대에 맞춰, `PlayerAnimator.LateUpdate`에서 매 프레임 모델의 뼈대(`Rig` 또는 `RigPelvis`) Y축 로컬 좌표를 `0`으로 강제로 멱살 잡아 내리는 하드코딩 해결책 도입.
*   **조준선 원점 분리 연동**:
    *   거대한 캡슐 콜라이더로 인해 발바닥 중앙(`Y=0`)에서 조준선이 나가던 이슈를 수정. `PlayerAim`에 `FirePoint`를 연결하거나, 없을 시 기본 배꼽 위치(`Y+1.0f`)에서 조준선 라인이 뻗도록 타겟 시크 조율 완수.

### 7. 🛤️ 정해진 경로 이동 플랫폼 조작감 유지 (Platform Feel Tweak) - [완료]
*   **물리적 관성(상대 속도) 허용**:
    *   플랫폼의 속도(`_platformVelocityZ`)만큼 플레이어 조작 속도를 감산하여 절대 속도를 고정시키려던 시도가 에스컬레이터를 타는 듯한 "무빙 워크 패널티/위화감" 버그를 유발함을 파악.
    *   액션 플랫포머 특유의 **이동 가속도**를 살리기 위해, 속도 상쇄(`- _platformVelocityZ`) 로직을 당일 즉각 롤백(제거)함으로써 관성 점프와 쾌적한 질주 조작성 회복 보장.

---

## 📅 2026-02-24
### 1. 👾 2.5D 캐릭터 애니메이션 전환 & 최적화 (2.5D Animation Polish) - [완료]
*   **애니메이터 컨트롤러 (Animator Controller) 세팅 정규화**:
    *   3D용 블렌딩 로직을 제거하고, 픽셀 아트 액션 게임 특유의 찰진 타격감을 위해 모든 트랜지션의 `Has Exit Time`을 끄고, `Transition Duration`을 `0`으로 세팅.
*   **PlayerAnimator.cs 리팩토링**:
    *   레거시 3D 속도 파라미터를 제거하고, `IsWalking`, `IsJumping`, `IsSwinging` 파라미터와 `Jump`, `Dash` 트리거 기능으로 전면 교체.
    *   `_animator` 컴포넌트를 `GetComponentInChildren`으로 자식(Sprite 렌더러)에서 올바르게 찾도록 수정.
*   **공중(Air) 및 스윙(Swing) 판정 강화**:
    *   물리 연산 딜레이로 인한 점프 직후 `IsGrounded == true` 착각 버그 방어 완료. `_rb.linearVelocity.y > 0.1f` 조건을 추가해 강제 점프 판정 보장.
    *   훅 액션 중이거나 넉백될 때 `IsSwinging`을 켜주는(True) 상태 체크 연동 로직 적용 완료.

### 2. 🎮 2.5D 조작감 마스터 튜닝 (Input Throttling & 2D Snapping) - [완료]
*   **점프 연타 방어벽 (Jump Cooldown)**:
    *   스페이스바 연타 및 버퍼링으로 인해 더블/트리플 점프가 의도치 않게 나가던 현상 완전 차단.
    *   `PlayerMovement`에 `jumpCooldown = 0.2f` 시스템을 도입하여 점프 쿨타임을 관리하는 안정적인 하드웨어급 쓰로틀링 구축 완료.
*   **종잇장 버그 해결 (Slerp Elimination)**:
    *   `ApplyRotation()` 내부 보간 코드를 전면 삭제하고, +Z와 -Z 180도로 칼같이 즉시 뒤집어지는(Snap) 2.5D 맞춤형 회전 하드코딩 적용. 액션 게임의 빠릿한 맛 200% 증가!

---

## 📅 2026-02-25
### 1. 🎯 조준선 파이프라인 정비 (Crosshair AimLine) - [완료]
*   **조준선 폭 감소 & 커스터마이징 허용 (`Alpha Curve`)**:
    *   기존 Particle Shader의 `TintColor` 지배적 침범을 피해 `Legacy Shaders/Particles/Alpha Blended`를 유지하되 Vertex Color로 투명도(Gradient)를 제어하도록 수정.
*   **알파 런타임 제어기 구현 (DeadZone Customization)**:
    *   `AnimationCurve`를 인스펙터에 노출하여 조준선의 시작부분(DeadZone) 투명도를 유저가 마음대로 튜닝할 수 있도록 GC Allocation 없이(`GradientAlphaKey` 수동 매핑 방식) 극한의 최적화로 구현 탑재!

### 2. 🧚‍♀️ 요정 동료 시스템 시각화 (Fairy Companion System) - [완료]
*   **고해상도 스프라이트 (High-Res Sprite)**:
    *   도트(Pixel Art) 대신 URP/Lighting과 자연스럽게 어울리는 2.5D 고해상도 요정 애니메이션 스프라이트 시트 모델링.
*   **알파 블렌딩(Alpha Transparency) 수정**:
    *   Solid Background 이슈를 백그라운드 리무버 파이썬 코드를 통해 런타임에 처리하여 Alpha Transparent 상태로 에셋 자동화(`Fairy_Transparent.png`).
*   **스프라이트 흔들림 방지 (Anti-Wobbling)**:
    *   원화의 크기 차이로 발생하는 Y축 진동 문제를 4프레임의 Pivot을 렌더링 중앙점(구슬)로 수동 Custom Fix(정렬) 안내.

### 3. 🪄 요정 PID 비행 제어기 (Fairy Flight Control) - [완료]
*   **독립 제어 시스템 (PID Follower)**:
    *   무거운 `Rigidbody` 대신 수리 물리학적 `PID.cs`를 재활용하여 가벼운 스프링 감쇠(역학) 기반 비행 추적 시스템 `FairyFollower.cs` 구축.
*   **비주얼/동적 액션 부여**:
    *   **호버링(Hovering)**: `Mathf.Sin`을 이용한 자체 상하 부유 렌더링.
    *   **공기역학 시선 처리(Tilt)**: 2D 종이 모델의 Y축 깊이 파고듦을 수정하여 Z축으로만 강제 Snap 보정. 비행 속도(`Velocity.z`)에 비례해 40도까지 머리를 기울어 숙이게 하는(Tilt) 속도감 있는 애니메이션 코딩 완성.

### 4. 🛠️ 전역 스크립트 최적화 (Global Optimization) - [완료]
*   **Vector3.Distance 상쇄**: 매 프레임 발생하는 자원 소모적인 `Vector3.Distance` 및 `magnitude` 연산을 루트 연산이 없는 `sqrMagnitude`로 일괄 교체 (`FairyFollower`, `EnemyShooter`, `MovingPlatform` 등).
*   **병목 색인 제거 (FindAnyObjectByType)**:
    *   해킹 스캔 및 미사일 역해킹 시 맵 전체를 뒤지던 악성 탐색 코드를 제거.
    *   기존에 생성해 둔 `OverlapSphere` 내의 배열 순회 및 `GameObject.FindWithTag` 해시 룩업 방식으로 전면 수정하여 성능 극대화 🚀.
*   **투사체 오브젝트 풀링 (Object Pooling)**:
    *   **공장 가동 중지!**: `EnemyShooter`, `EnemyMissile`, `EnemyProjectile`, `BossMissileLauncher` 등 각종 전투 객체들의 파괴/할당 병목을 막기 위해 `UnityEngine.Pool.ObjectPool` 시스템 적극 도입 및 델리게이트(`Action`) 재사용 반환 완벽 코딩. ♻️

### 5. 🕷️ 와이어 액션 개선 & 고무줄 반동 픽스 (Hook Zip Polish) - [완료]
*   **ZipToTargetRoutine 일원화**: 
    *   가벼운 적(Light)과 무거운 적(Heavy) 구분 없이, 훅 명중 시 플레이어가 항상 입체기동장치처럼 적에게 직접 날아가는(`ZipToTargetRoutine`) 익스트림 뷰로 통일.
*   **대시 관통 고무줄 버그(Rubber Banding) 픽스**:
    *   적을 향해 날아가는 도중, **대시 (Dash)** 나 점프 입력 시 와이어를 즉각적으로 텐션 파기(`StopHook`)시켜, 적을 뚫고 지나갈 때 와이어 역물리력 때문에 등 뒤로 확 튕겨져 버리는 반작용 버그 원천 차단! 😎

---

## 📅 2026-02-26
### 1. 💥 카미카제(자폭) 기능 분리 & 기즈모 시각화 - [완료]
*   **BaseEnemy.cs 개선**:
    *   기존에 소형 적(Light Enemy)에게만 종속되어 있던 자폭(Kamikaze) 패턴을 분리.
    *   인스펙터에 `canKamikaze` 토글(체크박스)을 추가하여 소형/대형 상관없이 원하는 적만 기폭되도록 스크립트 독립성 보장.
    *   플레이어가 `kamikazeTriggerRadius` 내로 진입 시 즉시 자폭 추적이 시작되도록 루트 연산 없는 거리 계산(`sqrMagnitude`) 적용.
*   **Scene 뷰 시각화 (Gizmos)**:
    *   `OnDrawGizmosSelected`를 활용하여 `canKamikaze`가 켜진 적을 클릭하면, 에디터의 Scene 뷰에서 **추적 감지 반경**(빨간색)과 **폭발 반경**(주황색)을 투명한 원(WireSphere)으로 한눈에 볼 수 있도록 시각화 피드백 추가. 레벨 디자인 효율성 극대화!

### 2. 🕳️ 가속도 터널링(Tunneling) 버그 픽스 - [완료]
*   **PlayerMovement.cs 리팩토링**:
    *   **Collision Detection 상향**: 낙하 시 오브젝트나 땅을 관통해버리는 문제를 원천 차단하기 위해 `Rigidbody`의 충돌 감지 방식을 `Continuous`에서 `ContinuousDynamic`으로 한 단계 더 격상. 
    *   **종단 속도(Terminal Velocity) 클램핑**: 낭떠러지로 떨어질 때 중력 가속도가 매 프레임 무한히 커지면서 물리 연산의 콜라이더 틱을 스킵하는 현상을 방어하기 위해, 최대 낙하 속도가 `fastFallSpeed * 2.5f` 이상으로 증가하지 못하도록 `linearVelocity.y` 제한 코드 추가. 속도감은 유지하되 안정성 대폭 상승!
*   **PlayerHook.cs 윈치 터널링(Winch Tunneling) 완벽 방어**:
    *   **논리적 길이 락(Clamp) 동기화**: 훅을 걸고 W(줄 감기)를 누를 때, 벽에 가로막혀서 실제로는 앞으로 가지 못해도 내부적으로 줄 길이(`currentRopeLength`)만 계속 짧아지던 치명적인 논리 결함 식별.
    *   **Max Constraint Error 제한**: 실제 물리 거리(`distToAnchor`)와 내부 논리적 스칼라 거리의 오차가 `0.05f(5cm)`를 초과하지 못하도록 방어(Clamp)하여, 텐션 격차로 인해 Constraint `MovePosition`이 터무니없는 거리 오차를 계산해 벽을 단숨에 통과해버리는 악성 터널링 버그 원천 봉쇄!

### 3. 👻 2.5D 캐릭터 대응 고스트 트레일(Ghost Trail) 렌더링 지원 - [완료]
*   **GhostTrail.cs 리팩토링 및 버그 픽스**:
    *   **SpriteRenderer 지원 및 충돌 해결**: 기존 3D 메쉬(`SkinnedMeshRenderer`, `MeshFilter`)의 껍데기를 복사해 오던 방식에서, 범용적인 2.5D 스프라이트 캐릭터 적용에 호환되도록 `SpriteRenderer` 캡처 로직을 추가. 추가로 `MeshRenderer`와 `SpriteRenderer`가 동일한 오브젝트에 함께 붙어 발생하던 `AddComponent` 충돌 버그 방지를 위해, 최상위 `Ghost_Pool` 오브젝트 하위에 `MeshGhost`와 `SpriteGhost` 자식 객체를 별도로 생성해 렌더링을 완전히 분리!
    *   **동적 렌더링 스위칭**: `GhostEffect` 내부에서 복사 대상(`_isSprite`)이 3D 메쉬 모델인지, 2D 스프라이트 이미지인지 판별한 후, 각각 URP Shader 매테리얼과 Sprite Renderer의 고유 `color(TintColor)` 속성으로 분기를 나누어 자연스러운 페이드아웃(잔상 소멸) 기능 구현 완수!
    *   **로컬 스케일 배율 버그 픽스**: 부모 오브젝트(Player)의 스케일 값이 잔상 오브젝트 생성 시 누락되어 잔상이 캐릭터보다 거대하게 출력되던 버그를 해결하기 위해 렌더러 복사 시 `localScale` 대신 절대 스케일인 `lossyScale` 값을 주입하도록 수정.

### 4. ⚔️ Q키(해킹/처치) 감지 반경 맵 전체급으로 초대폭 상향 - [완료]
*   **PlayerMovement.cs (`OnHack`) 개선**:
    *   기존에는 플레이어 주변 `20f` 반경의 정지된 적만 사거리 내에 들어와 해킹 및 처치(Execute)가 가능했으나, 화면 밖 멀리 있는 적이라도 상관없이 무조건 처치 판정이 들어가도록 감지 반경(`hackRadius`)을 `2000f`로 100배 대폭 증가!
    *   극단적인 범위를 스캔해도 성능 저하가 없는 유니티 타겟 추출 방식인 `OverlapSphere`를 유지하면서 최적화와 거리 편의성을 모두 챙김!

### 5. 👥 적 타입(Light/Heavy) 구분 완전 삭제 및 로직 통일 - [완료]
*   **BaseEnemy.cs 및 PlayerAim.cs 리팩토링**:
    *   훅(Hook) 시스템 개편으로 인해 Light 적과 Heavy 적 모두 동일하게 플레이어가 적에게 날아가는(Zip) 방식으로 통합됨에 따라, 굳이 인스펙터에 남겨둘 필요가 없어진 `EnemyType` Enum 자체를 스크립트에서 완전 삭제! 
    *   이에 따라 조준점(AimLine)도 타겟 타입에 따라 색상과 화살표 방향이 다르게 표시되던 코드를 걷어내고, 가장 직관적이고 공격적인 역방향 붉은색 화살표 하나로 심플하게 통일하여 시각적 혼란도 방지!

### 6. 🪝 훅(Hook) 돌진 시 적 앞 안전 거리 확보 및 체공(Brake) 픽스 - [완료]
*   **PlayerHook.cs (`ZipToTargetRoutine`) 개선**:
    *   **안전 반경(`safeZipDistance`) 도입**: 적에게 훅을 걸고 날아갈 때(Zip) 몹의 정중앙 좌표까지 파고들어 충돌 데미지를 입던 현상을 방지하기 위해, 적 판정이면 `1.5f`의 안전거리를 두고 훅을 종료시키는 `safeZipDistance` 변수를 도입.
    *   **관성 브레이크 락**: 훅이 종료된 시점에 중력 및 관성이 남아있어 미끄러지면서 적과 박치기하던 문제를 막기 위해, 거리에 도달해 멈출 때 `_rb.linearVelocity = Vector3.zero;`를 삽입. 날아가다 몹 멱살 잡기 직전에 공중에서 딱 멈추는 멋진 타격 연출 가능!

### 7. ⏱️ 피격 불릿 타임 (Hit Bullet Time) & 화면 왜곡 - [완료]
*   **PlayerHealth.cs (`TakeDamage`) 개선**:
    *   **위기 상황 연출 강화**: 플레이어가 피해를 입었을 때 기존의 히트 스탑(HitStop)과 화면 흔들림(Shake)뿐만 아니라, **0.5초 동안 0.1배속으로 시간이 느려지는 불릿 타임(Bullet Time)**이 추가로 발동되도록 `GameManager` 연동!
    *   **시각적 타격감 극대화**: 데미지를 입는 순간 `PostProcessManager`를 통해 화면 테두리가 어그러지는 강렬한 **색수차(Chromatic Aberration)** 효과(강도 1.0, 지속 0.5초)를 터뜨려 아프고 혼미한 시각적 피드백 완성 💥!

### 8. 💨 대시(Dash) 터널링(벽 뚫음) 버그 픽스 - [완료]
*   **PlayerMovement.cs (`DashRoutine`) 개선 (V3 풀 체인지!)**:
    *   **물리 루프 동기화**: 기존 `Update` 프레임(`yield return null;`)에 맞춰 속도를 강제로 덮어쓰던 코루틴을, `yield return new WaitForFixedUpdate();`로 교체하여 물리 엔진 계산 주기와 완벽히 동기화.
    *   **물리 충돌 사후 편승 (100% 방어)**: 기존 `SphereCast` 방식은 이미 벽에 가깝거나 모서리에 있을 때 가상 구체가 오버랩되어 감지 실패(사각지대)가 발생하는 취약점을 발견.
    *   **속도 편차 기반 중단 로직 (V3)**: 강제로 `dashSpeed`를 계속 주입하는 대신 물리 엔진(Physics)이 직전 프레임에서 계산을 끝낸 실제 속도(`_rb.linearVelocity`)를 역추적. 
    *   만약 벽에 가로막혀서 내가 가고 싶은 대시 방향의 속도 성분이 목표치(`dashSpeed`) 대비 30% 이하로 심각하게 깎였다면? -> **벽에 부딪혔다고 100% 확신!**
    *   벽에 부딪힌 즉시 강제 속도 주입을 중단하고 꺾인 방향(슬라이딩)의 물리력을 존중하게 만들어 얇은 벽, 두꺼운 벽, 모서리 등 어떠한 곳에서도 무적 철통 방어 성공!
*   **PlayerHook.cs 매달림-대시 충돌(터널링) 픽스**:
    *   로프에 매달려 스윙 중일 때 대시를 쓰면 훅의 장력이 강제로 원위치(MovePosition) 시켜서 벽 안쪽으로 플레이어를 텔레포트시키던 버그 파악. 
    *   이제 매달린 상태에서 대시를 발동하면, 스파이더맨처럼 **붙잡고 있던 훅을 즉시 놓고(Break) 시원하게 대시로 날아가도록** 물리적 충돌 관계를 리뉴얼 완료! 🕷️💨
