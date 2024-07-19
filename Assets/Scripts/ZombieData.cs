using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable/ZombieData", fileName = "Zombie Data")]
public class ZombieData : ScriptableObject
{
    public float health = 100f; // 체력
    public float damage = 20f;  // 공격력
    public float speed = 2f;    // 이동속도
    public Color skinColor = Color.white;   // 피부색
}