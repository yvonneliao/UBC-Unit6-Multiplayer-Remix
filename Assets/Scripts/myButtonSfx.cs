using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class myButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip _hoverSfx;
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

    public void OnPointerEnter(PointerEventData _eventData)
    {
        if (_button != null && !_button.IsInteractable())
            return;

        _play2DSfx(_hoverSfx, "ButtonHoverSfx");
    }

    private void _playClickSfx()
    {
        _play2DSfx(_clickSfx, "ButtonClickSfx");
    }

    private void _play2DSfx(AudioClip _clip, string _name)
    {
        if (_clip == null)
            return;

        GameObject _sfxObj = new GameObject(_name);
        AudioSource _source = _sfxObj.AddComponent<AudioSource>();
        _source.clip = _clip;
        _source.volume = _sfxVolume;
        _source.spatialBlend = 0f;
        _source.Play();
        Destroy(_sfxObj, _clip.length);
    }
}
