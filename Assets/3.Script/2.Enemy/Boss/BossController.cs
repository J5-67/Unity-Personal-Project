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
            
            // 보스가 활성화되면 공격 루틴 시작!
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
            Debug.Log($"🤖 AI: 페이즈 변화 감지! 현재 페이즈: {newPhase}");
        }

        private IEnumerator AttackLoop()
        {
            // 보스 등장 연출(Intro) 시간을 고려해서 잠시 대기!
            yield return new WaitForSeconds(3.5f);

            while (bossHealth != null && !bossHealth.IsDead)
            {
                _isAttacking = true;
                
                // 🚀 미사일 발사!
                if (launcher != null)
                {
                    launcher.FireSpreadMissiles();
                }

                _isAttacking = false;

                // 페이즈가 올라갈수록 공격 간격이 짧아져서 더 어려워질 거야! 😎
                float currentInterval = attackInterval;
                if (_currentPhase == 2) currentInterval = phase2AttackInterval;
                else if (_currentPhase == 3) currentInterval = phase3AttackInterval;

                yield return new WaitForSeconds(currentInterval);
            }
        }
    }
}
