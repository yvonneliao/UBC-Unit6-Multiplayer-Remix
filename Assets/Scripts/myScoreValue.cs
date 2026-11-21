using UnityEngine;

public class myScoreValue : MonoBehaviour
{
    [SerializeField] private int _points = 10;

    public int _getPoints()
    {
        return _points;
    }
}
