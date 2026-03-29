using UnityEngine;
using System.Collections;
namespace Enemy.Boss
{
    public class BossController : MonoBehaviour
    {
        [Header("🔗 References")]
        [SerializeField] private BossMissileLauncher launcher;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private BossPhaseManager phaseManager;
        [Header("⚙️ Pattern Settings")]
        [SerializeField] private float attackInterval = 3.0f;
        [SerializeField] private float phase2AttackInterval = 2.0f;
        [SerializeField] private float phase3AttackInterval = 1.0f;
        private int _currentPhase = 1;
        private bool _isAttacking = false;
        private void OnEnable()
        {
            if (phaseManager != null)
            {
                phaseManager.OnPhaseChanged += OnPhaseChanged;
            }
            StartCoroutine(AttackLoop());
        }
        private void OnDisable()
        {
            if (phaseManager != null)
            {
                phaseManager.OnPhaseChanged -= OnPhaseChanged;
            }
            StopAllCoroutines();
        }
        private void OnPhaseChanged(int newPhase)
        {
            _currentPhase = newPhase;
        }
        private IEnumerator AttackLoop()
        {
            yield return new WaitForSeconds(3.5f);
            while (bossHealth != null && !bossHealth.IsDead)
            {
                _isAttacking = true;
                if (launcher != null)
                {
                    launcher.FireSpreadMissiles();
                }
                _isAttacking = false;
                float currentInterval = attackInterval;
                if (_currentPhase == 2) currentInterval = phase2AttackInterval;
                else if (_currentPhase == 3) currentInterval = phase3AttackInterval;
                yield return new WaitForSeconds(currentInterval);
            }
        }
    }
}
