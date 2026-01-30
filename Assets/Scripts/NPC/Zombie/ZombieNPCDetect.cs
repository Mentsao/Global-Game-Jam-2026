using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.LowLevel;

public class ZombieNPCDetect : MonoBehaviour
{
    public GameObject target;
    [SerializeField] private LayerMask personLayer;
    public float fieldOfView = 45f;
    public float rangeOfView = 5f;
    public bool inFront = false;
    public bool inFOV = false;
    public bool inRange = false;

    void Update()
    {
        DetectAll(transform.position, rangeOfView);
        if (inFront)
        {
            CheckFieldOfView();
            CheckIfInRange();

            if (inFOV && inRange)
            {
                LookAtPlayer();
            }
        }
        
    }

    public void DetectAll(Vector3 center, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius, personLayer);
        float minDist = Mathf.Infinity;
        target = null;

        foreach (var hitCollider in hitColliders)
        {
            float enemyDist = Vector3.Distance(hitCollider.transform.position, transform.position);
            if (enemyDist < minDist)
            {
                minDist = enemyDist;
                target = hitCollider.gameObject;
            }
        }

        if (target != null)
        {
            CheckIfTargetIsInFront();
            Debug.Log(target + " is the target enemy");
        }
        else
        {
            inFront = false;
        }
    }

    void CheckIfTargetIsInFront()
    {
        Vector3 toTarget = (target.transform.position - transform.position).normalized;
        float forwardDot = Vector3.Dot(transform.forward, toTarget);
        float rightDot = Vector3.Dot(transform.right, toTarget);

        if (forwardDot > 0.5f)
        {
            inFront = true;
        }
        else
        {
            inFront = false;
        }
    }

    void CheckFieldOfView()
    {
        Vector3 toPerson = (target.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toPerson);

        float threshold = Mathf.Cos(fieldOfView * Mathf.Deg2Rad);

        if (dot > threshold)
        {
            if (Vector3.Distance(target.transform.position, transform.position) <= rangeOfView)
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

        if (Vector3.Distance(target.transform.position, transform.position) > rangeOfView)
        {
            inFOV = false;
        }
    }

    void CheckIfInRange()
    {
        Vector3 toPlayer = (target.transform.position - transform.position).normalized;
        Ray ray = new Ray(transform.position, toPlayer);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rangeOfView))
        {
            if (hit.transform.CompareTag("Player"))
            {
                inRange = true;
            }
            else
            {
                inRange = false;
            }
        }
    }

    void LookAtPlayer()
    {
        transform.LookAt(target.transform.position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeOfView);

        if (target == null) return;

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
        Gizmos.DrawLine(transform.position, target.transform.position);

        // FOV cone lines
        Gizmos.color = Color.yellow;
        Quaternion leftRot = Quaternion.Euler(0, -fieldOfView, 0);
        Quaternion rightRot = Quaternion.Euler(0, fieldOfView, 0);

        Gizmos.DrawLine(transform.position, transform.position + leftRot * transform.forward * 3);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * transform.forward * 3);
    }
}



