using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Cinemachine;

public class CameraSetup : NetworkBehaviour
{
    private Transform target;
    private static CameraSetup _singleton;

    public static CameraSetup Singleton
    {
        get => _singleton;
        set 
        {
            if(value == null)
                _singleton  = null;
            else if(_singleton == null)
                _singleton = value;
            else if(_singleton != value)
            {
                Destroy(value);
            }
        }
    }
    private void Awake()
    {
        Singleton = this;
    }
    private void OnDestroy()
    {
        if(Singleton == this)
            Singleton = null;
    }


    //private void LateUpdate()
    //{
    //    if(target != null)
    //    {
    //        if (target != null)
    //        {
    //            transform.SetPositionAndRotation(target.position, Quaternion.identity);
    //        }
    //    }
    //}

    //public void SetTarget(Transform newTarget)
    //{
    //    target = newTarget;
    //}


    public void CameraFollowPlayer()
    {
        CinemachineVirtualCamera followCam = FindObjectOfType<CinemachineVirtualCamera>();
        GameObject player = GameObject.FindWithTag("Player");
        followCam.Follow = player.transform;
        followCam.LookAt = player.transform;
    }
}
