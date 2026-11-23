using UnityEngine;

public class myScoreValue : MonoBehaviour
{
    public enum myPopupOrigin
    {
        TopSurface,
        BottomSurface,
        CenterOffset
    }

    [SerializeField] private int _points = 10;
    [SerializeField] private myPopupOrigin _popupOrigin = myPopupOrigin.TopSurface;
    [Range(-1f, 1f)]
    [SerializeField] private float _centerOffsetRatio = 0.6f;

    public int _getPoints()
    {
        return _points;
    }

    public void _getPopupData(Collider _col, out Vector3 _position, out bool _moveDownward)
    {
        if (_col == null)
        {
            _position = transform.position;
            _moveDownward = _popupOrigin == myPopupOrigin.BottomSurface;
            return;
        }

        Bounds _bounds = _col.bounds;
        switch (_popupOrigin)
        {
            case myPopupOrigin.BottomSurface:
                _position = new Vector3(_bounds.center.x, _bounds.min.y, _bounds.center.z);
                _moveDownward = true;
                break;

            case myPopupOrigin.CenterOffset:
                _position = new Vector3(
                    _bounds.center.x,
                    _bounds.center.y + _bounds.extents.y * _centerOffsetRatio,
                    _bounds.center.z);
                _moveDownward = false;
                break;

            default:
                _position = new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
                _moveDownward = false;
                break;
        }
    }
}
