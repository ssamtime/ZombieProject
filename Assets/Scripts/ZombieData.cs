using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable/ZombieData", fileName = "Zombie Data")]
public class ZombieData : ScriptableObject
{
    public float health = 100f; // ü��
    public float damage = 20f;  // ���ݷ�
    public float speed = 2f;    // �̵��ӵ�
    public Color skinColor = Color.white;   // �Ǻλ�
}