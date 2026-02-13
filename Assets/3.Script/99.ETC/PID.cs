using UnityEngine;

[System.Serializable]
public class PID
{
    private float _lastError;
    private float _integral;

    /// <summary>
    /// PID 제어 결과값 반환
    /// </summary>
    /// <param name="currentError">현재 오차 (목표값 - 현재값)</param>
    /// <param name="deltaTime">델타 타임</param>
    /// <param name="kp">비례 계수 (P)</param>
    /// <param name="ki">적분 계수 (I)</param>
    /// <param name="kd">미분 계수 (D)</param>
    /// <returns>제어 출력값</returns>
    public float GetOutput(float currentError, float deltaTime, float kp, float ki, float kd)
    {
        // 델타타임이 너무 작으면 0 반환 (나눗셈 에러 방지)
        if (deltaTime <= 0.00001f) return 0f;

        // 적분항 계산 (I가 0이면 누적값 초기화)
        if (ki <= 0f)
        {
            _integral = 0f;
        }
        else
        {
            _integral += currentError * deltaTime;
        }

        // 미분항 계산 (오차 변화율)
        float derivative = (currentError - _lastError) / deltaTime;
        _lastError = currentError;

        // 최종 출력: P항 + I항 + D항
        return (currentError * kp) + (_integral * ki) + (derivative * kd);
    }

    /// <summary>
    /// PID 상태 초기화
    /// </summary>
    public void Reset()
    {
        _lastError = 0f;
        _integral = 0f;
    }
}
