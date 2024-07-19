using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Menu;
using Fusion.Photon.Realtime;
using Fusion.Sockets;

#pragma warning disable 1998
#pragma warning disable 4014


public class MenuConnectionBehaviour : FusionMenuConnectionBehaviour
{
    public NetworkRunner RunnerPrefab;
    public MenuUIController UIController;
    public MenuConnectionPlugin ConnectionPlugin;

    public override IFusionMenuConnection Create()
    {
        if (ConnectionPlugin != null)
            return ConnectionPlugin.Create(this);

        return new MenuConnection(this);
    }
}

public class MenuConnection : IFusionMenuConnection
{
    public bool IsSessionOwner => _runner != null && _runner.IsRunning == true ? _runner.IsSceneAuthority : default;
    public string SessionName => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.Name : default;
    public int MaxPlayerCount => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.MaxPlayers : default;
    public string Region => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.Region : default;
    public string AppVersion => PhotonAppSettings.Global.AppSettings.AppVersion;
    public List<string> Usernames => default;
    public bool IsConnected => _runner != null ? _runner.IsConnectedToServer : default;
    public int Ping => _runner != null && _runner.IsRunning == true ? Mathf.RoundToInt((float)(_runner.GetPlayerRtt(PlayerRef.None) * 1000.0)) : default;

    protected FusionMenuConnectionBehaviour ConnectionBehaviour => _connectionBehaviour;

    private MenuConnectionBehaviour _connectionBehaviour;
    private NetworkRunner _runner;

    public MenuConnection(MenuConnectionBehaviour connectionBehaviour)
    {
        _connectionBehaviour = connectionBehaviour;
    }

    // 지역 추가
    public virtual async Task<List<FusionMenuOnlineRegion>> RequestAvailableOnlineRegionsAsync(IFusionMenuConnectArgs connectArgs)
    {
        List<FusionMenuOnlineRegion> regions = new List<FusionMenuOnlineRegion>();
        foreach (var region in _connectionBehaviour.UIController.Config.AvailableRegions)
        {
            regions.Add(new FusionMenuOnlineRegion { Code = region, Ping = 0 });
        }

        return regions;
    }

    // session 만드는 부분
    public virtual async Task<ConnectResult> ConnectAsync(IFusionMenuConnectArgs connectionArgs)
    {
        // id없으면 id만들어라고 하기
        if (string.IsNullOrEmpty(PhotonAppSettings.Global.AppSettings.AppIdFusion) == true)
        {
            await _connectionBehaviour.UIController.PopupAsync("The Fusion AppId is missing in PhotonAppSettings. Please follow setup instructions before running the game.", "Game not configured");
            _connectionBehaviour.UIController.Show<FusionMenuUIMain>();
            return ConnectionFail(ConnectFailReason.UserRequest);
        }

        _runner = CreateRunner();

        var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
        appSettings.FixedRegion = connectionArgs.Region;


        // session 정보 설정
        var startGameArgs = new StartGameArgs()
        {
            SessionName = connectionArgs.Session,
            PlayerCount = connectionArgs.MaxPlayerCount,
            GameMode = GetGameMode(connectionArgs),
            CustomPhotonAppSettings = appSettings
        };

        // session 만드는중아니면서 만들어진거 없으면
        if (connectionArgs.Creating == false && string.IsNullOrEmpty(connectionArgs.Session) == true)
        {
            startGameArgs.EnableClientSessionCreation = false;

            // session만들기
            var randomJoinResult = await StartRunner(startGameArgs);
            if (randomJoinResult.Success)
                return await StartGame(connectionArgs.Scene.SceneName); // Scene불러오기

            if (randomJoinResult.FailReason == ConnectFailReason.UserRequest)
                return ConnectionFail(randomJoinResult.FailReason);

            connectionArgs.Creating = true;

            _runner = CreateRunner();

            startGameArgs.EnableClientSessionCreation = true;
            startGameArgs.SessionName = _connectionBehaviour.UIController.Config.CodeGenerator.Create();
            startGameArgs.GameMode = GetGameMode(connectionArgs);
        }

        var result = await StartRunner(startGameArgs);
        if (result.Success)
            return await StartGame(connectionArgs.Scene.SceneName);

        await DisconnectAsync(result.FailReason);
        return ConnectionFail(result.FailReason);
    }

    public virtual async Task DisconnectAsync(int reason)
    {
        var runner = _runner;
        _runner = null;

        if (runner != null)
        {
            Scene sceneToUnload = default;

            if (runner.IsSceneAuthority == true && runner.TryGetSceneInfo(out NetworkSceneInfo sceneInfo) == true)
            {
                foreach (var sceneRef in sceneInfo.Scenes)
                {
                    await runner.UnloadScene(sceneRef);
                }
            }
            else
            {
                sceneToUnload = runner.SceneManager.MainRunnerScene;
            }

            await runner.Shutdown();

            if (sceneToUnload.IsValid() == true && sceneToUnload.isLoaded == true && sceneToUnload != _connectionBehaviour.gameObject.scene)
            {
                SceneManager.SetActiveScene(_connectionBehaviour.gameObject.scene);
                SceneManager.UnloadSceneAsync(sceneToUnload);
            }
        }

        if (reason != ConnectFailReason.UserRequest)
        {
            await _connectionBehaviour.UIController.PopupAsync(reason.ToString(), "Disconnected");
        }

        _connectionBehaviour.UIController.OnGameStopped();
    }

    private GameMode GetGameMode(IFusionMenuConnectArgs connectionArgs)
    {
        if (_connectionBehaviour.UIController.SelectedGameMode == GameMode.AutoHostOrClient)
            return connectionArgs.Creating ? GameMode.Host : GameMode.Client;

        return _connectionBehaviour.UIController.SelectedGameMode;
    }

    private NetworkRunner CreateRunner()
    {
        var runner = GameObject.Instantiate(_connectionBehaviour.RunnerPrefab);
        runner.ProvideInput = true;
        return runner;
    }

    private async Task<ConnectResult> StartRunner(StartGameArgs args)
    {
        var result = await _runner.StartGame(args);
        return new ConnectResult() { Success = _runner.IsRunning, FailReason = ConnectFailReason.Disconnect };
    }

    private async Task<ConnectResult> StartGame(string sceneName)
    {
        try
        {
            Debug.Log("StartGame 함수부분");
            _runner.AddCallbacks(new MenuConnectionCallbacks(_connectionBehaviour.UIController, sceneName));
            if (_runner.IsSceneAuthority)
            {
                await _runner.LoadScene(sceneName, LoadSceneMode.Additive, LocalPhysicsMode.None, true);
            }
            _connectionBehaviour.UIController.OnGameStarted();
            return ConnectionSuccess();
        }
        catch (ArgumentException e)
        {
            Debug.LogError($"Failed to load scene. {e}.");
            await DisconnectAsync(ConnectFailReason.Disconnect);
            return ConnectionFail(ConnectFailReason.Disconnect);
        }
    }

    private static ConnectResult ConnectionSuccess() => new ConnectResult() { Success = true };
    private static ConnectResult ConnectionFail(int failReason) => new ConnectResult() { FailReason = failReason };

    private class MenuConnectionCallbacks : INetworkRunnerCallbacks
    {
        public readonly MenuUIController Controller;
        public readonly string SceneName;
        private bool _mouseButton0;

        public MenuConnectionCallbacks(MenuUIController controller, string sceneName)
        {
            Controller = controller;
            SceneName = sceneName;
        }

        public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.DisconnectedByPluginLogic)
            {
                Controller.OnGameStopped();
                Controller.Show<FusionMenuUIMain>();
                Controller.PopupAsync("Disconnected from the server.", "Disconnected");

                if (runner.SceneManager != null)
                {
                    if (runner.SceneManager.MainRunnerScene.IsValid() == true)
                    {
                        SceneRef sceneRef = runner.SceneManager.GetSceneRef(runner.SceneManager.MainRunnerScene.name);
                        runner.SceneManager.UnloadScene(sceneRef);
                    }
                }
            }
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            GameObject.Find("GamePlay").GetComponent<GamePlay>().SpawnPlayer(runner.LocalPlayer);
            if (runner.IsServer)
            {
                // 서버에서 플레이어 스폰
                GameObject.Find("GamePlay").GetComponent<GamePlay>().SpawnPlayer(runner.LocalPlayer);                
            }
            else
            {
                // 클라이언트에서 서버로 플레이어 스폰 요청
                RequestSpawnPlayer(runner.LocalPlayer);
            }
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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

            if (Input.GetKeyDown(KeyCode.R))
            {
                // r버튼입력받고
                // 장전구현은 이미구현된함수에 연결
            }

            if (Input.GetMouseButton(0))
            {
                _mouseButton0 = true;
            }

            data.buttons.Set(NetworkedInput.MOUSEBUTTON0, _mouseButton0);

            input.Set(data);

            _mouseButton0 = false;
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RequestSpawnPlayer(PlayerRef playerRef)
        {
            if (_runner.IsServer)
            {
                GameObject.Find("GamePlay").GetComponent<GamePlay>().SpawnPlayer(playerRef);

            }
        }
    }
}
