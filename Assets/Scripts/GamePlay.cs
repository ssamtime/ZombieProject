using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerData : INetworkStruct
{
    [Networked, Capacity(24)]
    public string Nickname { get => default; set { } }
    public PlayerRef PlayerRef;
    public int Kills;
    public int Deaths;
    public int LastKillTick;
    public int StatisticPosition;
    public bool IsAlive;
    public bool IsConnected;
}

public class GamePlay : NetworkBehaviour
{

    public GameObject PlayerPrefab; // Player PlayerPrefab �̾���
    [Networked][Capacity(32)][HideInInspector]
    public NetworkDictionary<PlayerRef, PlayerData> PlayerData { get; }

    public override void Spawned()
    {
        // ��Ʈ��ũ ��ü�� ������ �� �ʱ�ȭ �۾��� �����մϴ�.
        if (Runner.IsServer)
        {
            // ���� �ʱ�ȭ �۾� (��: �÷��̾� ����)
            // ���� ���, �÷��̾� ���۷����� ���� �����ϴ� �ڵ带 ���⿡ �ۼ��� �� �ֽ��ϴ�.
            // ���⼭�� ���÷� LocalPlayer�� �����մϴ�.
            SpawnPlayer(Runner.LocalPlayer);
        }
    }

    private void SpawnPlayer(PlayerRef playerRef)
    {
        if (PlayerData.TryGet(playerRef, out var playerData) == false)
        {
            playerData = new PlayerData();
            playerData.PlayerRef = playerRef;
            playerData.Nickname = playerRef.ToString();
            playerData.StatisticPosition = int.MaxValue;
            playerData.IsAlive = false;
            playerData.IsConnected = false;
        }

        if (playerData.IsConnected == true)
            return;

        Debug.LogWarning($"{playerRef} connected.");

        playerData.IsConnected = true;
        playerData.IsAlive = true;

        PlayerData.Set(playerRef, playerData);

        var spawnPoint = GetSpawnPoint();
        var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

        // Set player instance as PlayerObject so we can easily get it from other locations.
        Runner.SetPlayerObject(playerRef, player);

        //RecalculateStatisticPositions();
    }

    private Transform GetSpawnPoint()
    {
        Transform spawnPoint = default;

        // Iterate over all spawn points in the scene.
        var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
        for (int i = 0, offset = Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
        {
            spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;

            //if (_recentSpawnPoints.Contains(spawnPoint) == false)
            //    break;
        }

        //// Add spawn point to list of recently used spawn points.
        //_recentSpawnPoints.Add(spawnPoint);

        //// Ignore only last 3 spawn points.
        //if (_recentSpawnPoints.Count > 3)
        //{
        //    _recentSpawnPoints.RemoveAt(0);
        //}

        return spawnPoint;
    }
}
