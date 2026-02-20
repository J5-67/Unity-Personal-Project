using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        // Prefab InstanceID -> Pool 매핑
        private Dictionary<int, ObjectPool<GameObject>> _pools = new Dictionary<int, ObjectPool<GameObject>>();

        // VFX 오브젝트들을 깔끔하게 정리할 부모 트랜스폼
        private Transform _vfxContainer;

        [Header("🔥 Common VFX")]
        [SerializeField] private GameObject hackExplosionPrefab;
        [SerializeField] private float hackShakeIntensity = 2.0f; // [New] 쉐이크 강도 통합

        [SerializeField] private GameObject kamikazeExplosionPrefab;
        [SerializeField] private float kamikazeShakeIntensity = 1.0f; // [New] 자폭은 조금 약하게

        public void PlayHackExplosion(Vector3 position)
        {
            if (hackExplosionPrefab != null)
            {
                PlayVFX(hackExplosionPrefab, position, Quaternion.identity);
                
                // [New] VFX 매니저가 흔들림까지 책임짐
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

                // [New] 자폭도 흔들림 추가
                if (Core.GameManager.Instance != null)
                {
                    Core.GameManager.Instance.TriggerCameraShake(kamikazeShakeIntensity);
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

            // 풀이 없으면 생성
            if (!_pools.ContainsKey(id))
            {
                CreatePool(prefab, id);
            }

            // 풀에서 가져오기
            GameObject instance = _pools[id].Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            
            // 파티클 시스템 재생 및 반환 예약
            if (instance.TryGetComponent(out ParticleSystem ps))
            {
                ps.Play();
                // 안전 마진 0.1초 추가
                float returnTime = ps.main.duration + ps.main.startLifetime.constantMax;
                StartCoroutine(ReturnRoutine(_pools[id], instance, returnTime));
            }
            else
            {
                // 파티클이 없는 경우 (Mesh renderer 등) 2초 뒤 반환
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
                maxSize: 50 // 너무 많이 쌓이면 Destroy
            );

            _pools.Add(id, pool);
        }

        private IEnumerator ReturnRoutine(ObjectPool<GameObject> pool, GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // [Fix] 씬 전환이나 외부 요인으로 이미 파괴된 오브젝트 접근 방지 (null 체크 추가)
            if (instance != null && instance.activeSelf)
            {
                pool.Release(instance);
            }
        }
    }
}
