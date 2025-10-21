using System.Collections.Generic;
using TSGame;
using UnityEngine;

public static class CardEffects
{
    // Применение к одной цели (или null для карт без цели)
    public static void Apply(CardData card, PlayerCore caster, PlayerCore target)
    {
        switch (card.effectType)
        {
            case EffectType.Damage:
                target?.ServerTakeDamage(card.effectValue);
                break;

            case EffectType.Heal:
                target?.ServerHeal(card.effectValue);
                break;

            case EffectType.Armor:
                target?.ServerAddArmor(card.effectValue);
                break;

            case EffectType.Draw:
                DeckManager.Instance.ServerDraw(caster, card.effectValue);
                break;

            case EffectType.ManaUp:
                caster.ServerAddMana(card.effectValue);
                break;

            default:
                Debug.LogWarning($"Эффект {card.effectType} не реализован!");
                break;
        }
    }

    // Применение к списку целей
    public static void Apply(CardData card, PlayerCore caster, List<PlayerCore> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            // карты без цели
            Apply(card, caster, (PlayerCore)null);
            return;
        }

        foreach (var target in targets)
        {
            Apply(card, caster, target);
        }
    }
}
