using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
namespace Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }
        private Dictionary<int, ObjectPool<GameObject>> _pools = new Dictionary<int, ObjectPool<GameObject>>();
        private Transform _vfxContainer;
        [Header("🔥 Common VFX")]
        [SerializeField] private GameObject hackExplosionPrefab;
        [SerializeField] private float hackShakeIntensity = 2.0f;
        [SerializeField] private GameObject kamikazeExplosionPrefab;
        [SerializeField] private float kamikazeShakeIntensity = 1.0f;
        [SerializeField] private GameObject spawnEffectPrefab;
        [SerializeField] private GameObject bossExplosionPrefab;
        [SerializeField] private float bossShakeIntensity = 4.0f;
        [Header("💥 Boss Hit Settings")]
        [SerializeField] private float bossHitShakeIntensity = 0.3f;
        public void PlaySpawnEffect(Vector3 position)
        {
            if (spawnEffectPrefab != null)
            {
                PlayVFX(spawnEffectPrefab, position, Quaternion.identity);
            }
        }
        public void PlayHackExplosion(Vector3 position)
        {
            if (hackExplosionPrefab != null)
            {
                PlayVFX(hackExplosionPrefab, position, Quaternion.identity);
                if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(hackShakeIntensity);
                }
            }
        }
        public void PlayKamikazeExplosion(Vector3 position)
        {
            if (kamikazeExplosionPrefab != null)
            {
                PlayVFX(kamikazeExplosionPrefab, position, Quaternion.identity);
                if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(kamikazeShakeIntensity);
                }
            }
        }
        public void PlayBossExplosion(Vector3 position)
        {
            if (bossExplosionPrefab != null)
            {
                PlayVFX(bossExplosionPrefab, position, Quaternion.identity);
                if (Core.GameManager.Instance != null) Core.GameManager.Instance.TriggerCameraShake(bossShakeIntensity);
            }
        }
        public void PlayBossHitExplosion(Vector3 position)
        {
            if (hackExplosionPrefab != null)
            {
                PlayVFX(hackExplosionPrefab, position, Quaternion.identity);
                if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(bossHitShakeIntensity);
                }
            }
        }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            _vfxContainer = new GameObject("@VFX_Pool").transform;
            _vfxContainer.SetParent(transform);
        }
        public void PlayVFX(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return;
            int id = prefab.GetInstanceID();
            if (!_pools.ContainsKey(id))
            {
                CreatePool(prefab, id);
            }
            GameObject instance = _pools[id].Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            if (instance.TryGetComponent(out ParticleSystem ps))
            {
                ps.Play();
                float returnTime = ps.main.duration + ps.main.startLifetime.constantMax;
                StartCoroutine(ReturnRoutine(_pools[id], instance, returnTime));
            }
            else
            {
                StartCoroutine(ReturnRoutine(_pools[id], instance, 2.0f));
            }
        }
        private void CreatePool(GameObject prefab, int id)
        {
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    GameObject obj = Instantiate(prefab, _vfxContainer);
                    return obj;
                },
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 10,
                maxSize: 50
            );
            _pools.Add(id, pool);
        }
        private IEnumerator ReturnRoutine(ObjectPool<GameObject> pool, GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (instance != null && instance.activeSelf)
            {
                pool.Release(instance);
            }
        }
    }
}
