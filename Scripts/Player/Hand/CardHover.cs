using UnityEngine;
using TSGame; // для PlayerCore

public class CardHover : MonoBehaviour
{
    // Глобально: только одна карта может быть "hovered" одновременно
    public static CardHover CurrentlyHovered = null;

    private Vector3 originalPosition;
    private bool isInHandZone = false;
    private bool isHovered = false;
    private bool isDragging = false;

    [Header("Hover settings")]
    [SerializeField] private float baseHoverHeight = 2f;
    [SerializeField] private float extraHoverHeight = 3f;
    [SerializeField] private float hoverSpeed = 8f;

    // Параметры, которые можно подбирать в инспекторе
    [Header("Hysteresis / switching")]
    [Tooltip("Во сколько раз расширяем радиус выхода для удержания hover (1.2 - 1.8).")]
    [SerializeField] private float leaveRadiusMultiplier = 1.8f;
    [Tooltip("Если расстояние до этой карты меньше чем до текущей hovered + bias, переключаем фокус.")]
    [SerializeField] private float switchDistanceBias = 0.1f;

    [Header("Scale settings")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float handZoneScale = 1.07f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Sorting settings")]
    [SerializeField] private int baseSortingOrder = 0;
    [SerializeField] private int hoveredSortingOrder = 10;
    [SerializeField] private float sortingLerpSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private float currentSortingOrder;
    private bool isHiddenByDrag = false;

    public bool IsHovered => isHovered || isDragging;

    // Components
    private Collider2D col;
    private MeshRenderer meshRenderer;

    // Владелец (Mirror)
    private PlayerCore owner;

    private bool lastHoverState = false;


    private CardVisual cardVisual;

    private void Start()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        targetScale = originalScale;

        col = GetComponent<Collider2D>();
        meshRenderer = GetComponent<MeshRenderer>();
        cardVisual = GetComponent<CardVisual>(); // ⚡ добавляем
    }

    public void SetOwner(PlayerCore player)
    {
        owner = player;
        if (col != null)
            col.enabled = owner != null && owner.isLocalPlayer; // только локальный игрок
    }

    private void Update()
    {
        if (owner == null || !owner.isLocalPlayer)
            return;

        if (isHiddenByDrag)
            return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);

        if (col == null)
        {
            isHovered = false;
            return;
        }

        bool inside = col.OverlapPoint(mousePos2D);
        float thisDist = Vector2.Distance(mousePos2D, col.bounds.center);
        float leaveThreshold = col.bounds.extents.magnitude * leaveRadiusMultiplier;

        // Если карта перетаскивается — она всегда удерживает фокус
        if (isDragging)
        {
            ClaimHover();
        }
        else
        {
            if (CurrentlyHovered == null)
            {
                // никто не удерживает — можем взять фокус если мышь внутри
                if (inside)
                    ClaimHover();
                else
                    isHovered = false;
            }
            else if (CurrentlyHovered == this)
            {
                // мы в фокусе — проверяем выход с гистерезисом
                if (inside || thisDist <= leaveThreshold)
                {
                    isHovered = true;
                }
                else
                {
                    ReleaseHover();
                }
            }
            else
            {
                // другая карта в фокусе
                var other = CurrentlyHovered;
                bool otherInside = other != null && other.col != null && other.col.OverlapPoint(mousePos2D);

                if (!otherInside && inside)
                {
                    // если текущая фокусная карта уже не содержит курсор, можем захватить
                    ClaimHover();
                }
                else if (inside && otherInside)
                {
                    // оба "видят" курсор — переключаем на ближайшую (с небольшим bias)
                    float otherDist = Vector2.Distance(mousePos2D, other.col.bounds.center);
                    if (thisDist + switchDistanceBias < otherDist)
                        ClaimHover();
                    else
                        isHovered = false;
                }
                else
                {
                    // не внутри текущей — и не внутри этой
                    isHovered = false;
                }

                // --- Обновление визуала только при изменении состояния hover ---
                if (cardVisual != null && lastHoverState != isHovered)
                {
                    if (isHovered)
                        cardVisual.OnHoverEnter();
                    else
                        cardVisual.OnHoverExit();

                    lastHoverState = isHovered;
                }


            }
        }

        isInHandZone = IsMouseInHandArea(mousePos2D);

        // --- Движение и масштаб ---
        if (!isDragging)
        {
            Vector3 basePosition = originalPosition;
            Vector3 targetPos = basePosition;

            if (isHovered)
                targetPos = basePosition + Vector3.up * (baseHoverHeight + extraHoverHeight);
            else if (isInHandZone)
                targetPos = basePosition + Vector3.up * baseHoverHeight;

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * hoverSpeed);

            if (isHovered)
                targetScale = originalScale * hoverScale;
            else if (isInHandZone)
                targetScale = originalScale * handZoneScale;
            else
                targetScale = originalScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        // --- Слой отрисовки ---
        if (meshRenderer != null)
        {
            int targetOrder = isHovered ? hoveredSortingOrder : baseSortingOrder;
            currentSortingOrder = Mathf.Lerp(currentSortingOrder, targetOrder, Time.deltaTime * sortingLerpSpeed);
            meshRenderer.sortingOrder = Mathf.RoundToInt(currentSortingOrder);
        }
    }

    private void ClaimHover()
    {
        if (CurrentlyHovered != null && CurrentlyHovered != this)
            CurrentlyHovered.isHovered = false;

        CurrentlyHovered = this;
        isHovered = true;
    }

    private void ReleaseHover()
    {
        if (CurrentlyHovered == this)
            CurrentlyHovered = null;
        isHovered = false;
    }

    private bool IsMouseInHandArea(Vector2 mousePos)
    {
        float handZoneY = -3.5f;
        return mousePos.y <= handZoneY + 2f;
    }

    // --- Drag API ---
    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        if (dragging)
        {
            // во время drag гарантируем, что эта карта в фокусе
            ClaimHover();
        }
        else
        {
            ResetScale();
            // не делаем немедленного Release — пусть Update решит по позиции мыши
        }
    }

    public void SetHiddenByDrag(bool hidden)
    {
        isHiddenByDrag = hidden;
    }

    public void ResetScale()
    {
        targetScale = originalScale;
    }

    public void SetScaleMultiplier(float multiplier)
    {
        targetScale = originalScale * multiplier;
    }

    public void SetOriginalPosition(Vector3 newPos)
    {
        originalPosition = newPos;
    }

    public void ForceSnapToOriginal()
    {
        transform.localPosition = originalPosition;
    }

    public void ForceSetPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        transform.localPosition = newPos;
    }

    public void InitScale(float baseScale)
    {
        originalScale = Vector3.one * baseScale;
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void OnDestroy()
    {
        // очистить статическую ссылку если уничтожаемся
        if (CurrentlyHovered == this) CurrentlyHovered = null;
    }

    public void UpdateHoverImmediate()
    {
        if (cardVisual == null) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        bool inside = col != null && col.OverlapPoint(mousePos2D);

        bool shouldHover = inside;
        if (shouldHover != isHovered)
        {
            isHovered = shouldHover;
            lastHoverState = !isHovered; // чтобы триггер сработил
        }
    }

    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }


}
