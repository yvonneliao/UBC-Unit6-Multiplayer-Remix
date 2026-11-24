using TMPro;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(TMP_Text))]
public class myTextWave : MonoBehaviour
{
    [SerializeField] private float _startDelay = 1f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _waveSpeed = 5f;
    [SerializeField] private float _waveWidth = 1.6f;
    [SerializeField] private float _loopPause = 2f;

    private TMP_Text _text;
    private myCurvedText _curvedText;
    private float _startTime;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _curvedText = GetComponent<myCurvedText>();
    }

    private void OnEnable()
    {
        _startTime = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        if (_text == null)
            return;

        float _elapsed = Time.unscaledTime - _startTime - _startDelay;
        if (_elapsed < 0f)
            return;

        if (_curvedText == null)
            _text.ForceMeshUpdate();

        TMP_TextInfo _textInfo = _text.textInfo;
        int _charCount = _textInfo.characterCount;
        if (_charCount == 0)
            return;

        int _visibleCount = 0;
        for (int _i = 0; _i < _charCount; _i++)
        {
            if (_textInfo.characterInfo[_i].isVisible)
                _visibleCount++;
        }

        if (_visibleCount == 0)
            return;

        float _cycleLength = _visibleCount + _waveWidth * 2f + _loopPause;
        float _wavePos = (_elapsed * _waveSpeed) % _cycleLength - _waveWidth;

        int _visibleIndex = 0;
        for (int _i = 0; _i < _charCount; _i++)
        {
            TMP_CharacterInfo _charInfo = _textInfo.characterInfo[_i];
            if (!_charInfo.isVisible)
                continue;

            float _dist = Mathf.Abs(_wavePos - _visibleIndex);
            _visibleIndex++;

            if (_dist >= _waveWidth)
                continue;

            float _weight = 0.5f * (1f + Mathf.Cos(Mathf.PI * (_dist / _waveWidth)));
            float _scale = Mathf.Lerp(1f, _maxScale, _weight);

            int _vertexIndex = _charInfo.vertexIndex;
            int _materialIndex = _charInfo.materialReferenceIndex;
            Vector3[] _vertices = _textInfo.meshInfo[_materialIndex].vertices;

            Vector3 _center = (_vertices[_vertexIndex + 0] + _vertices[_vertexIndex + 2]) * 0.5f;
            for (int _v = 0; _v < 4; _v++)
            {
                _vertices[_vertexIndex + _v] = _center + (_vertices[_vertexIndex + _v] - _center) * _scale;
            }
        }

        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
