using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private Color _baseColor, _offsetColor;
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private GameObject _highlight;

    private int _yPosition;

    public void Init(bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;

        _highlight.SetActive(false);
    }

    private void OnMouseEnter()
    {
        _highlight.SetActive(true);

    }

    private void OnMouseExit()
    {
        _highlight.SetActive(false);
    }
}
