using Mirror;
using UnityEngine;
using TSGame;

public class GameNetworkManager : NetworkManager
{
    private GameManager _gameManager;

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        int index = numPlayers;

        // 1. Создаем игрока
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        var pc = player.GetComponent<PlayerCore>();
        pc.avatarId = index % avatarPrefabs.Length;

        // 2. Спавним аватар через сеть
        if (PlayerSpawnSystem.Instance != null)
        {
            var spawnPoint = PlayerSpawnSystem.Instance.GetSpawnPoint(index);
            if (spawnPoint != null)
            {
                GameObject avatarObj = Instantiate(
                    avatarPrefabs[pc.avatarId],
                    spawnPoint.position,
                    spawnPoint.rotation
                );

                // Делам дочерним объектом игрока
                avatarObj.transform.SetParent(pc.transform);


                // Спавн через сервер — будет виден всем клиентам
                NetworkServer.Spawn(avatarObj, conn);

                var avatarTarget = avatarObj.GetComponentInChildren<TSGame.AvatarTarget>();
                if (avatarTarget != null)
                {
                    // Устанавливаем netId владельца (это SyncVar, оно уйдет клиентам)
                    avatarTarget.ownerNetId = pc.netId;
                }
                else
                {
                    Debug.LogWarning("[GameNetworkManager] Avatar prefab не содержит AvatarTarget!");
                }


                pc.avatarObject = avatarObj;

            }
            else
            {
                Debug.LogWarning($"Не найдена точка спавна для аватара {index}");
            }
        }

        // 3. Регистрируем игрока
        if (_gameManager == null)
            _gameManager = FindObjectOfType<GameManager>();
        _gameManager?.ServerRegisterPlayer(pc);
    }



    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName.Contains("Game"))
        {
            _gameManager = FindObjectOfType<GameManager>();
            if (_gameManager == null)
            {
                var go = new GameObject("Managers");
                _gameManager = go.AddComponent<GameManager>();
                // DontDestroyOnLoad(go); // если нужно сохранить между сценами
            }
        }
    }


    [Header("Аватары игроков")]
    public GameObject[] avatarPrefabs;

}
