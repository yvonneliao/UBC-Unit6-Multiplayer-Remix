using System.Collections;
using UnityEngine;

public class myRingGlow : MonoBehaviour
{
    [SerializeField] private Color _emissionColor = Color.white;
    [SerializeField] private float _emissionIntensity = 3f;
    [SerializeField] private float _glowDuration = 1f;

    private Material _material;
    private Coroutine _glowCoroutine;

    private void Awake()
    {
        Renderer _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _setEmission(false);
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    private void OnTriggerEnter(Collider _other)
    {
        if (_material == null || _other == null)
            return;

        NetworkStats _stats = _other.GetComponentInParent<NetworkStats>();
        if (_stats == null || !_stats.IsAlive.Value)
            return;

        if (_glowCoroutine != null)
            StopCoroutine(_glowCoroutine);

        _glowCoroutine = StartCoroutine(_glowRoutine());
    }

    private IEnumerator _glowRoutine()
    {
        _setEmission(true);
        yield return new WaitForSeconds(_glowDuration);
        _setEmission(false);
        _glowCoroutine = null;
    }

    private void _setEmission(bool _enabled)
    {
        if (_material == null)
            return;

        if (_enabled)
        {
            _material.EnableKeyword("_EMISSION");
            _material.SetColor("_EmissionColor", _emissionColor * _emissionIntensity);
        }
        else
        {
            _material.SetColor("_EmissionColor", Color.black);
            _material.DisableKeyword("_EMISSION");
        }
    }
}
