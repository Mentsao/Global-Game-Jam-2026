using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NPCDetect : MonoBehaviour
{
    public Transform player;
    public float fieldOfView = 45f;
    public float rangeOfView = 5f;
    public bool inFOV;
    public bool detectsPlayer;

    void Update()
    {
        CheckFieldOfView();
        CheckIfPlayerIsSeen();

        if (detectsPlayer && inFOV)
        {
            LookAtPlayer();
        }
    }

    void CheckFieldOfView()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toPlayer);

        float threshold = Mathf.Cos(fieldOfView * Mathf.Deg2Rad);

        if (dot > threshold)
        {
            if (Vector3.Distance(player.position, transform.position) <= rangeOfView)
            {
                inFOV = true;
            }
            Debug.Log("Player is INSIDE the field of view.");
        }
        else
        {
            inFOV = false;
            Debug.Log("Player is OUTSIDE the field of view.");
        }

        if (Vector3.Distance(player.position, transform.position) > rangeOfView)
        {
            inFOV = false;
        }
    }

    void CheckIfPlayerIsSeen()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;
        Ray ray = new Ray(transform.position, toPlayer);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Player"))
            {
                detectsPlayer = true;
            }
            else
            {
                detectsPlayer = false;
            }
        }
    }

    void LookAtPlayer()
    {
        transform.LookAt(player.position);
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        Vector3 start = transform.position;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(start, direction, out RaycastHit hit))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(start, hit.point);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, start + direction);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * rangeOfView);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, player.position);

        Gizmos.color = Color.yellow;
        Quaternion leftRot = Quaternion.Euler(0, -fieldOfView, 0);
        Quaternion rightRot = Quaternion.Euler(0, fieldOfView, 0);

        Gizmos.DrawLine(transform.position, transform.position + leftRot * transform.forward * 3);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * transform.forward * 3);
    }


}

