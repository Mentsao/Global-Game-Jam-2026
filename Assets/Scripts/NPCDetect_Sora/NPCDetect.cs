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

    [Header("Social Stealth")]
    [SerializeField] private Items.Masks.MaskType maskToIgnore = Items.Masks.MaskType.None;

    [Header("Stealth Settings")]
    [SerializeField] private float forgetTime = 3f;
    private float _forgetTimer;
    private float _baseRange;

    private void Start()
    {
        _baseRange = rangeOfView;
    }

    void Update()
    {
        UpdateStealthAdjustments();
        CheckFieldOfView();
        CheckIfPlayerIsSeen();

        if (detectsPlayer && inFOV)
        {
            LookAtPlayer();
            _forgetTimer = 0f;
        }
        else if (detectsPlayer || inFOV)
        {
            // Partially seen or last known?
            HandleForgetting();
        }
    }

    private void UpdateStealthAdjustments()
    {
        if (UI.StealthManager.Instance != null)
        {
            float stealth = UI.StealthManager.Instance.CurrentStealth;
            // Reduce range dynamically: if stealth is 0, range is 30% of base. If 100, range is 100%.
            float multiplier = Mathf.Lerp(0.3f, 1f, stealth / 100f);
            rangeOfView = _baseRange * multiplier;
        }
    }

    private void HandleForgetting()
    {
        _forgetTimer += Time.deltaTime;
        if (_forgetTimer >= forgetTime)
        {
            detectsPlayer = false;
            inFOV = false;
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
        if (player == null) return;

        Vector3 toPlayer = (player.position - transform.position).normalized;
        
        // Stealth check: if very stealthy and not in direct line of sight within a tight cone, maybe skip
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > rangeOfView)
        {
            detectsPlayer = false;
            return;
        }

        Ray ray = new Ray(transform.position, toPlayer);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rangeOfView))
        {
            if (hit.transform.CompareTag("Player"))
            {
                // Social Stealth Check
                Player.PlayerPickup pickup = hit.transform.GetComponent<Player.PlayerPickup>();
                if (pickup != null)
                {
                    // Government Mask grants total immunity against regular NPCs
                    if (pickup.CurrentMaskType == Items.Masks.MaskType.Government)
                    {
                        detectsPlayer = false;
                        return;
                    }

                    if (maskToIgnore != Items.Masks.MaskType.None && pickup.CurrentMaskType == maskToIgnore)
                    {
                        detectsPlayer = false;
                        return;
                    }
                }

                detectsPlayer = true;
            }
            else
            {
                detectsPlayer = false;
            }
        }
        else
        {
            detectsPlayer = false;
        }
    }

    void LookAtPlayer()
    {
        // Fix: Project player position to NPC's height to prevent tilting/toppling
        Vector3 lookTarget = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookTarget);
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

