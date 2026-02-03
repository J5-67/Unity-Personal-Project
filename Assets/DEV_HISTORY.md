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
