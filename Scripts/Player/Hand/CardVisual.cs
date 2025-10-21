using Spine;
using Spine.Unity;
using TSGame;
using UnityEngine;

public class CardVisual : MonoBehaviour
{
    [Header("References")]
    public SkeletonAnimation skeletonAnimation;
    [HideInInspector] public CardData cardData;
    [HideInInspector] public PlayerCore owner;
    [HideInInspector] public string cardId; // уникальный идентификатор карты


    private Spine.AnimationState animState;

    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation != null)
            animState = skeletonAnimation.AnimationState;
    }

    // ⚡ Инициализация карты
    public void Init(CardData data, PlayerCore cardOwner, bool playAppearance = true)
    {
        cardData = data;
        owner = cardOwner;

        if (skeletonAnimation == null)
        {
            Debug.LogError("CardVisual: Нет SkeletonAnimation на карте!");
            return;
        }

        if (playAppearance && !string.IsNullOrEmpty(cardData.animations.appearanceAnimation))
        {
            var entry = animState.SetAnimation(0, cardData.animations.appearanceAnimation, false);
            entry.Complete += OnAppearComplete;
        }
        else
        {
            SetIdleFrame();
        }
    }


    private void OnAppearComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= OnAppearComplete;
        SetIdleFrame();

        // После завершения анимации плавно поднимаем карту в веер
       /* if (TryGetComponent<CardHover>(out var hover))
        {
            StartCoroutine(MoveToOriginalPosition());
        }
       */
    }


    // ⚡ Idle freeze (первый кадр)
    public void SetIdleFrame()
    {
        if (animState == null || string.IsNullOrEmpty(cardData.animations.idleAnimation)) return;

        // Принудительно убираем все предыдущие анимации на этом треке
        animState.ClearTrack(0);

        var entry = animState.SetAnimation(0, cardData.animations.idleAnimation, true);
        if (entry != null)
        {
            entry.TrackTime = 0f;
            animState.Update(0f);
            animState.Apply(skeletonAnimation.Skeleton);
            animState.TimeScale = 0f;
        }
    }



    // Hover → idle в loop
    public void OnHoverEnter()
    {
        if (animState == null || string.IsNullOrEmpty(cardData.animations.idleAnimation)) return;

        var entry = animState.GetCurrent(0);
        if (entry == null || entry.Animation.Name != cardData.animations.idleAnimation)
            SetIdleFrame(); // создаём трек заново

        animState.TimeScale = 1f;
    }


    public void OnHoverExit()
    {
        // возвращаем на первый кадр и замораживаем
        var entry = animState.GetCurrent(0);
        if (entry != null && entry.Animation != null && entry.Animation.Name == cardData.animations.idleAnimation)
            entry.TrackTime = 0f;

        animState.Update(0f);
        animState.Apply(skeletonAnimation.Skeleton);
        animState.TimeScale = 0f;
    }


    // Общий метод проигрывания
    public void PlayAnimation(string animationName, bool loop)
    {
        if (animState == null || string.IsNullOrEmpty(animationName)) return;
        animState.SetAnimation(0, animationName, loop);
    }

    // ⚡ Использование карты
    public void PlayUse()
    {
        if (!string.IsNullOrEmpty(cardData.animations.useAnimation))
        {
            var entry = animState.SetAnimation(0, cardData.animations.useAnimation, false);
            // entry.Complete += OnUseComplete; // убираем прямое Destroy
        }
    }


    private void OnUseComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= OnUseComplete;
        Destroy(gameObject); // карта исчезает визуально
    }

    // ⚡ Сброс карты
    public void PlayDiscard()
    {
        if (!string.IsNullOrEmpty(cardData.animations.discardAnimation))
        {
            var entry = animState.SetAnimation(0, cardData.animations.discardAnimation, false);
            entry.Complete += OnDiscardComplete;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDiscardComplete(TrackEntry trackEntry)
    {
        trackEntry.Complete -= OnDiscardComplete;
        Destroy(gameObject); // карта исчезает
    }

   /* private System.Collections.IEnumerator MoveToOriginalPosition()
    {
        if (!TryGetComponent<CardHover>(out var hover))
            yield break;

        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = hover.GetOriginalPosition(); // оригинальная позиция из HandManager
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = transform.localRotation; // можно оставить как есть

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        transform.localPosition = targetPos;
    }
   */

}
