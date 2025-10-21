using UnityEngine;



[System.Serializable]
public class CardAnimations
{
    [Header("Анимации карты")]
    public string appearanceAnimation; // при появлении в руке
    public string idleAnimation;       // idle (цикл при наведении, freeze без наведения)
    public string useAnimation;        // при использовании
    public string discardAnimation;    // при сбросе (если нужно отдельно)
}



public enum EffectType
{
    Damage,
    Heal,
    Armor,
    Draw,
    ManaUp,
    // Позже добавим сложные эффекты: SecondLife, MirrorDamage и т.д.
}

public enum TargetType
{
    Self,               // 1. на себя
    OtherPlayer,        // 2. на других игроков
    AnyPlayer,          // 3. на себя или других игроков
    AllPlayers,         // 4. массово на всех игроков
    NoTarget            // 5. без цели (карта сама срабатывает)
}




[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;         // Название карты
    public int manaCost;            // Стоимость маны
    public EffectType effectType;   // Тип эффекта
    public int effectValue;         // Величина эффекта (урон, хил, броня, добор, мана)
    public TargetType targetType;   //цель
    public Sprite icon;             // Иконка карты
    public GameObject spineData;    // Prefab Spine-анимации карты
    public CardAnimations animations;

    public bool requiresTarget
    {
        get
        {
            return targetType == TargetType.Self
                || targetType == TargetType.OtherPlayer
                || targetType == TargetType.AnyPlayer;
        }
    }

}

   