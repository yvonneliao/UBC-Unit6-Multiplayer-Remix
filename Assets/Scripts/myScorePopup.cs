using TMPro;
using UnityEngine;

public class myScorePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _floatSpeed = 1.5f;
    [SerializeField] private float _duration = 0.9f;
    [SerializeField] private float _rotateAngle = 100f;
    [SerializeField] private Color _positiveColor = new Color(0.2f, 0.95f, 0.35f, 1f);
    [SerializeField] private Color _negativeColor = new Color(1f, 0.25f, 0.25f, 1f);

    private Vector3 _moveDirection = Vector3.up;
    private float _elapsed;
    private Color _baseColor;
    private Camera _mainCamera;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponentInChildren<TMP_Text>();

        _mainCamera = Camera.main;
    }

    public void _initialize(int _points, bool _moveDownward = false)
    {
        if (_text == null)
            _text = GetComponentInChildren<TMP_Text>();

        _moveDirection = _moveDownward ? Vector3.down : Vector3.up;
        _elapsed = 0f;

        if (_text != null)
        {
            _text.text = _points >= 0 ? $"+{_points}" : _points.ToString();
            _baseColor = _points >= 0 ? _positiveColor : _negativeColor;
            _text.color = _baseColor;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float _progress = Mathf.Clamp01(_elapsed / _duration);

        transform.position += _moveDirection * (_floatSpeed * Time.deltaTime);

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        Quaternion _spin = Quaternion.Euler(0f, 0f, _rotateAngle * _progress);
        transform.rotation = _mainCamera != null
            ? _mainCamera.transform.rotation * _spin
            : _spin;

        if (_text != null)
        {
            float _alpha = Mathf.Clamp01(1f - _progress);
            _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _alpha);
        }

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
