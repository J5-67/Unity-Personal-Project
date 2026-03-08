using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMissileLauncher : MonoBehaviour
{
    [Header("🚀 Missile Configuration")]
    [SerializeField] private EnemyMissile missilePrefab;
    [SerializeField] private Transform[] firePoints;

    [Header("🎯 Pattern Settings")]
    [SerializeField] private int missileCount = 6;
    [SerializeField] private float spreadAngle = 90f;
    [SerializeField] private float launchDelay = 0.5f;
    [SerializeField] private float fireInterval = 0.1f;

    [Header("🔊 Sound Effects")]
    [SerializeField] private AudioClip fireSound;

    [Header("Debug")]
    [SerializeField] private bool autoFireTest = false;
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

    public void FireSpreadMissiles()
    {
        if (missilePrefab == null) return;
        if (firePoints == null || firePoints.Length == 0)
        {

            return;
        }

        StartCoroutine(SpreadFireRoutine());
    }

    private IEnumerator SpreadFireRoutine()
    {

        float startAngle = -spreadAngle / 2f;
        float angleStep = spreadAngle / (missileCount > 1 ? missileCount - 1 : 1);

        for (int i = 0; i < missileCount; i++)
        {

            float currentAngle = startAngle + (angleStep * i);

            Transform spawnPoint = firePoints[i % firePoints.Length];

            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);

            Vector3 fireDirection = rotation * transform.up;

            CreateMissile(spawnPoint.position, fireDirection);

            if (fireInterval > 0)
            {
                yield return new WaitForSeconds(fireInterval);
            }
        }
    }

    private UnityEngine.Pool.ObjectPool<EnemyMissile> _missilePool;

    private void Awake()
    {
        _missilePool = new UnityEngine.Pool.ObjectPool<EnemyMissile>(
            createFunc: () => {
                var m = Instantiate(missilePrefab);

                m.OnReleaseToPool = (proj) => _missilePool.Release((EnemyMissile)proj);
                return m;
            },
            actionOnGet: (missile) => missile.gameObject.SetActive(true),
            actionOnRelease: (missile) => {
                missile.gameObject.SetActive(false);
            },
            actionOnDestroy: (missile) => Destroy(missile.gameObject),
            defaultCapacity: 20,
            maxSize: 50
        );
    }

    private void CreateMissile(Vector3 position, Vector3 direction)
    {
        if (_missilePool == null) return;

        if (fireSound != null && Core.AudioManager.Instance != null)
        {
            Core.AudioManager.Instance.PlaySFX(fireSound);
        }

        EnemyMissile missile = _missilePool.Get();

        missile.transform.position = position;
        missile.transform.rotation = Quaternion.LookRotation(direction);

        missile.Set3DHoming(true);
        missile.SetOwner(transform); // 🎯 누구 미사일인지 알려줘야 해!

        missile.Launch(direction, launchDelay);

    }
}
