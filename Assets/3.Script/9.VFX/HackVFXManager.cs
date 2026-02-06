using UnityEngine;
using UnityEngine.Pool;

namespace VFX
{
    public class HackVFXManager : MonoBehaviour
    {
        public static HackVFXManager Instance { get; private set; }

        [Header("🔥 Effect Settings")]
        [SerializeField] private ParticleSystem hackExplosionPrefab;
        [SerializeField] private int poolSize = 10;
        
        [Header("📸 Screen Effect")]
        [SerializeField] private float shakeIntensity = 2.0f;

        private ObjectPool<ParticleSystem> _pool;

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

            InitPool();
        }

        private void InitPool()
        {
            if (hackExplosionPrefab == null)
            {
                Debug.LogWarning("HackVFXManager: Particle Prefab is missing.");
                return;
            }

            _pool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(hackExplosionPrefab, transform),
                actionOnGet: (ps) => ps.gameObject.SetActive(true),
                actionOnRelease: (ps) => ps.gameObject.SetActive(false),
                actionOnDestroy: (ps) => Destroy(ps.gameObject),
                defaultCapacity: poolSize,
                maxSize: 20
            );
        }

        public void PlayHackEffect(Vector3 position)
        {
            if (hackExplosionPrefab == null) return;

            ParticleSystem ps = _pool.Get();
            ps.transform.position = position;
            ps.Play();

            StartCoroutine(ReturnToPool(ps));
        }

        private System.Collections.IEnumerator ReturnToPool(ParticleSystem ps)
        {
            yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
            _pool.Release(ps);
        }
        
        public void TriggerMassiveGlitch()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.TriggerCameraShake(shakeIntensity);
            }
        }
    }
}
