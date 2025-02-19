using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyScript : MonoBehaviour
{
    [Header("Detection Ranges")]
    [SerializeField] private float sprintDetectionRadius = 30f;
    [SerializeField] private float walkDetectionRadius = 15f;
    [SerializeField] private float crouchDetectionRadius = 5f;

    [Header("Detection Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionInterval = 0.2f;

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 7f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float minDistanceToTarget = 2f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 20f;
    [SerializeField] private float minPatrolWaitTime = 2f;
    [SerializeField] private float maxPatrolWaitTime = 5f;
    [SerializeField] private int maxPatrolAttempts = 30;

    private Transform player;
    private FirstPersonController playerMovement;
    private NavMeshAgent agent;
    private float detectionTimer;
    private bool isPlayerDetected;
    private Vector3 lastKnownPosition;
   
    // Patrol state variables
    private Vector3 currentPatrolPoint;
    private float waitTimer;
    private bool isWaiting;
    private bool hasPatrolPoint;

    private void Start()
    {
        // Initialize components
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<FirstPersonController>();

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement component not found on player!");
        }

        // Set initial agent settings
        agent.speed = patrolSpeed;
        agent.stoppingDistance = minDistanceToTarget;

        // Start patrolling
        SetNewPatrolPoint();
    }

    private void Update()
    {
        detectionTimer += Time.deltaTime;

        // Check for player detection at regular intervals
        if (detectionTimer >= detectionInterval)
        {
            CheckPlayerSound();
            detectionTimer = 0f;
        }

        // Update monster behavior
        UpdateMonsterBehavior();
    }

    private void UpdateMonsterBehavior()
    {
        if (isPlayerDetected)
        {
            // Chase behavior
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);

            // Rotate towards player
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else if (lastKnownPosition != Vector3.zero)
        {
            // Investigate last known position
            agent.speed = patrolSpeed;
            if (Vector3.Distance(transform.position, lastKnownPosition) <= minDistanceToTarget)
            {
                lastKnownPosition = Vector3.zero;
                SetNewPatrolPoint();
            }
            else
            {
                agent.SetDestination(lastKnownPosition);
            }
        }
        else
        {
            // Patrol behavior
            UpdatePatrolBehavior();
        }
    }

    private void UpdatePatrolBehavior()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                SetNewPatrolPoint();
            }
            return;
        }

        if (!hasPatrolPoint)
        {
            SetNewPatrolPoint();
            return;
        }

        // Check if we've reached the patrol point
        if (Vector3.Distance(transform.position, currentPatrolPoint) <= minDistanceToTarget)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
        hasPatrolPoint = false;
    }

    private void SetNewPatrolPoint()
    {
        Vector3 randomPoint = Vector3.zero;
        bool foundPoint = false;

        // Try to find a valid point on the NavMesh
        for (int i = 0; i < maxPatrolAttempts; i++)
        {
            // Generate random point within patrol radius
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Check if point is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
            {
                currentPatrolPoint = hit.position;
                agent.SetDestination(currentPatrolPoint);
                hasPatrolPoint = true;
                foundPoint = true;
                break;
            }
        }

        // If no valid point found, wait and try again
        if (!foundPoint)
        {
            StartWaiting();
        }
    }

    private void CheckPlayerSound()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float currentDetectionRadius = GetCurrentDetectionRadius();

        // Check if player is within detection radius
        if (distanceToPlayer <= currentDetectionRadius)
        {
            // Perform raycast to check for obstacles between monster and player
            if (HasLineOfSight())
            {
                OnPlayerDetected();
                lastKnownPosition = player.position;
            }
            else
            {
                OnPlayerLost();
            }
        }
        else
        {
            OnPlayerLost();
        }
    }

    private float GetCurrentDetectionRadius()
    {
        if (playerMovement.isSprinting)
        {
            return sprintDetectionRadius;
        }
        else if (playerMovement.isWalking)
        {
            return walkDetectionRadius;
        }
        else // Crouching
        {
            return crouchDetectionRadius;
        }
    }

    private bool HasLineOfSight()
    {
        RaycastHit hit;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Check if there are any obstacles between the monster and player
        if (Physics.Raycast(transform.position, directionToPlayer, out hit, sprintDetectionRadius))
        {
            return hit.transform.CompareTag("Player");
        }

        return false;
    }

    private void OnPlayerDetected()
    {
        if (!isPlayerDetected)
        {
            isPlayerDetected = true;
            // Add additional detection behavior here
            // For example: Play alert sound, animation, etc.
        }
    }

    private void OnPlayerLost()
    {
        if (isPlayerDetected)
        {
            isPlayerDetected = false;
            // Add additional behavior when losing the player
            // For example: Play confused sound, search animation, etc.
        }
    }

    // Optional: Visual debugging in Unity Editor
    private void OnDrawGizmosSelected()
    {
        // Draw detection radiuses
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sprintDetectionRadius);
       
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkDetectionRadius);
       
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, crouchDetectionRadius);

        // Draw patrol radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        // Draw current patrol point and last known position
        if (Application.isPlaying)
        {
            if (hasPatrolPoint)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(currentPatrolPoint, 1f);
                Gizmos.DrawLine(transform.position, currentPatrolPoint);
            }

            if (lastKnownPosition != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(lastKnownPosition, 1f);
            }
        }
    }
}