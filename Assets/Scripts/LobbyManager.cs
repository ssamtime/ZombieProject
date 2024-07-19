using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// 마스터(매치 메이킹) 서버와 룸 접속을 담당
public class LobbyManager : SimulationBehaviour, INetworkRunnerCallbacks
{
    [HideInInspector] public NetworkRunner networkRunner;

    public Text connectionInfoText; // 네트워크 정보를 표시할 텍스트
    public Button joinButton; // 룸 접속 버튼
    public GameObject uiCanvas; // 플레이어 생성 확인할때 안보이게 하려고

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private bool _mouseButton0;

    private bool sceneLoaded = false;


    private void Start() {

        // 룸 접속 버튼을 활성화
        joinButton.interactable = true;

        // 접속을 시도 중임을 텍스트로 표시
        connectionInfoText.text = "방을 create하거나 join하세요";
    }

    private async void StartGame(GameMode mode)
    {
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            networkRunner.ProvideInput = true;
        }

        // 현재 씬으로부터 NetworkSceneInfo 생성
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        var result = await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            joinButton.interactable = true;
            connectionInfoText.text = "방참가 성공, 당신은 " + networkRunner.GameMode +"입니다.";
            
        }
        else
        {
            joinButton.interactable = false;
            connectionInfoText.text = "방참가 실패";
            //// 일정 시간 후 재시도
            //await Task.Delay(2000);
            //await ConnectToServer();
        }
    }

    public void Connect()
    {
        // onclick함수로 쓰기위해 비동기 메서드를 호출
        StartGame(GameMode.AutoHostOrClient);
    }

    // INetworkRunnerCallbacks 인터페이스들

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        print("OnPlayerJoined");

        if(runner.IsServer)
        {
            // 플레이어의 고유 위치 생성
            Vector3 spawnPosition =
                new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 2, 1, 0);
            NetworkObject networkPlayerObject =
                runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // 쉬운 접근을 위한 플레이어 아바타 dictionary 추가
            _spawnedCharacters.Add(player, networkPlayerObject);

            // 
            runner.SetPlayerObject(runner.LocalPlayer, networkPlayerObject);

            
        }

        if (Runner.IsSceneAuthority)
        {
            if (sceneLoaded == false)
            {
                Runner.LoadScene(SceneRef.FromIndex(1), LoadSceneMode.Additive);
                sceneLoaded = true;
                //Runner.SetActiveScene
            }
        }



    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        if(_spawnedCharacters.TryGetValue(player ,out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }


    private void Update()
    {
        _mouseButton0 = _mouseButton0 | Input.GetMouseButton(0);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkedInput();

        //if (Input.GetKey(KeyCode.W))
        //    data.direction += Vector3.forward;
        //if (Input.GetKey(KeyCode.S))
        //    data.direction += Vector3.back;
        //if (Input.GetKey(KeyCode.A))
        //    data.direction += Vector3.left;
        //if (Input.GetKey(KeyCode.D))
        //    data.direction += Vector3.right;

        data.direction.z += Input.GetAxis("Vertical");
        data.direction.x += Input.GetAxis("Horizontal");

        if(Input.GetKeyDown(KeyCode.R))
        {    


        }

        data.buttons.Set(NetworkedInput.MOUSEBUTTON0, _mouseButton0);
                

        input.Set(data);

        _mouseButton0 = false;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        connectionInfoText.text = "Disconnected";
        print("OnShutdown");
    }

    public void OnConnectedToServer(NetworkRunner runner) 
    {
        print("OnConnectedToServer ");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner) { 

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    {
        print("Session list updated 12312323123223123");   
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

    public void OnSceneLoadDone(NetworkRunner runner) 
    {
        if(uiCanvas)
            uiCanvas.gameObject.SetActive(false);

        
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }
}