using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 전용 미사일 발사기
// 여러 발의 미사일을 부채꼴 등 다양한 패턴으로 발사
public class BossMissileLauncher : MonoBehaviour
{
    [Header("🚀 Missile Configuration")]
    [SerializeField] private EnemyMissile missilePrefab; // 발사할 미사일 프리팹 (EnemyMissile 스크립트 포함)
    [SerializeField] private Transform[] firePoints;     // 미사일이 발사될 위치들 (예: 왼쪽 어깨, 오른쪽 어깨)

    [Header("🎯 Pattern Settings")]
    [SerializeField] private int missileCount = 6;       // 한 번 발사 시 미사일 개수
    [SerializeField] private float spreadAngle = 90f;    // 부채꼴 각도 (예: 90도 부채꼴)
    [SerializeField] private float launchDelay = 0.5f;   // 발사 후 유도 시작까지의 지연 시간 (초기 상승)
    [SerializeField] private float fireInterval = 0.1f;  // 미사일 간 발사 간격 (순차 발사 시)

    [Header("🔊 Sound Effects")]
    // [ToDo] AudioManager 연동 필요

    [Header("Debug")]
    [SerializeField] private bool autoFireTest = false;  // 자동 발사 테스트용
    [SerializeField] private float testInterval = 3.0f;

    private float _timer;

    private void Update()
    {
        if (autoFireTest)
        {
            _timer += Time.deltaTime;
            if (_timer >= testInterval)
            {
                FireSpreadMissiles();
                _timer = 0f;
            }
        }
    }

    // 부채꼴 확산 발사 (Spread Launch)
    // [Key Logic] 초기에는 위쪽(부채꼴)으로 발사하고, launchDelay 후에 플레이어를 향해 유도 시작
    public void FireSpreadMissiles()
    {
        if (missilePrefab == null) return;
        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogWarning("[BossMissileLauncher] FirePoints가 설정되지 않았습니다!");
            return;
        }

        StartCoroutine(SpreadFireRoutine());
    }

    private IEnumerator SpreadFireRoutine()
    {
        // 1. 발사 각도 계산 (부채꼴)
        // startAngle: 가장 왼쪽 각도
        // angleStep: 미사일 사이의 각도
        float startAngle = -spreadAngle / 2f;
        float angleStep = spreadAngle / (missileCount > 1 ? missileCount - 1 : 1);

        for (int i = 0; i < missileCount; i++)
        {
            // 2. 현재 미사일의 발사 각도 계산
            float currentAngle = startAngle + (angleStep * i);
            
            // 3. 발사 위치 선택 (FirePoints 순환)
            Transform spawnPoint = firePoints[i % firePoints.Length];

            // 4. 회전 계산 (보스의 위쪽 방향 기준 + Z축 회전으로 부채꼴 만듦)
            // Quaternion.AngleAxis(각도, 축) 사용
            // 횡스크롤(XY 평면) 기준: Z축 회전
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
            
            // 5. 발사 방향 벡터 (보스의 윗방향 기준)
            // [Fix] 보스가 회전해도(기울어져도) 그 방향 기준으로 쏘도록 변경
            Vector3 fireDirection = rotation * transform.up; 
            // 횡스크롤이므로 Y축 윗방향(Vector3.up)을 기준으로 Z축을 돌려서 부채꼴 형성
            // 만약 3D라면 보스 정면(Vector3.forward) 기준으로 Y축 회전일 수도 있음.
            // 일단은 횡스크롤 가정 (XY 평면)

            // 6. 미사일 생성 & 발사
            CreateMissile(spawnPoint.position, fireDirection);

            // 7. 순차 발사 대기
            if (fireInterval > 0)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }
    }

    private void CreateMissile(Vector3 position, Vector3 direction)
    {
        // [Opt] 나중엔 오브젝트 풀링 사용 권장 (지금은 Instantiate)
        EnemyMissile missile = Instantiate(missilePrefab, position, Quaternion.identity);
        
        // 보스 미사일은 3D 공간 추적 활성화 (X축 무시 끄기)
        missile.Set3DHoming(true);

        // 미사일 초기화 (방향 설정 및 유도 지연)
        // Set3DHoming 후 Launch 호출 (그래야 올바른 모드로 시작)
        missile.Launch(direction, launchDelay);
    }
}
