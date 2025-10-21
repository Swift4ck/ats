// Assets/_Project/Scripts/Player/PlayerCore.cs
using Mirror;
using UnityEngine;
using System.Collections.Generic;
using TSGame;


namespace TSGame
{
    public class PlayerCore : NetworkBehaviour
    {
        [SyncVar] public string playerName;
        [SyncVar] public PlayerRole role;
        [SyncVar(hook = nameof(OnHealthChanged))] public int health = 5;
        [SyncVar(hook = nameof(OnArmorChanged))] public int armor = 0;
        [SyncVar(hook = nameof(OnManaChanged))] public int mana = 2;
        [SyncVar] public bool hasExtraLife = false;        // запасная жизнь
        [SyncVar] public bool forsakenRespawnedWithSix = false; // бонус при 6 игроках

        [SyncVar] public bool isAlive = true;
        [SyncVar] public bool isRoleRevealed = false;
        [SyncVar] public bool isReaperAlive = false; // для отслеживания Reaper
        [SyncVar] public int avatarId = 0; // индекс выбранного аватара





        public HandManager handManager;


        [SyncVar(hook = nameof(OnAvatarChanged))]
        public GameObject avatarObject;

        public PlayerAvatarController avatar; // контроллер анимаций

        void OnAvatarChanged(GameObject oldAvatar, GameObject newAvatar)
        {
            if (newAvatar != null)
            {
                avatar = newAvatar.GetComponent<PlayerAvatarController>();
                avatar?.PlayIdle();
            }
        }


        public SyncList<string> hand = new SyncList<string>();






        [Server]
        public void ServerTakeDamage(int amount)
        {
            if (!isAlive) return;

            int remaining = amount;

            // броня поглощает урон
            if (armor > 0)
            {
                int absorbed = Mathf.Min(armor, remaining);
                armor -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0)
            {
                health -= remaining;
                if (health <= 0)
                {
                    health = 0;

                    if (role == PlayerRole.Forsaken && hasExtraLife)
                    {
                        // тратим запасную жизнь
                        hasExtraLife = false;
                        isAlive = true;
                        health = 5;
                        armor = 0;

                        // выдаём бонус при 6 игроках (RoleManager сообщит)
                        if (forsakenRespawnedWithSix)
                        {
                            mana = 3;
                        }
                        else
                        {
                            mana = 2;
                        }

                        RpcOnRespawn();
                    }



                    else
                    {
                        isAlive = false;
                        isRoleRevealed = true;
                        ServerDiscardAllHand();
                        // Если игрок Reaper — помечаем его как не живого
                        if (role == PlayerRole.Reaper)
                            isReaperAlive = false;

                        RpcOnDeath();
                    }
                }
            }



                RpcOnDamage(amount);
        }
        [Server]
        public void ServerHeal(int amount)
        {
            if (!isAlive) return;
            health = Mathf.Min(5, health + amount);
            RpcOnHeal(amount);
        }

        [Server]
        public void ServerAddArmor(int amount)
        {
            armor = Mathf.Min(5, armor + amount);
        }

        // 🔹 Commands (клиент → сервер)
        [Command]
        public void CmdPlayCard(int handIndex, uint targetNetId)
        {
            // --- 1. Базовые проверки ---
            if (TurnManager.Instance.GetCurrentPlayer() != this)
            {
                Debug.LogWarning($"[CmdPlayCard] Игрок {netId} пытался ходить не в свой ход!");
                return;
            }

            if (!isAlive)
            {
                Debug.LogWarning($"[CmdPlayCard] Игрок {netId} мёртв и не может играть карты!");
                return;
            }

            if (handIndex < 0 || handIndex >= hand.Count)
            {
                Debug.LogWarning($"[CmdPlayCard] Неверный индекс карты: {handIndex}");
                return;
            }

            // --- 2. Получаем карту ---
            string cardName = hand[handIndex];
            CardData card = DeckManager.Instance.GetCardByName(cardName);
            if (card == null)
            {
                Debug.LogError($"[CmdPlayCard] Не найден CardData для карты {cardName}");
                return;
            }

            // --- 3. Проверяем ману ---
            if (mana < card.manaCost)
            {
                Debug.LogWarning($"[CmdPlayCard] Недостаточно маны: {mana}/{card.manaCost}");
                return;
            }

            // --- 4. Определяем цели ---
            List<PlayerCore> targets = new List<PlayerCore>();
            switch (card.targetType)
            {
                case TargetType.Self:
                    targets.Add(this);
                    break;

                case TargetType.OtherPlayer:
                    if (!NetworkIdentity.spawned.TryGetValue(targetNetId, out NetworkIdentity niOther)) return;
                    PlayerCore targetOther = niOther.GetComponent<PlayerCore>();
                    if (targetOther == null || !targetOther.isAlive || targetOther == this) return;
                    targets.Add(targetOther);
                    break;

                case TargetType.AnyPlayer:
                    if (!NetworkIdentity.spawned.TryGetValue(targetNetId, out NetworkIdentity niAny)) return;
                    PlayerCore targetAny = niAny.GetComponent<PlayerCore>();
                    if (targetAny == null || !targetAny.isAlive) return;
                    targets.Add(targetAny);
                    break;

                case TargetType.AllPlayers:
                    foreach (var ni in NetworkIdentity.spawned.Values)
                    {
                        PlayerCore p = ni.GetComponent<PlayerCore>();
                        if (p != null && p.isAlive) targets.Add(p);
                    }
                    break;

                case TargetType.NoTarget:
                    targets = null; // эффект сам по себе, target = null
                    break;
            }

            // --- 5. Применяем эффект ---
            CardEffects.Apply(card, this, targets);

            // --- 6. Списываем ману ---
            mana -= card.manaCost;

            // --- 7. Удаляем карту из руки ---
            hand.RemoveAt(handIndex);

           

            // --- 8. Сбрасываем карту в колоду ---
            DeckManager.Instance.ServerDiscard(card);

            // --- 9. RPC для анимации ---
            RpcOnCardPlayed(card.name, netId, targetNetId);
        }


        [ClientRpc]
        private void RpcOnCardPlayed(string cardName, uint casterId, uint targetId)
        {
            // Тут можно дернуть анимацию/звук/визуализацию
            Debug.Log($"[RpcOnCardPlayed] {cardName} сыграна игроком {casterId} против {targetId}");
        }

        [Command]
        public void CmdEndTurn()
        {
            if (TurnManager.Instance.GetCurrentPlayer() == this)
            {
                TurnManager.Instance.EndTurn();
            }
        }

        // 🔹 RPC (сервер → клиенты)
        [ClientRpc]
        void RpcOnDamage(int amount)
        {
            if (avatar != null)
                avatar.PlayDamage();
        }

        [ClientRpc]
        void RpcOnHeal(int amount)
        {
            if (avatar != null)
                avatar.PlayIdle(); // у нас пока нет heal-анимации, можно просто вернуть idle
        }

        [ClientRpc]
        void RpcOnDeath()
        {
            if (avatar != null)
                avatar.PlayDeath();
        }

        [ClientRpc]
        void RpcOnRespawn()
        {
            if (avatar != null)
                avatar.PlayRespawn();
        }


        [ClientRpc]
        public void RpcOnReaperDeath()
        {
            Debug.Log("Reaper умер!");
            // Здесь можно добавить визуальное уведомление для всех игроков
        }



        // 🔹 SyncVar хуки
        void OnHealthChanged(int oldValue, int newValue) { /* обновить UI */ }
        void OnArmorChanged(int oldValue, int newValue) { /* обновить UI */ }
        void OnManaChanged(int oldValue, int newValue) { /* обновить UI */ }


        public override void OnStartServer()
        {
            base.OnStartServer();
           

            // если имени нет, создаём дефолтное
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = $"Player_{connectionToClient.connectionId}";

            Debug.Log($"[PlayerCore] Зарегистрирован на сервере: {netId}, имя={playerName}");

            // Регистрируем игрока в GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ServerRegisterPlayer(this);
                Debug.Log($"[PlayerCore] Передан в GameManager.ServerRegisterPlayer: {netId}");
            }
            else
            {
                Debug.LogError("[PlayerCore] ❌ GameManager.Instance == null — игрок не зарегистрирован!");
            }
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            handManager = GetComponentInChildren<HandManager>();


            if (handManager != null && isLocalPlayer)
            {
                handManager.Initialize(this);
                handManager.ShowEndTurnButton(false); // скрываем по умолчанию
                handManager.RefreshHandUI(hand);
            }
        }









        [Server]
        public void StartTurn()
        {
            Debug.Log($"[PlayerCore] StartTurn вызван у игрока {netId} (имя={playerName})");

            for (int i = 0; i < 2; i++)
            {
                string newCard = DeckManager.Instance.DrawCard();

                if (newCard != null)
                {
                    hand.Add(newCard);


                    Debug.Log($"[PlayerCore] Игрок {netId} получил карту: {newCard}. Теперь в руке {hand.Count} карт");
                }
                else
                {
                    Debug.LogWarning($"[PlayerCore] DeckManager вернул null при доборе карты для игрока {netId}");
                }
            }

            Debug.Log($"[PlayerCore] Стартовый добор завершён. Всего карт в руке: {hand.Count}");
        }



        [Server]
        public void EndTurn()
        {
            Debug.Log($"{netId} закончил ход");
        }

        [Server]
        public void ServerAddMana(int amount)
        {
            if (!isAlive) return;
            mana = Mathf.Min(10, mana + amount); // максимум 10 (можно поменять)
            RpcOnManaGain(amount);
        }

        [ClientRpc]
        void RpcOnManaGain(int amount)
        {
            // Тут можно добавить анимацию/эффект получения маны
        }

        [Server]
        public void ServerDiscardAllHand()
        {
            foreach (var cardName in hand)
            {
                DeckManager.Instance.DiscardCard(cardName);
            }
            hand.Clear();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerDiscardAllHand(); // сбросить все карты игрока при дисконнекте
            Debug.Log($"[PlayerCore] Игрок {netId} отключился — его карты сброшены.");
        }


    }



}
