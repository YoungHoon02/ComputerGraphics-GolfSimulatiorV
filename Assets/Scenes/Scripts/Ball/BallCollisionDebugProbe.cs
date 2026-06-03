// 테스트용이기에 읽지 마세요.
using UnityEngine;

public class BallCollisionDebugProbe : MonoBehaviour
{
    [Header("Log")]
    [SerializeField] private bool enableLog = true;
    [SerializeField] private bool logFixedUpdate = true;
    [SerializeField] private bool logCollisionStay = false;
    [SerializeField] private bool resetDistanceOnSpace = true;
    [SerializeField] private float fixedUpdateLogInterval = 0.25f;

    [Header("Runtime")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float frameDistance;
    [SerializeField] private float totalDistance;
    [SerializeField] private SurfaceType currentSurface;

    private BallPhysics ball;
    private Rigidbody rb;
    private Vector3 previousPosition;
    private float logTimer;

    private void Awake()
    {
        ball = GetComponent<BallPhysics>();
        rb = GetComponent<Rigidbody>();
        previousPosition = transform.position;

        Debug.Log($"[BallProbe] Awake | hasBallPhysics={ball != null}, hasRigidbody={rb != null}");
    }

    private void OnEnable()
    {
        ResetDistance();
        Debug.Log("[BallProbe] OnEnable");
    }

    private void FixedUpdate()
    {
        if (!enableLog)
        {
            return;
        }

        if (resetDistanceOnSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ResetDistance();
            Debug.Log("[BallProbe] ResetDistance by Space");
        }

        Vector3 currentPosition = transform.position;
        frameDistance = Vector3.Distance(previousPosition, currentPosition);
        totalDistance += frameDistance;
        previousPosition = currentPosition;

        currentSpeed = ball != null ? ball.Velocity.magnitude : 0f;
        currentSurface = ball != null ? ball.CurrentSurface : SurfaceType.None;

        if (!logFixedUpdate)
        {
            return;
        }

        logTimer += Time.fixedDeltaTime;
        if (logTimer < fixedUpdateLogInterval)
        {
            return;
        }

        logTimer = 0f;

        Debug.Log(
            $"[BallProbe] FixedUpdate | speed={currentSpeed:F3}, frameDistance={frameDistance:F3}, totalDistance={totalDistance:F3}, surface={currentSurface}, position={transform.position}"
        );
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableLog)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Debug.Log(
            $"[BallProbe] OnCollisionEnter | object={collision.gameObject.name}, layer={LayerMask.LayerToName(collision.gameObject.layer)}, point={contact.point}, normal={contact.normal}, relativeVelocity={collision.relativeVelocity}"
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!enableLog || !logCollisionStay)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Debug.Log(
            $"[BallProbe] OnCollisionStay | object={collision.gameObject.name}, layer={LayerMask.LayerToName(collision.gameObject.layer)}, point={contact.point}, normal={contact.normal}"
        );
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!enableLog)
        {
            return;
        }

        Debug.Log(
            $"[BallProbe] OnCollisionExit | object={collision.gameObject.name}, layer={LayerMask.LayerToName(collision.gameObject.layer)}"
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableLog)
        {
            return;
        }

        Debug.Log(
            $"[BallProbe] OnTriggerEnter | object={other.gameObject.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}, tag={other.tag}"
        );
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enableLog)
        {
            return;
        }

        Debug.Log(
            $"[BallProbe] OnTriggerExit | object={other.gameObject.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}, tag={other.tag}"
        );
    }

    public void ResetDistance()
    {
        previousPosition = transform.position;
        frameDistance = 0f;
        totalDistance = 0f;
        logTimer = 0f;
    }
}
