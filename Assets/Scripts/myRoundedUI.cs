using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class myRoundedUI : MonoBehaviour
{
    [SerializeField, Min(0)] private int _cornerRadius = 20;

    private Image _image;
    private Texture2D _texture;
    private Sprite _sprite;
    private int _lastRadius = -1;

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        _lastRadius = -1;
        _applyRounding();
    }

    private void OnValidate()
    {
        _image = GetComponent<Image>();
        _lastRadius = -1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) _applyRounding();
        };
#else
        _applyRounding();
#endif
    }

    private void OnDisable()
    {
        _clearGenerated();
    }

    private void OnDestroy()
    {
        _clearGenerated();
    }

    private void _applyRounding()
    {
        if (_image == null || !enabled || _lastRadius == _cornerRadius) return;
        _lastRadius = _cornerRadius;

        _clearGenerated();

        if (_cornerRadius <= 0)
        {
            _image.sprite = null;
            _image.type = Image.Type.Simple;
            return;
        }

        int _size = _cornerRadius * 2 + 2;
        _texture = new Texture2D(_size, _size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32[] _pixels = new Color32[_size * _size];
        float _min = _cornerRadius;
        float _max = _size - _cornerRadius;

        for (int _y = 0; _y < _size; _y++)
        {
            float _py = _y + 0.5f;
            float _cy = Mathf.Clamp(_py, _min, _max);

            for (int _x = 0; _x < _size; _x++)
            {
                float _px = _x + 0.5f;
                float _cx = Mathf.Clamp(_px, _min, _max);

                float _dist = Vector2.Distance(new Vector2(_px, _py), new Vector2(_cx, _cy));
                byte _alpha = (byte)(Mathf.Clamp01(_cornerRadius - _dist + 0.5f) * 255f);
                _pixels[_y * _size + _x] = new Color32(255, 255, 255, _alpha);
            }
        }

        _texture.SetPixels32(_pixels);
        _texture.Apply();

        Vector4 _border = new Vector4(_cornerRadius, _cornerRadius, _cornerRadius, _cornerRadius);
        _sprite = Sprite.Create(
            _texture,
            new Rect(0, 0, _size, _size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            _border
        );
        _sprite.hideFlags = HideFlags.HideAndDontSave;

        _image.sprite = _sprite;
        _image.type = Image.Type.Sliced;
    }

    private void _clearGenerated()
    {
        if (_sprite != null)
        {
            if (Application.isPlaying) Destroy(_sprite);
            else DestroyImmediate(_sprite);
            _sprite = null;
        }

        if (_texture != null)
        {
            if (Application.isPlaying) Destroy(_texture);
            else DestroyImmediate(_texture);
            _texture = null;
        }
    }
}
