using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkedInput : INetworkInput
{
    // Movement
    public Vector3 direction;

    // Bullet function
    public const byte MOUSEBUTTON0 = 1;
    public NetworkButtons buttons;

    public Vector3 LookDelta;
}

public class NetworkPlayer : NetworkBehaviour
{
    //[SerializeField] private SimpleKCC kcc;
    [SerializeField] private float speed = 5f;
    //[SerializeField] private float junpImpulse = 10f;
    [SerializeField] private Transform camTarget;
    //[SerializeField] private float lookSensitivity = 0.15f;

    float currentSpeed;

    public Gun gun; // ����� ��

    private NetworkCharacterController _cc;
    [SerializeField] private NetworkBullet _bulletPrefab;

    private Vector3 _forward = Vector3.forward;

    [Networked] private TickTimer delay { get; set; }

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        FindObjectOfType<PlayerHealth>().onDeath +=
            GameObject.Find("GameManager").GetComponent<GameManager>().EndGame;
        GameObject.Find("Item Spawner").GetComponent<ItemSpawner>().playerTransform =
            GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void Spawned()
    {
        //kcc.SetGravity(Physics.gravity.y * 2f);

        //if (HasInputAuthority)
        //    CameraSetup.Singleton.SetTarget(camTarget);

        if (HasInputAuthority)
            CameraSetup.Singleton.CameraFollowPlayer();
    }

    public override void FixedUpdateNetwork()
    {
        if(GetInput(out NetworkedInput data))
        {
            // Movement
            data.direction.Normalize();
            _cc.Move(speed * data.direction * Runner.DeltaTime);

            if(data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;  //������ ���� ����
            }

            // ���� ��� 0�̶� �Դٰ����ؼ� �ִϸ��̼��� ����� ����
            // fixedUpdate�� �־ �׷���
            // �Է°��� ���� �ִϸ������� Move �Ķ���Ͱ� ����
            GetComponent<Animator>().SetFloat("Move", _cc.Velocity.magnitude);

            // Instantiate bullet
            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkedInput.MOUSEBUTTON0))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    Runner.Spawn(_bulletPrefab,
                        transform.position + _forward, Quaternion.LookRotation(_forward),
                        Object.InputAuthority, (Runner, O) =>
                        {
                            O.GetComponent<NetworkBullet>().Init();
                        });

                    gun.Fire();
                }
            }
            
        }
    }
}
