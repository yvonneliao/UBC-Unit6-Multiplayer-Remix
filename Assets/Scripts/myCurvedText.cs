using TMPro;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class myCurvedText : MonoBehaviour
{
    [SerializeField] private AnimationCurve _curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f));

    [SerializeField] private float _curveScale = 20f;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();

        _applyCurve();
    }

    private void LateUpdate()
    {
        if (_text == null)
            return;

        _applyCurve();
    }

    private void _applyCurve()
    {
        _text.ForceMeshUpdate();
        TMP_TextInfo _textInfo = _text.textInfo;
        int _charCount = _textInfo.characterCount;
        if (_charCount == 0)
            return;

        float _minX = _text.bounds.min.x;
        float _maxX = _text.bounds.max.x;
        float _width = _maxX - _minX;
        if (_width < 0.0001f)
            return;

        for (int _i = 0; _i < _charCount; _i++)
        {
            TMP_CharacterInfo _charInfo = _textInfo.characterInfo[_i];
            if (!_charInfo.isVisible)
                continue;

            int _vertexIndex = _charInfo.vertexIndex;
            int _materialIndex = _charInfo.materialReferenceIndex;
            Vector3[] _vertices = _textInfo.meshInfo[_materialIndex].vertices;

            float _midX = (_vertices[_vertexIndex + 0].x + _vertices[_vertexIndex + 2].x) * 0.5f;
            Vector3 _pivot = new Vector3(_midX, _charInfo.baseLine, 0f);

            float _t = Mathf.InverseLerp(_minX, _maxX, _midX);
            float _yOffset = _curve.Evaluate(_t) * _curveScale;

            float _dt = 0.01f;
            float _yLeft = _curve.Evaluate(_t - _dt) * _curveScale;
            float _yRight = _curve.Evaluate(_t + _dt) * _curveScale;
            float _dx = (_dt * 2f) * _width;
            float _angle = Mathf.Atan2(_yRight - _yLeft, _dx) * Mathf.Rad2Deg;

            Quaternion _rotation = Quaternion.Euler(0f, 0f, _angle);
            Vector3 _offset = new Vector3(0f, _yOffset, 0f);

            for (int _v = 0; _v < 4; _v++)
            {
                Vector3 _local = _vertices[_vertexIndex + _v] - _pivot;
                _vertices[_vertexIndex + _v] = (_rotation * _local) + _pivot + _offset;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
