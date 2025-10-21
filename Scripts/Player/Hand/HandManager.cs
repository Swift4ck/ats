using Mirror;
using System.Collections.Generic;
using TSGame;
using UnityEngine;
using System;
public class HandManager : MonoBehaviour
{
    [Header("Визуальные параметры руки")]
    public Transform handCenter;     // Центр руки
    public float spacingX = 1f;      // Расстояние по X
    public float spacingY = 1f;      // Смещение вниз от центра
    public float maxRotation = 25f;  // Угол поворота крайних карт

    [Header("Масштаб карт")]
    public float defaultCardScale = 0.6f;

    private PlayerCore owner;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();

    public EndTurnButton endTurnButton;

    private bool isLocalHand = false;

    // ⚡ Инициализация руки игрока
    public void Initialize(PlayerCore player)
    {
        owner = player;
        if (owner == null)
        {
            Debug.LogError("[HandManager] Initialize вызван с null-игроком!");
            return;
        }

        isLocalHand = player.isLocalPlayer;

        // рука существует только у локального игрока
        gameObject.SetActive(isLocalHand);

        if (isLocalHand)
        {
            // Сначала подписываемся на SyncList.Callback
            owner.hand.Callback += OnHandChanged;

            // Затем создаём визуалы для уже существующих карт
            foreach (string cardName in owner.hand)
            {
                AddCard(cardName);
            }

            RefreshHandUI(owner.hand);
            StartCoroutine(SmoothUpdatePositionsPublic());

            Debug.Log($"[HandManager] Initialize done. owner.isLocalPlayer={isLocalHand}. owner.hand.Count={owner.hand.Count}");

            UpdateCardPositions();
        }
    }




    private void OnHandChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        if (!isLocalHand) return;

        Debug.Log($"[HandManager] OnHandChanged → {op}, index={index}, new={newItem}");

        switch (op)
        {
            case SyncList<string>.Operation.OP_ADD:
                {
                    // Добавляем новую карту с анимацией появления
                    GameObject newCard = null;
                    AddCard(newItem, playAppearance: true);

                    // Ждём немного, пока анимация появления завершится,
                    // затем плавно перестраиваем руку
                    StartCoroutine(WaitAndSmoothUpdate(0.4f)); // 👈 новая корутина
                    break;
                }

            case SyncList<string>.Operation.OP_REMOVEAT:
            case SyncList<string>.Operation.OP_CLEAR:
            case SyncList<string>.Operation.OP_INSERT:
            case SyncList<string>.Operation.OP_SET:
                {
                    RefreshHandUI(owner.hand);
                    StartCoroutine(SmoothUpdatePositionsPublic());
                    break;
                }
        }
    }

    private System.Collections.IEnumerator WaitAndSmoothUpdate(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(SmoothUpdatePositionsPublic());
    }




    private System.Collections.IEnumerator SmoothUpdatePositions()
    {
        spawnedCards.RemoveAll(c => c == null);

        int count = spawnedCards.Count;
        if (count == 0) yield break;

        float centerIndex = (count - 1) / 2f;

        float duration = 0.25f; // время анимации
        float elapsed = 0f;

        // Сохраняем начальные позиции/углы
        Vector3[] startPos = new Vector3[count];
        Quaternion[] startRot = new Quaternion[count];
        Vector3[] targetPos = new Vector3[count];
        Quaternion[] targetRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            startPos[i] = card.transform.localPosition;
            startRot[i] = card.transform.localRotation;

            float offsetFromCenter = i - centerIndex;
            float x = offsetFromCenter * spacingX;
            float y = -Mathf.Abs(offsetFromCenter) * spacingY;
            targetPos[i] = new Vector3(x, y, 0);

            float angle = centerIndex != 0 ? -offsetFromCenter * (maxRotation / centerIndex) : 0f;
            targetRot[i] = Quaternion.Euler(0, 0, angle);

            // обновляем оригинальную позицию для CardHover
            if (card.TryGetComponent<CardHover>(out var hover))
                hover.SetOriginalPosition(targetPos[i]);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < count; i++)
            {
                var card = spawnedCards[i];
                if (card == null) continue;

                card.transform.localPosition = Vector3.Lerp(startPos[i], targetPos[i], t);
                card.transform.localRotation = Quaternion.Lerp(startRot[i], targetRot[i], t);
            }

            yield return null;
        }

        // финально зафиксировать
        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            if (card == null) continue;

            card.transform.localPosition = targetPos[i];
            card.transform.localRotation = targetRot[i];
        }
    }




    private void RemoveCardById(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        // Находим карту с этим cardId
        GameObject card = spawnedCards.Find(c =>
        {
            if (c == null) return false;
            if (c.TryGetComponent<CardVisual>(out var visual))
                return visual.cardId == cardId;
            return false;
        });

        if (card != null)
        {
            spawnedCards.Remove(card);

            if (card.TryGetComponent<CardVisual>(out var visual))
            {
                StartCoroutine(DestroyAfterUse(visual));
            }
            else
            {
                Destroy(card);
                StartCoroutine(SmoothUpdatePositionsPublic());
            }
        }
    }


    private System.Collections.IEnumerator DestroyAfterUse(CardVisual visual)
    {
        visual.PlayUse();
        yield return new WaitForSeconds(0.5f); // ждем анимацию use

        if (visual != null && spawnedCards.Contains(visual.gameObject))
            spawnedCards.Remove(visual.gameObject);

        Destroy(visual.gameObject);

        StartCoroutine(SmoothUpdatePositions());
    }






    // ⚡ Полная перестройка руки

    public void RefreshHandUI(IList<string> cards)
    {
        ClearHand();
        if (cards == null) return;

        // При инициализации — проигрываем appearance
        bool firstSetup = spawnedCards.Count == 0;

        foreach (var cardName in cards)
        {
            AddCard(cardName, playAppearance: firstSetup);
        }

        UpdateCardPositions();
    }



    // ⚡ Добавление карты
    // ⚡ Добавление карты в руку
    public void AddCard(string cardName, bool playAppearance = true)
    {
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogWarning("[HandManager] AddCard: пустое имя карты");
            return;
        }

        CardData cardData = DeckManager.Instance.GetCardByName(cardName);
        if (cardData == null)
        {
            Debug.LogError($"[HandManager] AddCard: карта {cardName} не найдена в DeckManager!");
            return;
        }

        // Создаём карту
        GameObject cardGO = Instantiate(cardData.spineData, handCenter);
        cardGO.GetComponent<CardVisual>().cardId = Guid.NewGuid().ToString();

        // Новые карты начинаем с нижней позиции (видно появление)
        if (playAppearance)
        {
            cardGO.transform.localPosition = new Vector3(0, 3f, 0); 
        }


        cardGO.transform.localScale = Vector3.one * defaultCardScale;

        // CardVisual
        if (cardGO.TryGetComponent<CardVisual>(out var visual))
        {
            visual.Init(cardData, owner, playAppearance); // 👈 обновим Init позже
        }

        // Hover
        if (cardGO.TryGetComponent<CardHover>(out var hover))
        {
            hover.SetOwner(owner);
            hover.InitScale(defaultCardScale);
            hover.UpdateHoverImmediate();
        }

        // Drag
        if (cardGO.TryGetComponent<CardDrag>(out var drag))
        {
            drag.owner = owner;
        }

        spawnedCards.Add(cardGO);

        // Если добор новых — плавно перестраиваем руку
        if (!playAppearance)
        {
            // если карта без появления — плавно перестроим
            StartCoroutine(SmoothUpdatePositionsPublic());
        }
        else
        {
            // если карта новая с анимацией — не трогаем старые карты сразу
            // (SmoothUpdate вызовется позже из WaitAndSmoothUpdate)
        }
        if (hover != null)
            hover.SetOriginalPosition(cardGO.transform.localPosition);

        Debug.Log($"[HandManager] Добавлена карта {cardName}, playAppearance={playAppearance}");
    }



    // ⚡ Удаление карты
    private void RemoveCard(string cardName)
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            var card = spawnedCards[i];
            if (card == null) continue;

            if (card.TryGetComponent<CardVisual>(out var visual) && visual.cardData != null && visual.cardData.cardName == cardName)
            {
                spawnedCards.RemoveAt(i);
                Destroy(card);
            }
        }

        spawnedCards.RemoveAll(c => c == null);
        UpdateCardPositions();
    }




    // ⚡ Очистка руки
    public void ClearHand()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        spawnedCards.Clear();
        Debug.Log("[HandManager] Рука очищена");
        UpdateCardPositions();
    }

    // ⚡ Раскладка карт веером
    public void UpdateCardPositions()
    {
        spawnedCards.RemoveAll(c => c == null);

        int count = spawnedCards.Count;
        if (count == 0) return;

        float centerIndex = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            if (card == null) continue;

            if (card.TryGetComponent<CardHover>(out var hover) && hover.IsHovered)
                continue;

            float offsetFromCenter = i - centerIndex;
            float x = offsetFromCenter * spacingX;
            float y = -Mathf.Abs(offsetFromCenter) * spacingY;
            Vector3 newPos = new Vector3(x, y, 0);

            float angle = centerIndex != 0 ? -offsetFromCenter * (maxRotation / centerIndex) : 0f;

            card.transform.localPosition = newPos;
            card.transform.localRotation = Quaternion.Euler(0, 0, angle);

            if (hover != null)
                hover.SetOriginalPosition(newPos);
        }
    }

    // 🔹 публичная корутина, чтобы другие скрипты могли её вызвать
    public System.Collections.IEnumerator SmoothUpdatePositionsPublic()
    {
        spawnedCards.RemoveAll(c => c == null);

        int count = spawnedCards.Count;
        if (count == 0) yield break;

        float centerIndex = (count - 1) / 2f;

        Vector3[] startPos = new Vector3[count];
        Quaternion[] startRot = new Quaternion[count];
        Vector3[] targetPos = new Vector3[count];
        Quaternion[] targetRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            startPos[i] = card.transform.localPosition;
            startRot[i] = card.transform.localRotation;

            float offsetFromCenter = i - centerIndex;
            float x = offsetFromCenter * spacingX;
            float y = -Mathf.Abs(offsetFromCenter) * spacingY;
            targetPos[i] = new Vector3(x, y, 0);

            float angle = centerIndex != 0 ? -offsetFromCenter * (maxRotation / centerIndex) : 0f;
            targetRot[i] = Quaternion.Euler(0, 0, angle);

            if (card.TryGetComponent<CardHover>(out var hover))
                hover.SetOriginalPosition(targetPos[i]);
        }

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < count; i++)
            {
                var card = spawnedCards[i];
                card.transform.localPosition = Vector3.Lerp(startPos[i], targetPos[i], t);
                card.transform.localRotation = Quaternion.Lerp(startRot[i], targetRot[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            var card = spawnedCards[i];
            card.transform.localPosition = targetPos[i];
            card.transform.localRotation = targetRot[i];
        }
    }


    public void ShowEndTurnButton(bool show)
    {
        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(show);
    }
    private void SyncSpawnedCardsWithHand()
    {
        if (owner == null || owner.hand == null) return;

        List<GameObject> sorted = new List<GameObject>();

        foreach (string cardName in owner.hand)
        {
            GameObject found = spawnedCards.Find(c =>
            {
                if (c == null) return false;
                if (c.TryGetComponent<CardVisual>(out var visual))
                    return visual.cardData.cardName == cardName;
                return false;
            });

            if (found == null)
            {
                // если визуала нет — пересоздаём карту
                AddCard(cardName);
                found = spawnedCards.Find(c =>
                {
                    if (c == null) return false;
                    if (c.TryGetComponent<CardVisual>(out var visual))
                        return visual.cardData.cardName == cardName;
                    return false;
                });
            }

            if (found != null)
            {
                sorted.Add(found);
            }
        }

        spawnedCards.Clear();
        spawnedCards.AddRange(sorted);
    }


    public void UseCard(GameObject cardGO)
    {
        if (cardGO == null) return;

        if (!cardGO.TryGetComponent<CardVisual>(out var visual) || visual.cardData == null)
            return;

        // 🔹 вместо поиска по имени используем уникальный cardId
        string cardId = visual.cardId;

        // Локально запускаем анимацию использования
        visual.PlayUse();

        // После анимации удаляем карту и перестраиваем руку
        StartCoroutine(RemoveAfterUseCoroutine(cardId));
    }


    private System.Collections.IEnumerator RemoveAfterUseCoroutine(string cardId)
    {
        // ждем 0.5 секунды для проигрывания анимации
        yield return new WaitForSeconds(0.5f);

        RemoveCardById(cardId);

        // Плавно перестраиваем оставшиеся карты
        yield return StartCoroutine(SmoothUpdatePositionsPublic());
    }



}
