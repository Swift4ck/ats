using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace TSGame
{
    public class TurnManager : NetworkBehaviour
    {
        public static TurnManager Instance; // Singleton

        private List<PlayerCore> players = new List<PlayerCore>();
        [SyncVar] private int currentIndex = 0;

        private void Awake()
        {
            Instance = this;
        }

        // Сервер запускает игру
        [Server]
        public void StartGame(List<PlayerCore> playerList)
        {
            players = playerList;
            if (playerList == null || playerList.Count == 0) 
                {
                    Debug.LogError("[TurnManager] StartGame вызван с пустым списком игроков!");
                    return;
                }
            currentIndex = GetNextAliveIndex(-1); // начнём с первого живого

            if (currentIndex < 0 || currentIndex >= players.Count)
            {
                Debug.LogError("[TurnManager] Нет живых игроков для начала игры!");
                return;
            }

            // В старт игры нет «предыдущего игрока», поэтому вручную даём базовую ману первому
            var first = players[currentIndex];
            int baselineMana = (first.forsakenRespawnedWithSix ? 3 : 2);
            Debug.Log($"[TurnManager] Игра стартует. Первый игрок: {first.playerName} ({first.netId})");

            first.mana = baselineMana;
            first.mana = 2; // позже: if (first.forsakenRespawnedWithSixLimit) first.mana = 3;

            StartTurn();
        }

        [Server]
        private int GetNextAliveIndex(int start)
        {
            for (int step = 1; step <= players.Count; step++)
            {
                int idx = (start + step + players.Count) % players.Count;
                if (players[idx] != null && players[idx].isAlive)
                    return idx;
            }
            return start >= 0 ? start : 0;
        }

        [Server]
        private void StartTurn()
        {
            var currentPlayer = players[currentIndex];
            if (players == null || players.Count == 0)
            {
                Debug.LogError("[TurnManager] Список игроков пуст, StartTurn отменён");
                return;
            }

            if (currentIndex < 0 || currentIndex >= players.Count)
            {
                Debug.LogWarning("[TurnManager] currentIndex вне диапазона, ищем первого живого");
                currentIndex = GetNextAliveIndex(-1);
                if (currentIndex < 0 || currentIndex >= players.Count)
                {
                    Debug.LogError("[TurnManager] Нет живых игроков для хода!");
                    return;
                }
            }


            Debug.Log($"[TurnManager] ➡️ Ход переходит к игроку {currentPlayer.netId} (имя={currentPlayer.playerName})");

            // (Когда внедрим DeckManager) — добор +2 карт сделаем здесь.
            // deck.ServerDraw(currentPlayer, 2);

            currentPlayer.StartTurn(); // визуал/лог
            RpcTurnStarted(currentPlayer.netIdentity);
        }

        [Server]
        public void EndTurn()
        {
            var currentPlayer = players[currentIndex];
            Debug.Log($"[TurnManager] ⏹ Игрок {currentPlayer.playerName} (netId={currentPlayer.netId}) закончил ход");


            // Проверка лимита руки перед концом хода
            int handLimit = currentPlayer.forsakenRespawnedWithSix ? 6 : 5;
            if (currentPlayer.hand.Count > handLimit)
            {
                Debug.LogWarning(
                    $"Игрок {currentPlayer.netId} не может закончить ход: {currentPlayer.hand.Count}/{handLimit}"
                );
                return;
            }

            // Проверка победы перед переходом
            var result = VictoryService.Check(players);
            if (result != WinTeam.None)
            {
                RpcGameOver(result);
                return;
            }

            // Переходим к следующему живому игроку
            int nextIndex = GetNextAliveIndex(currentIndex);
            if (nextIndex == -1 || nextIndex == currentIndex)
            {
                Debug.LogWarning("[TurnManager] Нет других живых игроков, игра завершена?");
                return;
            }

            var nextPlayer = players[nextIndex];

            // Устанавливаем базовую ману следующему игроку
            int baselineMana = nextPlayer.forsakenRespawnedWithSix ? 3 : 2;
            nextPlayer.mana = Mathf.Clamp(baselineMana, 0, 5);

            currentIndex = nextIndex;

          
            nextPlayer.mana = nextPlayer.forsakenRespawnedWithSix ? 3 : 2;

            StartTurn();
        }



        [ClientRpc]
        private void RpcGameOver(WinTeam team)
        {
            Debug.Log($"Игра окончена! Победила команда: {team}");
        }



        // Клиентам уведомление чей ход
        [ClientRpc]
        private void RpcTurnStarted(NetworkIdentity playerId)
        {
            var player = playerId.GetComponent<PlayerCore>();
            Debug.Log($"[TurnManager] 🟢 Сейчас ходит игрок: {player.playerName} (netId={player.netId})");

            // Показываем кнопку только локальному игроку через HandManager
            if (player.isLocalPlayer && player.handManager != null)
            {
                player.handManager.ShowEndTurnButton(true);
            }

            // Скрываем кнопку у всех остальных локально
            foreach (var otherPlayer in FindObjectsOfType<PlayerCore>())
            {
                if (otherPlayer != player && otherPlayer.isLocalPlayer && otherPlayer.handManager != null)
                {
                    otherPlayer.handManager.ShowEndTurnButton(false);
                }
            }
        }


        public PlayerCore GetCurrentPlayer()
        {
            return players.Count > 0 ? players[currentIndex] : null;
        }

        [Command]
        public void CmdEndTurn()
        {
            var current = players[currentIndex];
            if (current.connectionToClient != connectionToClient) return;

            if (current.hand.Count > 5) return; // нельзя завершить ход с >5 картами

            EndTurn();
        }




    }
}
