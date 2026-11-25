using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class myButtonSfx : MonoBehaviour
{
    [SerializeField] private AudioClip _clickSfx;
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(_playClickSfx);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(_playClickSfx);
        }
    }

    private void _playClickSfx()
    {
        if (_clickSfx == null)
            return;

        GameObject _sfxObj = new GameObject("ButtonClickSfx");
        AudioSource _source = _sfxObj.AddComponent<AudioSource>();
        _source.clip = _clickSfx;
        _source.volume = _sfxVolume;
        _source.spatialBlend = 0f;
        _source.Play();
        Destroy(_sfxObj, _clickSfx.length);
    }
}
