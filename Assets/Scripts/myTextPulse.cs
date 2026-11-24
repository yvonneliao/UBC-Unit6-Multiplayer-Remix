using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class myTextPulse : MonoBehaviour
{
    [SerializeField] private float _minAlpha = 0.3f;
    [SerializeField] private float _maxAlpha = 1f;
    [SerializeField] private float _pulseSpeed = 3f;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (_text == null)
            return;

        float _t = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f;
        Color _color = _text.color;
        _color.a = Mathf.Lerp(_minAlpha, _maxAlpha, _t);
        _text.color = _color;
    }
}
