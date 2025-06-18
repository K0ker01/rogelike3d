using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class SuperCharacterController : MonoBehaviour
{
    [SerializeField]
    Vector3 debugMove = Vector3.zero;

    [SerializeField]
    QueryTriggerInteraction triggerInteraction;

    [SerializeField]
    bool fixedTimeStep;

    [SerializeField]
    int fixedUpdatesPerSecond;

    [SerializeField]
    bool clampToMovingGround;

    [SerializeField]
    bool debugSpheres;

    [SerializeField]
    bool debugGrounding;

    [SerializeField]
    bool debugPushbackMesssages;

    [SerializeField]
    public struct Ground
    {
        public RaycastHit hit { get; set; }
        public RaycastHit nearHit { get; set; }
        public RaycastHit farHit { get; set; }
        public RaycastHit secondaryHit { get; set; }
        public SuperCollisionType collisionType { get; set; }
        public Transform transform { get; set; }

        public Ground(RaycastHit hit, RaycastHit nearHit, RaycastHit farHit, RaycastHit secondaryHit, SuperCollisionType superCollisionType, Transform hitTransform)
        {
            this.hit = hit;
            this.nearHit = nearHit;
            this.farHit = farHit;
            this.secondaryHit = secondaryHit;
            this.collisionType = superCollisionType;
            this.transform = hitTransform;
        }
    }

    [SerializeField]
    CollisionSphere[] spheres =
        new CollisionSphere[3] {
            new CollisionSphere(0.5f, true, false),
            new CollisionSphere(1.0f, false, false),
            new CollisionSphere(1.5f, false, true),
        };

    public LayerMask Walkable;

    [SerializeField]
    Collider ownCollider;

    [SerializeField]
    public float radius = 0.5f;

    public float deltaTime { get; private set; }
    public SuperGround currentGround { get; private set; }
    public CollisionSphere feet { get; private set; }
    public CollisionSphere head { get; private set; }

    public float height { get { return Vector3.Distance(SpherePosition(head), SpherePosition(feet)) + radius * 2; } }

    public Vector3 up { get { return transform.up; } }
    public Vector3 down { get { return -transform.up; } }
    public List<SuperCollision> collisionData { get; private set; }
    public Transform currentlyClampedTo { get; set; }
    public float heightScale { get; set; }
    public float radiusScale { get; set; }
    public bool manualUpdateOnly { get; set; }

    public delegate void UpdateDelegate();
    public event UpdateDelegate AfterSingleUpdate;

    private Vector3 initialPosition;
    private Vector3 groundOffset;
    private Vector3 lastGroundPosition;
    private bool clamping = true;
    private bool slopeLimiting = true;

    private List<Collider> ignoredColliders;
    private List<IgnoredCollider> ignoredColliderStack;

    private const float Tolerance = 0.05f;
    private const float TinyTolerance = 0.01f;
    private const string TemporaryLayer = "TempCast";
    private const int MaxPushbackIterations = 2;
    private int TemporaryLayerIndex;
    private float fixedDeltaTime;

    private static SuperCollisionType defaultCollisionType;

    void Awake()
    {
        collisionData = new List<SuperCollision>();

        TemporaryLayerIndex = LayerMask.NameToLayer(TemporaryLayer);

        ignoredColliders = new List<Collider>();
        ignoredColliderStack = new List<IgnoredCollider>();

        currentlyClampedTo = null;

        fixedDeltaTime = 1.0f / fixedUpdatesPerSecond;

        heightScale = 1.0f;

        if (ownCollider)
            IgnoreCollider(ownCollider);

        foreach (var sphere in spheres)
        {
            if (sphere.isFeet)
                feet = sphere;

            if (sphere.isHead)
                head = sphere;
        }

        if (feet == null)
            Debug.LogError("[SuperCharacterController] Feet not found on controller");

        if (head == null)
            Debug.LogError("[SuperCharacterController] Head not found on controller");

        if (defaultCollisionType == null)
            defaultCollisionType = new GameObject("DefaultSuperCollisionType", typeof(SuperCollisionType)).GetComponent<SuperCollisionType>();

        currentGround = new SuperGround(Walkable, this, triggerInteraction);

        manualUpdateOnly = false;

        gameObject.SendMessage("SuperStart", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        if (manualUpdateOnly)
            return;

        if (!fixedTimeStep)
        {
            deltaTime = Time.deltaTime;

            SingleUpdate();
            return;
        }
        else
        {
            float delta = Time.deltaTime;

            while (delta > fixedDeltaTime)
            {
                deltaTime = fixedDeltaTime;

                SingleUpdate();

                delta -= fixedDeltaTime;
            }

            if (delta > 0f)
            {
                deltaTime = delta;

                SingleUpdate();
            }
        }
    }

    public void ManualUpdate(float deltaTime)
    {
        this.deltaTime = deltaTime;

        SingleUpdate();
    }

    void SingleUpdate()
    {
        bool isClamping = clamping || currentlyClampedTo != null;
        Transform clampedTo = currentlyClampedTo != null ? currentlyClampedTo : currentGround.transform;

        if (clampToMovingGround && isClamping && clampedTo != null && clampedTo.position - lastGroundPosition != Vector3.zero)
            transform.position += clampedTo.position - lastGroundPosition;

        initialPosition = transform.position;

        ProbeGround(1);

        transform.position += debugMove * deltaTime;

        gameObject.SendMessage("SuperUpdate", SendMessageOptions.DontRequireReceiver);

        collisionData.Clear();

        RecursivePushback(0, MaxPushbackIterations);

        ProbeGround(2);

        if (slopeLimiting)
            SlopeLimit();

        ProbeGround(3);

        if (clamping)
            ClampToGround();

        isClamping = clamping || currentlyClampedTo != null;
        clampedTo = currentlyClampedTo != null ? currentlyClampedTo : currentGround.transform;

        if (isClamping)
            lastGroundPosition = clampedTo.position;

        if (debugGrounding)
            currentGround.DebugGround(true, true, true, true, true);

        if (AfterSingleUpdate != null)
            AfterSingleUpdate();
    }

    void ProbeGround(int iter)
    {
        PushIgnoredColliders();
        currentGround.ProbeGround(SpherePosition(feet), iter);
        PopIgnoredColliders();
    }

    bool SlopeLimit()
    {
        Vector3 n = currentGround.PrimaryNormal();
        float a = Vector3.Angle(n, up);

        if (a > currentGround.superCollisionType.SlopeLimit)
        {
            Vector3 absoluteMoveDirection = Math3d.ProjectVectorOnPlane(n, transform.position - initialPosition);

            Vector3 r = Vector3.Cross(n, down);
            Vector3 v = Vector3.Cross(r, n);

            float angle = Vector3.Angle(absoluteMoveDirection, v);

            if (angle <= 90.0f)
                return false;

            Vector3 resolvedPosition = Math3d.ProjectPointOnLine(initialPosition, r, transform.position);
            Vector3 direction = Math3d.ProjectVectorOnPlane(n, resolvedPosition - transform.position);

            RaycastHit hit;

            if (Physics.CapsuleCast(SpherePosition(feet), SpherePosition(head), radius, direction.normalized, out hit, direction.magnitude, Walkable, triggerInteraction))
            {
                transform.position += v.normalized * hit.distance;
            }
            else
            {
                transform.position += direction;
            }

            return true;
        }

        return false;
    }

    void ClampToGround()
    {
        float d = currentGround.Distance();
        transform.position -= up * d;
    }

    public void EnableClamping()
    {
        clamping = true;
    }

    public void DisableClamping()
    {
        clamping = false;
    }

    public void EnableSlopeLimit()
    {
        slopeLimiting = true;
    }

    public void DisableSlopeLimit()
    {
        slopeLimiting = false;
    }

    public bool IsClamping()
    {
        return clamping;
    }

    void RecursivePushback(int depth, int maxDepth)
    {
        PushIgnoredColliders();

        bool contact = false;

        foreach (var sphere in spheres)
        {
            foreach (Collider col in Physics.OverlapSphere((SpherePosition(sphere)), radius, Walkable, triggerInteraction))
            {
                Vector3 position = SpherePosition(sphere);
                Vector3 contactPoint;
                bool contactPointSuccess = SuperCollider.ClosestPointOnSurface(col, position, radius, out contactPoint);
                
                if (!contactPointSuccess)
                {
                    return;
                }
                                            
                if (debugPushbackMesssages)
                    DebugDraw.DrawMarker(contactPoint, 2.0f, Color.cyan, 0.0f, false);
                    
                Vector3 v = contactPoint - position;
                if (v != Vector3.zero)
                {
                    int layer = col.gameObject.layer;

                    col.gameObject.layer = TemporaryLayerIndex;

                    bool facingNormal = Physics.SphereCast(new Ray(position, v.normalized), TinyTolerance, v.magnitude + TinyTolerance, 1 << TemporaryLayerIndex);

                    col.gameObject.layer = layer;

                    if (facingNormal)
                    {
                        if (Vector3.Distance(position, contactPoint) < radius)
                        {
                            v = v.normalized * (radius - v.magnitude) * -1;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        v = v.normalized * (radius + v.magnitude);
                    }

                    contact = true;

                    transform.position += v;

                    col.gameObject.layer = TemporaryLayerIndex;

                    RaycastHit normalHit;

                    Physics.SphereCast(new Ray(position + v, contactPoint - (position + v)), TinyTolerance, out normalHit, 1 << TemporaryLayerIndex);

                    col.gameObject.layer = layer;

                    SuperCollisionType superColType = col.gameObject.GetComponent<SuperCollisionType>();

                    if (superColType == null)
                        superColType = defaultCollisionType;

                    var collision = new SuperCollision()
                    {
                        collisionSphere = sphere,
                        superCollisionType = superColType,
                        gameObject = col.gameObject,
                        point = contactPoint,
                        normal = normalHit.normal
                    };

                    collisionData.Add(collision);
                }
            }            
        }

        PopIgnoredColliders();

        if (depth < maxDepth && contact)
        {
            RecursivePushback(depth + 1, maxDepth);
        }
    }

    protected struct IgnoredCollider
    {
        public Collider collider;
        public int layer;

        public IgnoredCollider(Collider collider, int layer)
        {
            this.collider = collider;
            this.layer = layer;
        }
    }

    private void PushIgnoredColliders()
    {
        ignoredColliderStack.Clear();

        for (int i = 0; i < ignoredColliders.Count; i++)
        {
            Collider col = ignoredColliders[i];
            ignoredColliderStack.Add(new IgnoredCollider(col, col.gameObject.layer));
            col.gameObject.layer = TemporaryLayerIndex;
        }
    }

    private void PopIgnoredColliders()
    {
        for (int i = 0; i < ignoredColliderStack.Count; i++)
        {
            IgnoredCollider ic = ignoredColliderStack[i];
            ic.collider.gameObject.layer = ic.layer;
        }

        ignoredColliderStack.Clear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (debugSpheres)
        {
            if (spheres != null)
            {
                if (heightScale == 0) heightScale = 1;

                foreach (var sphere in spheres)
                {
                    Gizmos.color = sphere.isFeet ? Color.green : (sphere.isHead ? Color.yellow : Color.cyan);
                    Gizmos.DrawWireSphere(SpherePosition(sphere), radius);
                }
            }
        }
    }

    public Vector3 SpherePosition(CollisionSphere sphere)
    {
        if (sphere.isFeet)
            return transform.position + sphere.offset * up;
        else
            return transform.position + sphere.offset * up * heightScale;
    }

    public bool PointBelowHead(Vector3 point)
    {
        return Vector3.Angle(point - SpherePosition(head), up) > 89.0f;
    }

    public bool PointAboveFeet(Vector3 point)
    {
        return Vector3.Angle(point - SpherePosition(feet), down) > 89.0f;
    }

    public void IgnoreCollider(Collider col)
    {
        ignoredColliders.Add(col);
    }

    public void RemoveIgnoredCollider(Collider col)
    {
        ignoredColliders.Remove(col);
    }

    public void ClearIgnoredColliders()
    {
        ignoredColliders.Clear();
    }

    public class SuperGround
    {
        public SuperGround(LayerMask walkable, SuperCharacterController controller, QueryTriggerInteraction triggerInteraction)
        {
            this.walkable = walkable;
            this.controller = controller;
            this.triggerInteraction = triggerInteraction;
        }

        private class GroundHit
        {
            public Vector3 point { get; private set; }
            public Vector3 normal { get; private set; }
            public float distance { get; private set; }

            public GroundHit(Vector3 point, Vector3 normal, float distance)
            {
                this.point = point;
                this.normal = normal;
                this.distance = distance;
            }
        }

        private LayerMask walkable;
        private SuperCharacterController controller;
        private QueryTriggerInteraction triggerInteraction;

        private GroundHit primaryGround;
        private GroundHit nearGround;
        private GroundHit farGround;
        private GroundHit stepGround;
        private GroundHit flushGround;

        public SuperCollisionType superCollisionType { get; private set; }
        public Transform transform { get; private set; }

        private const float groundingUpperBoundAngle = 60.0f;
        private const float groundingMaxPercentFromCenter = 0.85f;
        private const float groundingMinPercentFromcenter = 0.50f;

        public void ProbeGround(Vector3 origin, int iter)
        {
            ResetGrounds();

            Vector3 up = controller.up;
            Vector3 down = -up;

            Vector3 o = origin + (up * Tolerance);

            float smallerRadius = controller.radius - (Tolerance * Tolerance);

            RaycastHit hit;

            if (Physics.SphereCast(o, smallerRadius, down, out hit, Mathf.Infinity, walkable, triggerInteraction))
            {
                var superColType = hit.collider.gameObject.GetComponent<SuperCollisionType>();

                if (superColType == null)
                {
                    superColType = defaultCollisionType;
                }

                superCollisionType = superColType;
                transform = hit.transform;

                SimulateSphereCast(hit.normal, out hit);

                primaryGround = new GroundHit(hit.point, hit.normal, hit.distance);

                if (Vector3.Distance(Math3d.ProjectPointOnPlane(controller.up, controller.transform.position, hit.point), controller.transform.position) < TinyTolerance)
                {
                    return;
                }


                Vector3 toCenter = Math3d.ProjectVectorOnPlane(up, (controller.transform.position - hit.point).normalized * TinyTolerance);

                Vector3 awayFromCenter = Quaternion.AngleAxis(-80.0f, Vector3.Cross(toCenter, up)) * -toCenter;

                Vector3 nearPoint = hit.point + toCenter + (up * TinyTolerance);
                Vector3 farPoint = hit.point + (awayFromCenter * 3);

                RaycastHit nearHit;
                RaycastHit farHit;

                Physics.Raycast(nearPoint, down, out nearHit, Mathf.Infinity, walkable, triggerInteraction);
                Physics.Raycast(farPoint, down, out farHit, Mathf.Infinity, walkable, triggerInteraction);

                nearGround = new GroundHit(nearHit.point, nearHit.normal, nearHit.distance);
                farGround = new GroundHit(farHit.point, farHit.normal, farHit.distance);

                if (Vector3.Angle(hit.normal, up) > superColType.StandAngle)
                {
                    Vector3 r = Vector3.Cross(hit.normal, down);
                    Vector3 v = Vector3.Cross(r, hit.normal);

                    Vector3 flushOrigin = hit.point + hit.normal * TinyTolerance;

                    RaycastHit flushHit;

                    if (Physics.Raycast(flushOrigin, v, out flushHit, Mathf.Infinity, walkable, triggerInteraction))
                    {
                        RaycastHit sphereCastHit;

                        if (SimulateSphereCast(flushHit.normal, out sphereCastHit))
                        {
                            flushGround = new GroundHit(sphereCastHit.point, sphereCastHit.normal, sphereCastHit.distance);
                        }
                        else
                        {
                        }
                    }
                }

                if (Vector3.Angle(nearHit.normal, up) > superColType.StandAngle || nearHit.distance > Tolerance)
                {
                    SuperCollisionType col = null;
                
                    if (nearHit.collider != null)
                    {
                        col = nearHit.collider.gameObject.GetComponent<SuperCollisionType>();
                    }
                    
                    if (col == null)
                    {
                        col = defaultCollisionType;
                    }

                    if (Vector3.Angle(nearHit.normal, up) > col.StandAngle)
                    {
                        Vector3 r = Vector3.Cross(nearHit.normal, down);
                        Vector3 v = Vector3.Cross(r, nearHit.normal);

                        RaycastHit stepHit;

                        if (Physics.Raycast(nearPoint, v, out stepHit, Mathf.Infinity, walkable, triggerInteraction))
                        {
                            stepGround = new GroundHit(stepHit.point, stepHit.normal, stepHit.distance);
                        }
                    }
                    else
                    {
                        stepGround = new GroundHit(nearHit.point, nearHit.normal, nearHit.distance);
                    }
                }
            }
            else if (Physics.Raycast(o, down, out hit, Mathf.Infinity, walkable, triggerInteraction))
            {
                var superColType = hit.collider.gameObject.GetComponent<SuperCollisionType>();

                if (superColType == null)
                {
                    superColType = defaultCollisionType;
                }

                superCollisionType = superColType;
                transform = hit.transform;

                RaycastHit sphereCastHit;

                if (SimulateSphereCast(hit.normal, out sphereCastHit))
                {
                    primaryGround = new GroundHit(sphereCastHit.point, sphereCastHit.normal, sphereCastHit.distance);
                }
                else
                {
                    primaryGround = new GroundHit(hit.point, hit.normal, hit.distance);
                }
            }
            else
            {
                Debug.LogError("[SuperCharacterComponent]: No ground was found below the player; player has escaped level");
            }
        }

        private void ResetGrounds()
        {
            primaryGround = null;
            nearGround = null;
            farGround = null;
            flushGround = null;
            stepGround = null;
        }

        public bool IsGrounded(bool currentlyGrounded, float distance)
        {
            Vector3 n;
            return IsGrounded(currentlyGrounded, distance, out n);
        }

        public bool IsGrounded(bool currentlyGrounded, float distance, out Vector3 groundNormal)
        {
            groundNormal = Vector3.zero;

            if (primaryGround == null || primaryGround.distance > distance)
            {
                return false;
            }

            if (farGround != null && Vector3.Angle(farGround.normal, controller.up) > superCollisionType.StandAngle)
            {
                if (flushGround != null && Vector3.Angle(flushGround.normal, controller.up) < superCollisionType.StandAngle && flushGround.distance < distance)
                {
                    groundNormal = flushGround.normal;
                    return true;
                }

                return false;
            }

            if (farGround != null && !OnSteadyGround(farGround.normal, primaryGround.point))
            {
                if (nearGround != null && nearGround.distance < distance && Vector3.Angle(nearGround.normal, controller.up) < superCollisionType.StandAngle && !OnSteadyGround(nearGround.normal, nearGround.point))
                {
                    groundNormal = nearGround.normal;
                    return true;
                }

                if (stepGround != null && stepGround.distance < distance && Vector3.Angle(stepGround.normal, controller.up) < superCollisionType.StandAngle)
                {
                    groundNormal = stepGround.normal;
                    return true;
                }

                return false;
            }


            if (farGround != null)
            {
                groundNormal = farGround.normal;
            }
            else
            {
                groundNormal = primaryGround.normal;
            }

            return true;
        }

        private bool OnSteadyGround(Vector3 normal, Vector3 point)
        {
            float angle = Vector3.Angle(normal, controller.up);

            float angleRatio = angle / groundingUpperBoundAngle;

            float distanceRatio = Mathf.Lerp(groundingMinPercentFromcenter, groundingMaxPercentFromCenter, angleRatio);

            Vector3 p = Math3d.ProjectPointOnPlane(controller.up, controller.transform.position, point);

            float distanceFromCenter = Vector3.Distance(p, controller.transform.position);

            return distanceFromCenter <= distanceRatio * controller.radius;
        }

        public Vector3 PrimaryNormal()
        {
            return primaryGround.normal;
        }

        public float Distance()
        {
            return primaryGround.distance;
        }

        public void DebugGround(bool primary, bool near, bool far, bool flush, bool step)
        {
            if (primary && primaryGround != null)
            {
                DebugDraw.DrawVector(primaryGround.point, primaryGround.normal, 2.0f, 1.0f, Color.yellow, 0, false);
            }

            if (near && nearGround != null)
            {
                DebugDraw.DrawVector(nearGround.point, nearGround.normal, 2.0f, 1.0f, Color.blue, 0, false);
            }

            if (far && farGround != null)
            {
                DebugDraw.DrawVector(farGround.point, farGround.normal, 2.0f, 1.0f, Color.red, 0, false);
            }

            if (flush && flushGround != null)
            {
                DebugDraw.DrawVector(flushGround.point, flushGround.normal, 2.0f, 1.0f, Color.cyan, 0, false);
            }

            if (step && stepGround != null)
            {
                DebugDraw.DrawVector(stepGround.point, stepGround.normal, 2.0f, 1.0f, Color.green, 0, false);
            }
        }

        private bool SimulateSphereCast(Vector3 groundNormal, out RaycastHit hit)
        {
            float groundAngle = Vector3.Angle(groundNormal, controller.up) * Mathf.Deg2Rad;

            Vector3 secondaryOrigin = controller.transform.position + controller.up * Tolerance;

            if (!Mathf.Approximately(groundAngle, 0))
            {
                float horizontal = Mathf.Sin(groundAngle) * controller.radius;
                float vertical = (1.0f - Mathf.Cos(groundAngle)) * controller.radius;

                Vector3 r2 = Vector3.Cross(groundNormal, controller.down);
                Vector3 v2 = -Vector3.Cross(r2, groundNormal);

                secondaryOrigin += Math3d.ProjectVectorOnPlane(controller.up, v2).normalized * horizontal + controller.up * vertical;
            }
            
            if (Physics.Raycast(secondaryOrigin, controller.down, out hit, Mathf.Infinity, walkable, triggerInteraction))
            {
                hit.distance -= Tolerance + TinyTolerance;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

[Serializable]
public class CollisionSphere
{
    public float offset;
    public bool isFeet;
    public bool isHead;

    public CollisionSphere(float offset, bool isFeet, bool isHead)
    {
        this.offset = offset;
        this.isFeet = isFeet;
        this.isHead = isHead;
    }
}

public struct SuperCollision
{
    public CollisionSphere collisionSphere;
    public SuperCollisionType superCollisionType;
    public GameObject gameObject;
    public Vector3 point;
    public Vector3 normal;
}
