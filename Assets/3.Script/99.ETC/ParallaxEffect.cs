using UnityEngine;

namespace Environment
{
    // [유니] 배경이 카메라를 따라오는데, 원근감 있게 속도를 다르게 주는 스크립트야! 🌄
    public class ParallaxEffect : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("1 = 배경 고정 (카메라랑 똑같이 움직임), 0 = 움직이지 않음. (먼 배경 = 0.9, 가까운 배경 = 0.1)")]
        [SerializeField] private Vector2 parallaxFactor; // X=가로(World Z), Y=세로(World Y)
        
        [Header("Infinite Scrolling")]
        [Tooltip("체크하면 배경이 끊기지 않고 무한 반복됨 (Texture가 Seamless여야 함)")]
        [SerializeField] private bool infiniteHorizontal = true;
        [SerializeField] private bool infiniteVertical = false;

        private Transform _cameraTransform;
        private Vector3 _lastCameraPosition;
        private float _textureUnitSizeZ; // 가로(World Z) 길이
        private float _textureUnitSizeY; // 세로(World Y) 길이

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
                _lastCameraPosition = _cameraTransform.position;
            }
            else
            {
                Debug.LogError("[유니] 메인 카메라를 찾을 수 없어! 태그가 MainCamera인지 확인해줘!");
            }

            // [유니] 오빠 게임은 Z축이 가로니까, 텍스처의 너비를 Z축 길이로 인식해야 해!
            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Sprite sprite = spriteRenderer.sprite;
                Texture2D texture = sprite.texture;
                
                // [수정] 오빠가 스케일(크기)을 키웠을 수 있으니까, 스케일값도 곱해줘야 정확한 길이가 나와!
                // Rotation Y=90이면, Local X축이 World Z축이 됨 -> Scale.x를 곱해야 함.
                _textureUnitSizeZ = (texture.width / sprite.pixelsPerUnit) * transform.lossyScale.x;
                _textureUnitSizeY = (texture.height / sprite.pixelsPerUnit) * transform.lossyScale.y;
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            // 1. 카메라 이동량 계산
            Vector3 deltaMovement = _cameraTransform.position - _lastCameraPosition;

            // 2. 패럴랙스 적용 (오빠 설정: 깊이는 X, 가로는 Z, 세로는 Y)
            // X축(깊이)은 건드리지 않고, Z축(가로)과 Y축(세로)만 이동!
            transform.position += new Vector3(
                0, // X축 고정!
                deltaMovement.y * parallaxFactor.y, // 세로
                deltaMovement.z * parallaxFactor.x  // 가로 (Factor.x가 Z축 제어)
            );

            _lastCameraPosition = _cameraTransform.position;

            // 3. 무한 스크롤 (Z축 기준)
            if (infiniteHorizontal)
            {
                // 카메라Z 와 배경Z 거리 비교
                if (Mathf.Abs(_cameraTransform.position.z - transform.position.z) >= _textureUnitSizeZ)
                {
                    float offsetPositionZ = (_cameraTransform.position.z - transform.position.z) % _textureUnitSizeZ;
                    // Z축 이동 (X, Y는 유지)
                    transform.position = new Vector3(transform.position.x, transform.position.y, _cameraTransform.position.z + offsetPositionZ);
                }
            }

            if (infiniteVertical)
            {
                if (Mathf.Abs(_cameraTransform.position.y - transform.position.y) >= _textureUnitSizeY)
                {
                    float offsetPositionY = (_cameraTransform.position.y - transform.position.y) % _textureUnitSizeY;
                    transform.position = new Vector3(transform.position.x, _cameraTransform.position.y + offsetPositionY, transform.position.z);
                }
            }
        }
    }
}
