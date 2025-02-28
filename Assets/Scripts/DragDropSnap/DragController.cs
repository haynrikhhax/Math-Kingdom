using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DragController : MonoBehaviourSingleton<DragController>
{
    public Dragable LastDraged => _lastDraged;
    public int stoneBrickCount = 0;
    public int goldBrickCount = 0;
    private bool _isDragActive = false;
    private Vector2 _screenPosition;
    private Vector3 _worldPosition;
    private Dragable _lastDraged;
    private GameObject _draggedDuplicate;
    private TextMeshProUGUI stoneBrick;
    private TextMeshProUGUI goldBrick;
    private string brickType;
    [SerializeField] private LevelMetadata levelMetadata;
    [SerializeField] public string gridType;
    [SerializeField] private GameObject stoneBrickObject;
    [SerializeField] private GameObject goldBrickObject;
    [SerializeField] private LayerMask TransparentBoxLayer;
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private AudioClip audioClip;
    private AudioSource audioSource;

    protected void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = audioClip;
        stoneBrick = stoneBrickObject.GetComponent<TextMeshProUGUI>();
        goldBrick = goldBrickObject.GetComponent<TextMeshProUGUI>();

        DragController[] controllers = FindObjectsOfType<DragController>();
        if (controllers.Length > 1)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (_isDragActive)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                Drop();
                return;
            }
        }
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            _screenPosition = new Vector2(mousePos.x, mousePos.y);
        }
        else if (Input.touchCount > 0)
        {
            _screenPosition = Input.GetTouch(0).position;
        }
        else
        {
            return;
        }

        _worldPosition = Camera.main.ScreenToWorldPoint(_screenPosition);

        if (_isDragActive)
        {
            Drag();
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(_worldPosition, Vector2.zero);
            if (hit.collider != null)
            {
                Dragable dragable = hit.transform.gameObject.GetComponent<Dragable>();
                if (dragable != null)
                {
                    _lastDraged = dragable;
                    InitDrag();
                }
            }
        }
    }

    void InitDrag()
    {
        _lastDraged.gridType = gridType;
        EventsManager.Instance.OnStartDrag.Invoke();
        _lastDraged.LastPosition = _lastDraged.transform.position;
        UpdateDragStatus(true);
    }

    void Drag()
    {
        _lastDraged.transform.position = new Vector3(_worldPosition.x, _worldPosition.y, 5);
    }

    void Drop()
    {
        Vector3 mousePos = Input.mousePosition;
        _screenPosition = new Vector2(mousePos.x, mousePos.y);
        RaycastHit2D hit = Physics2D.Raycast(_worldPosition, Vector2.zero, 20, TransparentBoxLayer);
        Sprite newSprite;

        if (hit.collider != null && hit.collider.gameObject.CompareTag("DropValid"))
        {
            GameObject transparentBox = hit.collider.gameObject;
            if (transparentBox.TryGetComponent<TransparentBox>(out var component))
            {
                SpriteRenderer spriteRenderer = transparentBox.GetComponent<SpriteRenderer>();
                newSprite = Resources.Load<Sprite>("Sprites/TransparentBox");

                if (component.AssignedBrickType != _lastDraged.BrickType)
                {
                    switch (component.AssignedBrickType)
                    {
                        case BrickTypeEnum.Stone:
                            stoneBrickCount--;
                            break;
                        case BrickTypeEnum.Gold:
                            goldBrickCount--;
                            break;
                    }

                    if (_lastDraged.BrickType == BrickTypeEnum.Stone)
                    {
                        newSprite = Resources.Load<Sprite>("Sprites/StoneTileConstruction");
                        stoneBrickCount += 1;
                    }
                    else if (_lastDraged.BrickType == BrickTypeEnum.Gold)
                    {
                        newSprite = Resources.Load<Sprite>("Sprites/GoldTileConstruction");
                        goldBrickCount += 1;
                    }
                    stoneBrick.text = stoneBrickCount.ToString();
                    goldBrick.text = goldBrickCount.ToString();
                }

                PlayAudio();
                PlayParticle();
                spriteRenderer.sprite = newSprite;
                component.AssignedBrickType = _lastDraged.BrickType;
            }

            EventsManager.Instance.OnEndDrag.Invoke();
            UpdateDragStatus(false);
            Destroy(_lastDraged.gameObject);
        }
    }

    public bool WinCondition()
    {
        var currentLevelIndex = int.Parse(SceneManager.GetActiveScene().name.Split(' ').Last());
        var levelInfo = levelMetadata.LevelImages.First(x => x.Id == currentLevelIndex);
        return levelInfo.StoneCount == stoneBrickCount && levelInfo.GoldCount == goldBrickCount;
    }

    void UpdateDragStatus(bool isDragging)
    {
        _isDragActive = _lastDraged.IsDraging = isDragging;
        _lastDraged.gameObject.layer = isDragging ? Layer.Draging : Layer.Default;
    }

    private void UpdateBrickType(string newBrickType)
    {
        brickType = newBrickType;
    }

    public void PlayAudio()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    public void PlayParticle()
    {
        if (particleSystem != null)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            InstantiateParticleSystem(mousePosition);
        }
    }
    private void InstantiateParticleSystem(Vector3 position)
    {
        if (particleSystem != null)
        {
            ParticleSystem particleSystemInstance = Instantiate(particleSystem, position, Quaternion.identity);
            particleSystemInstance.Play();
        }
    }
}
