// UnitMotor.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnitMotor : MonoBehaviour
{
    public float stepSpeed = 3.5f;       // unidades/seg
    public float rotateSpeed = 12f;      // suavizado de giro
    Animator anim;

    void Awake() { anim = GetComponent<Animator>(); }

    public IEnumerator MoveAlong(List<GridSystem.Node> path)
    {
        if (path == null || path.Count <= 1) yield break;
        anim.SetBool("isMoving", true);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 target = path[i].worldPos;
            while ((transform.position - target).sqrMagnitude > 0.0004f)
            {
                Vector3 dir = (target - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotateSpeed);
                }
                transform.position = Vector3.MoveTowards(transform.position, target, stepSpeed * Time.deltaTime);
                yield return null;
            }
        }

        anim.SetBool("isMoving", false);
    }
}
