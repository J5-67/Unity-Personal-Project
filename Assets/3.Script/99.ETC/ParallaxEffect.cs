using UnityEngine;

namespace Environment
{
    public class ParallaxEffect : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Vector2 parallaxFactor;

        [Header("Infinite Scrolling")]
        [SerializeField] private bool infiniteHorizontal = true;
        [SerializeField] private bool infiniteVertical = false;

        private Transform _cameraTransform;
        private Vector3 _lastCameraPosition;
        private float _textureUnitSizeZ;
        private float _textureUnitSizeY;

        private void Start()
        {
            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
                _lastCameraPosition = _cameraTransform.position;
            }
            else
            {

            }

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Sprite sprite = spriteRenderer.sprite;
                Texture2D texture = sprite.texture;

                _textureUnitSizeZ = (texture.width / sprite.pixelsPerUnit) * transform.lossyScale.x;
                _textureUnitSizeY = (texture.height / sprite.pixelsPerUnit) * transform.lossyScale.y;
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null) return;

            Vector3 deltaMovement = _cameraTransform.position - _lastCameraPosition;

            transform.position += new Vector3(
                0,
                deltaMovement.y * parallaxFactor.y,
                deltaMovement.z * parallaxFactor.x
            );

            _lastCameraPosition = _cameraTransform.position;

            if (infiniteHorizontal)
            {
                if (Mathf.Abs(_cameraTransform.position.z - transform.position.z) >= _textureUnitSizeZ)
                {
                    float offsetPositionZ = (_cameraTransform.position.z - transform.position.z) % _textureUnitSizeZ;
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
