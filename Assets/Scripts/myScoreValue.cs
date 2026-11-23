using UnityEngine;

public class myScoreValue : MonoBehaviour
{
    public enum myPopupOrigin
    {
        TopSurface,
        BottomSurface
    }

    [SerializeField] private int _points = 10;
    [SerializeField] private myPopupOrigin _popupOrigin = myPopupOrigin.TopSurface;

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
        if (_popupOrigin == myPopupOrigin.BottomSurface)
        {
            _position = new Vector3(_bounds.center.x, _bounds.min.y, _bounds.center.z);
            _moveDownward = true;
        }
        else
        {
            _position = new Vector3(_bounds.center.x, _bounds.max.y, _bounds.center.z);
            _moveDownward = false;
        }
    }
}
