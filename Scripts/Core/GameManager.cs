// Assets/_Project/Scripts/Core/GameManager.cs
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace TSGame
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;

        [SyncVar] public GamePhase phase = GamePhase.WaitingForPlayers;

        private readonly List<PlayerCore> players = new List<PlayerCore>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject); // защита от дублей
        }

        [Server]
        public void ServerRegisterPlayer(PlayerCore p)
        {
            if (p == null || players.Contains(p)) return;

            players.Add(p);
            Debug.Log($"[GameManager] Player registered: {p.netId}");

            // выдаём стартовые карты
            DeckManager.Instance.ServerDraw(p, 5);

            // 🚀 Если подключились все нужные игроки – запускаем игру
            if (players.Count >= 2) // тут можно поставить нужное число игроков
            {
                Debug.Log("[GameManager] Все игроки подключились, начинаем игру!");
                ServerStartGame();
            }
        }


        [Server]
        public void ServerStartGame()
        {
            phase = GamePhase.InProgress;
            Debug.Log("[GameManager] Game started");

            // 🚀 Запускаем цикл ходов
            // Новое:
            if (TurnManager.Instance != null)
            {
                // 🚀 безопасный запуск игры с заполненным списком игроков
                TurnManager.Instance.StartGame(players);
            }
            else
            {
                Debug.LogError("[GameManager] TurnManager.Instance == null, не могу начать игру!");
            }
        }


        [Server]
        public List<PlayerCore> GetPlayers() => players;
    }
}
