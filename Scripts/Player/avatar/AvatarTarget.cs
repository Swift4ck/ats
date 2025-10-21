using Mirror;
using UnityEngine;

namespace TSGame
{
    // NetworkBehaviour: чтобы ownerNetId корректно синхронизировался при спавне/подключениях
    public class AvatarTarget : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnOwnerChanged))]
        public uint ownerNetId; // netId игрока-владельца (0 == none)

        // Кэш ссылки на PlayerCore (получаем на клиенте после spawn)
        [System.NonSerialized] public PlayerCore ownerPlayer;

        void OnOwnerChanged(uint oldId, uint newId)
        {
            ResolveOwner();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ResolveOwner();
        }

        void ResolveOwner()
        {
            ownerPlayer = null;
            if (ownerNetId == 0) return;
            if (NetworkIdentity.spawned.TryGetValue(ownerNetId, out NetworkIdentity ni))
            {
                ownerPlayer = ni.GetComponent<PlayerCore>();
            }
        }

        // Удобный геттер для CardDrag
        public uint GetOwnerNetId() => ownerNetId;
    }
}
