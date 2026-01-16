using System.Collections.Generic;
using UnityEngine;

public class FireballOrbitAbility : MonoBehaviour
{
    [Header("Runtime Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float orbitSpeed;
    [SerializeField] private int currentStacks;

    private FireballPowerUp powerUpData;
    private List<OrbitingFireball> activeFireballs = new List<OrbitingFireball>();

    // Rotation state
    private float currentAngle;
    private float currentSelfRotation;

    public int CurrentStacks => currentStacks;

    public void Initialize(FireballPowerUp data)
    {
        if (data == null)
        {
            Debug.LogError("[FireballOrbitAbility] PowerUp data is NULL!");
            return;
        }

        powerUpData = data;
        currentStacks = 1;
        currentAngle = 0f;
        currentSelfRotation = 0f;

        UpdateStats();
        SpawnFireballs();
    }

    public void AddStack(FireballPowerUp data)
    {
        powerUpData = data;
        currentStacks++;
        UpdateStats();
        SpawnFireballs();
    }

    private void UpdateStats()
    {
        if (powerUpData == null) return;

        damage = powerUpData.GetDamage(currentStacks);
        orbitSpeed = powerUpData.GetOrbitSpeed(currentStacks);

        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null)
            {
                activeFireballs[i].SetDamage(damage);
            }
        }
    }

    private void SpawnFireballs()
    {
        if (powerUpData == null || powerUpData.fireballPrefab == null) return;

        int targetCount = powerUpData.GetFireballCount(currentStacks);

        while (activeFireballs.Count < targetCount)
        {
            SpawnSingleFireball();
        }

        RepositionFireballs();
    }

    private void SpawnSingleFireball()
    {
        // Initial rotation: Model Rotation + Self Rotation (initially 0)
        Quaternion initialRotation = Quaternion.Euler(powerUpData.modelRotation);

        GameObject fireballObj = Instantiate(powerUpData.fireballPrefab, transform.position, initialRotation);

        if (fireballObj == null) return;

        fireballObj.transform.localScale = Vector3.one * powerUpData.fireballScale;

        OrbitingFireball fireball = fireballObj.GetComponent<OrbitingFireball>();
        if (fireball == null)
        {
            fireball = fireballObj.AddComponent<OrbitingFireball>();
        }

        fireball.Initialize(damage, powerUpData.hitCooldown);
        activeFireballs.Add(fireball);
    }

    private void RepositionFireballs()
    {
        int count = activeFireballs.Count;
        if (count == 0) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (activeFireballs[i] != null)
            {
                activeFireballs[i].SetOrbitIndex(i, angleStep);
            }
        }
    }

    private void Update()
    {
        if (powerUpData == null) return;

        // Update Orbit Angle (Movement around player)
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // Update Self Rotation Angle (Spinning on Z axis)
        currentSelfRotation += powerUpData.selfRotationSpeed * Time.deltaTime;
        if (currentSelfRotation >= 360f) currentSelfRotation -= 360f;

        UpdateFireballPositions();
    }

    private void UpdateFireballPositions()
    {
        int count = activeFireballs.Count;
        if (count == 0) return;

        Vector3 center = transform.position + Vector3.up * powerUpData.heightOffset;
        float angleStep = 360f / count;

        // Cache rotations
        Quaternion baseModelRotation = Quaternion.Euler(powerUpData.modelRotation);
        Quaternion spinRotation = Quaternion.Euler(0, 0, currentSelfRotation);

        for (int i = 0; i < count; i++)
        {
            OrbitingFireball fireball = activeFireballs[i];
            if (fireball == null)
            {
                activeFireballs.RemoveAt(i);
                i--;
                count--;
                continue;
            }

            // 1. Position Logic
            float angle = (currentAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * powerUpData.orbitRadius,
                0f,
                Mathf.Sin(angle) * powerUpData.orbitRadius
            );
            fireball.transform.position = center + offset;

            // 2. Rotation Logic
            Quaternion targetRotation;

            if (powerUpData.faceMovementDirection)
            {
                // Look ahead to find tangent/velocity direction
                float nextAngle = (currentAngle + i * angleStep + 5f) * Mathf.Deg2Rad;
                Vector3 nextPosOffset = new Vector3(
                    Mathf.Cos(nextAngle) * powerUpData.orbitRadius,
                    0f,
                    Mathf.Sin(nextAngle) * powerUpData.orbitRadius
                );

                Vector3 direction = (center + nextPosOffset) - fireball.transform.position;

                if (direction.sqrMagnitude > 0.001f)
                {
                    // Order: Look at Dir -> Apply Model correction -> Apply Self Spin (local Z)
                    targetRotation = Quaternion.LookRotation(direction) * baseModelRotation * spinRotation;
                }
                else
                {
                    targetRotation = baseModelRotation * spinRotation;
                }
            }
            else
            {
                // Order: Fixed Base Rotation -> Apply Self Spin (local Z)
                targetRotation = baseModelRotation * spinRotation;
            }

            fireball.transform.rotation = targetRotation;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null) Destroy(activeFireballs[i].gameObject);
        }
        activeFireballs.Clear();
    }

    public void ResetFireballs()
    {
        for (int i = 0; i < activeFireballs.Count; i++)
        {
            if (activeFireballs[i] != null) Destroy(activeFireballs[i].gameObject);
        }
        activeFireballs.Clear();
        SpawnFireballs();
    }
}