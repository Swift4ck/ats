using UnityEngine;
using TSGame; // PlayerCore, CardData и т.д.



[RequireComponent(typeof(Collider2D))]
public class CardDrag : MonoBehaviour
{
    // назначается при создании карты в HandManager (drag.owner = owner)
    public PlayerCore owner;

    [Header("Play zone")]
    [Tooltip("Если мышь выше этой Y — считаем, что карта отпущена над игровым полем")]
    public float playZoneY = -2.5f; // настройте под вашу сцену

    private CardHover cardHover;
    private CardVisual cardVisual;
    private HandManager handManager;

    private bool isDragging = false;
    private Vector3 offset;

    private void Start()
    {
        cardHover = GetComponent<CardHover>();
        cardVisual = GetComponent<CardVisual>();
        handManager = GetComponentInParent<HandManager>();
    }

    private void OnMouseDown()
    {
        if (owner == null || !owner.isLocalPlayer) return;

        isDragging = true;
        if (cardHover != null) cardHover.SetDragging(true);

        // рассчитываем смещение, чтобы карта не "скакала"
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z) + offset;
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;
        if (cardHover != null) cardHover.SetDragging(false);

        // Если над игровой зоной — пробуем сыграть карту
        if (IsOverPlayZone())
        {
            int cardIndex = GetCardIndex();
            uint targetNetId = GetTargetNetId();
            // Если карта требует цель, но подходящая цель не найдена — возвращаем в руку
            if (cardVisual.cardData.requiresTarget && targetNetId == 0)
            {
                ReturnToHand();
                return;
            }

            if (cardIndex < 0)
            {
                // не нашли карту в локальной руке — вернём обратно
                ReturnToHand();
                return;
            }



            // Вызов команды на сервер
            owner.CmdPlayCard(cardIndex, targetNetId);

        }
        else
        {
            // не над площадкой — возвращаем в руку
            ReturnToHand();
        }
    }



    private void ReturnToHand()
    {
        if (handManager != null)
            handManager.StartCoroutine(ReturnAndReposition());



        cardHover?.ForceSnapToOriginal();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    private System.Collections.IEnumerator ReturnAndReposition()
    {
        yield return new WaitForSeconds(0.05f);
        if (handManager != null)
            yield return handManager.SmoothUpdatePositionsPublic();
    }


    private bool IsOverPlayZone()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return mouseWorld.y > playZoneY;
    }

    // Находим индекс карты в owner.hand по имени (CardData.cardName)
    // Возвращает -1 если не найдено
    private int GetCardIndex()
    {
        if (owner == null || cardVisual == null || cardVisual.cardData == null) return -1;

        string name = cardVisual.cardData.cardName;
        // безопасный перебор — SyncListString поддерживает Count и индексатор
        for (int i = 0; i < owner.hand.Count; i++)
        {
            if (owner.hand[i] == name)
                return i;
        }
        return -1;
    }


    private uint GetTargetNetId()
    {
        if (cardVisual == null || cardVisual.cardData == null) return 0;
        if (!cardVisual.cardData.requiresTarget) return 0;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(mouseWorld.x, mouseWorld.y);

        // ищем коллайдеры под курсором
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        foreach (var c in hits)
        {
            if (c == null) continue;

            // 1) Сначала ищем AvatarTarget (явная метка для аватара)
            var at = c.GetComponentInParent<TSGame.AvatarTarget>();
            if (at != null && at.GetOwnerNetId() != 0)
            {
                Debug.Log($"[CardDrag] Found AvatarTarget -> ownerNetId={at.GetOwnerNetId()}");
                return at.GetOwnerNetId();
            }

            // 2) Если AvatarTarget не найден — пробуем PlayerCore
            PlayerCore pc = c.GetComponentInParent<PlayerCore>();
            if (pc != null && pc.isAlive)
            {
                Debug.Log($"[CardDrag] Found PlayerCore directly -> netId={pc.netId}");
                return pc.netId;
            }
        }

        // ничего не найдено
        return 0;
    }


}
