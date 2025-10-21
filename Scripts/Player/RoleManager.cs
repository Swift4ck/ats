using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace TSGame
{
    public class RoleManager : NetworkBehaviour
    {
        [Server]
        public void ServerAssignRoles(List<PlayerCore> players)
        {
            // Простейший пример для 3–5 игроков
            // Shuffle
            var shuffled = new List<PlayerCore>(players);
            for (int i = 0; i < shuffled.Count; i++)
            {
                var tmp = shuffled[i];
                int r = Random.Range(i, shuffled.Count);
                shuffled[i] = shuffled[r];
                shuffled[r] = tmp;
            }



            // Простейшая раздача: 2 TrueSoul, 1 Forsaken, Reaper появится позже
            for (int i = 0; i < shuffled.Count; i++)
            {
                if (i < 2) shuffled[i].role = PlayerRole.TrueSoul;
                else shuffled[i].role = PlayerRole.Forsaken;

                // Для 5–6 игроков назначаем одного Reaper
                if (players.Count >= 5 && i == shuffled.Count - 1)
                {
                    shuffled[i].role = PlayerRole.Reaper;
                    shuffled[i].isReaperAlive = true;
                }
            }

            foreach (var p in players)
            {
                if (p.role == PlayerRole.Forsaken)
                {
                    p.hasExtraLife = true;

                    // если игроков 6 → включаем бонус
                    if (players.Count == 6)
                        p.forsakenRespawnedWithSix = true;
                }
            }

        }

        [Server]
        public void RevealRole(PlayerCore player)
        {
            player.isRoleRevealed = true;

            if (player.role == PlayerRole.Reaper)
                player.isReaperAlive = false;
        }
    }
}
