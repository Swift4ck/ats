using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace TSGame
{
    public class DeckManager : NetworkBehaviour
    {
        public static DeckManager Instance; // singleton

        [Header("Все CardData (ScriptableObjects)")]
        public List<CardData> allCardsData = new List<CardData>();

        private List<string> deck = new List<string>();   // идентификаторы карт
        private List<string> discard = new List<string>();

        private void Awake()
        {
            Instance = this;
        }

        // Добавить в DeckManager (внутри класса)
        public override void OnStartServer()
        {
            base.OnStartServer();

            // Если в allCardsData есть элементы — инициализируем deck их именами
            if (allCardsData != null && allCardsData.Count > 0)
            {
                List<string> names = new List<string>(allCardsData.Count);
                foreach (var cd in allCardsData)
                {
                    if (cd != null && !string.IsNullOrEmpty(cd.cardName))
                        names.Add(cd.cardName);
                }

                // Инициализация внутренней колоды
                InitDeck(names);
                Debug.Log($"[DeckManager] OnStartServer: инициализирована колода ({deckCount()} карт)");
            }
            else
            {
                Debug.LogWarning("[DeckManager] OnStartServer: allCardsData пустой — колода не инициализирована");
            }

            int deckCount() { return deck?.Count ?? 0; }
        }


        [Server]
        public void InitDeck(List<string> allCards)
        {
            deck.Clear();
            discard.Clear();
            deck.AddRange(allCards);
            Shuffle(deck);
        }

        [Server]
        public string DrawCard()
        {
            if (deck.Count == 0)
            {
                Reshuffle();
            }

            if (deck.Count == 0)
            {
                Debug.LogWarning("[DeckManager] Нет карт даже после пересборки!");
                return null; // игра реально без карт
            }

            string card = deck[0];
            deck.RemoveAt(0);

            Debug.Log($"[DeckManager] Взята карта {card}. В колоде {deck.Count}, в сбросе {discard.Count}");
            return card;
        }

        [Server]
        public void DiscardCard(string card)
        {
            if (!string.IsNullOrEmpty(card))
            {
                discard.Add(card);
                Debug.Log($"[DeckManager] Карта {card} отправлена в сброс. В сбросе {discard.Count}");
            }
        }

        [Server]
        public void ServerDiscard(CardData card)
        {
            if (card != null)
            {
                DiscardCard(card.cardName);
            }
        }

        [Server]
        private void Reshuffle()
        {
            if (discard.Count == 0) return;

            deck.AddRange(discard);
            discard.Clear();
            Shuffle(deck);

            Debug.Log($"[DeckManager] Пересобрали колоду! Теперь {deck.Count} карт, сброс очищен.");
        }

        private void Shuffle(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rnd = Random.Range(i, list.Count);
                (list[i], list[rnd]) = (list[rnd], list[i]);
            }
        }

        [Server]
        public void ServerDraw(PlayerCore player, int count)
        {
            if (player == null || !player.isAlive) return;

            Debug.Log($"[DeckManager] ServerDraw → {count} карт для {player.netId}");

            for (int i = 0; i < count; i++)
            {
                string card = DrawCard();
                Debug.Log($"[DeckManager] Выдана карта: {card} игроку {player.netId}");

                if (card != null)
                {
                    player.hand.Add(card); // SyncList синхронизирует на клиентах
                }
            }
        }


        // 🔹 Получить CardData по имени карты
        public CardData GetCardByName(string cardName)
        {
            return allCardsData.Find(c => c.cardName == cardName);
        }
    }
}
