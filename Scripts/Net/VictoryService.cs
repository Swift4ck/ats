using System.Collections.Generic;

namespace TSGame
{
    public static class VictoryService
    {
        public static WinTeam Check(List<PlayerCore> players)
        {
            bool anyTrueSoulAlive = false;
            bool anyForsakenAlive = false;

            foreach (var p in players)
            {
                if (!p.isAlive) continue;

                if (p.role == PlayerRole.TrueSoul) anyTrueSoulAlive = true;
                if (p.role == PlayerRole.Forsaken) anyForsakenAlive = true;
            }

            if (!anyForsakenAlive) return WinTeam.TrueSouls;
            if (!anyTrueSoulAlive) return WinTeam.Forsaken;
            bool anyReaperAlive = false;
            foreach (var p in players)
            {
                if (p.isAlive && p.role == PlayerRole.Reaper)
                    anyReaperAlive = true;
            }

            if (anyReaperAlive && !anyTrueSoulAlive && !anyForsakenAlive)
                return WinTeam.Reaper;

            return WinTeam.None;
        }
    }




}
