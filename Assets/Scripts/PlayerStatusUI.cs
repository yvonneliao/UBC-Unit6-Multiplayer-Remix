using System.Collections;
using UnityEngine;

using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

public class PlayerStatusUI : MonoBehaviour
{
    [SerializeField] Image healthbar;
    [SerializeField] TMP_Text healthText;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text livesText;
    [SerializeField] TMP_Text scoreText;

    [Header("Score Pulse")]
    [SerializeField] private float _scorePulseScale = 1.7f;
    [SerializeField] private float _scorePulseDuration = 0.3f;

    private NetworkStats stats;
    private NetworkedPlayerController _controller;
    private int _lastScore;
    private bool _hasLastScore;
    private Vector3 _scoreBaseScale = Vector3.one;
    private bool _hasBaseScale;
    private Coroutine _scorePulseRoutine;

    private void Awake()
    {
        if (scoreText != null)
        {
            _scoreBaseScale = scoreText.rectTransform.localScale;
            _hasBaseScale = true;
        }
    }

    public void Bind(NetworkStats statsScript)
    {
        Unbind();

        stats = statsScript;
        if (stats != null)
        {
            nameText.text = $"Player {stats.OwnerClientId}";
            stats.CurrentHealth.OnValueChanged += OnHealthChanged;      
            stats.Lives.OnValueChanged += OnLivesChanged;
            stats.Score.OnValueChanged += OnScoreChanged;

            if (stats.TryGetComponent(out _controller))
            {
                _controller._onPlayerColorChanged += _setHealthColor;
                if (_controller._tryGetPlayerColor(out Color _color))
                {
                    _setHealthColor(_color);
                }
            }
        }        

        Refresh();
    }

    public void Unbind()
    {
        if (stats != null)
        {
            nameText.text = " - ";
            stats.CurrentHealth.OnValueChanged -= OnHealthChanged;
            stats.Lives.OnValueChanged -= OnLivesChanged;
            stats.Score.OnValueChanged -= OnScoreChanged;
        }

        if (_controller != null)
        {
            _controller._onPlayerColorChanged -= _setHealthColor;
            _controller = null;
        }

        _hasLastScore = false;
        if (_scorePulseRoutine != null)
        {
            StopCoroutine(_scorePulseRoutine);
            _scorePulseRoutine = null;
        }

        if (scoreText != null && _hasBaseScale)
        {
            scoreText.rectTransform.localScale = _scoreBaseScale;
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        Debug.Log("Health Changed!");
        Refresh();
    }

    private void OnLivesChanged(int oldValue, int newValue)
    {
        Debug.Log("Lives Changed!");
        Refresh();
    }

    private void OnScoreChanged(int oldValue, int newValue)
    {

        Debug.Log("Score Changed!");
        Refresh();
    }

    public void Refresh()
    {
        if (stats != null)
        {
            nameText.text = $"Player {stats.OwnerClientId}";
            healthText.text = $"{stats.CurrentHealth.Value} / {stats.MaxHealth}";
            healthbar.fillAmount = stats.CurrentHealth.Value / stats.MaxHealth;

            livesText.text = $"{stats.Lives.Value}";
            _updateScoreDisplay(stats.Score.Value);
        }
    }

    public void UpdateContent(string name, int health, int lives, int score)
    {
        nameText.text = $"Player {name}";
        healthText.text = health + " / 100";
        healthbar.fillAmount = (float)health / 100f;
        livesText.text = lives.ToString();
        _updateScoreDisplay(score);
    }

    private void _updateScoreDisplay(int _newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = _newScore.ToString();
        }

        if (!_hasLastScore)
        {
            _lastScore = _newScore;
            _hasLastScore = true;
            return;
        }

        if (_newScore > _lastScore)
        {
            _triggerScorePulse();
        }

        _lastScore = _newScore;
    }

    private void _triggerScorePulse()
    {
        if (scoreText == null || !gameObject.activeInHierarchy)
            return;

        if (!_hasBaseScale)
        {
            _scoreBaseScale = scoreText.rectTransform.localScale;
            _hasBaseScale = true;
        }

        if (_scorePulseRoutine != null)
        {
            StopCoroutine(_scorePulseRoutine);
        }

        _scorePulseRoutine = StartCoroutine(_pulseScoreRoutine());
    }

    private IEnumerator _pulseScoreRoutine()
    {
        RectTransform _rect = scoreText.rectTransform;
        Vector3 _peakScale = _scoreBaseScale * _scorePulseScale;
        float _duration = Mathf.Max(0.01f, _scorePulseDuration);

        float _elapsed = 0f;
        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float _t = Mathf.Clamp01(_elapsed / _duration);
            _rect.localScale = Vector3.Lerp(_scoreBaseScale, _peakScale, _t);
            yield return null;
        }

        _elapsed = 0f;
        while (_elapsed < _duration)
        {
            _elapsed += Time.deltaTime;
            float _t = Mathf.Clamp01(_elapsed / _duration);
            _rect.localScale = Vector3.Lerp(_peakScale, _scoreBaseScale, _t);
            yield return null;
        }

        _rect.localScale = _scoreBaseScale;
        _scorePulseRoutine = null;
    }

    public void _setHealthColor(Color _color)
    {
        if (healthbar != null)
        {
            healthbar.color = _color;
        }
    }

    private void OnDestroy()
    {
        Unbind();
    }

}
