using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;

public class ProjectileDando : MonoBehaviour
{
    public float spawnChance;
    public int spawnMinCount;
    public int spawnMaxCount;
    public VisualEffect vfx;

    public int Count;

    private Vector3 spawn;
    public GameObject Target;
    private Vector3 target;
    public float Power;
    public float gravity = 9.81f;
    private GraphicsBuffer projectileBuffer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawn = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            target = Target.transform.position;
            Shoot();
        }
        //if (UnityEngine.Random.value < spawnChance)
        //{
        //    spawnCount = UnityEngine.Random.Range(spawnMinCount, spawnMaxCount);
        //    TransformData[] bufferData = new TransformData[spawnCount];
        //    projectileBuffer?.Release();
        //    projectileBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spawnCount, Marshal.SizeOf(typeof(TransformData)));
        //    for (int i = 0; i < spawnCount; i++)
        //    {
        //        bufferData[i] = new TransformData(new Vector3(UnityEngine.Random.Range(-50, 50), 3, UnityEngine.Random.Range(-50, 50)), new Vector3(UnityEngine.Random.Range(-10, 10), 3, UnityEngine.Random.Range(-10, 10)));
        //    }
        //    projectileBuffer.SetData(bufferData);
        //    vfx.SetGraphicsBuffer("LaunchData", projectileBuffer);
        //    vfx.SendEvent("Launch");
        //}
    }
    void Shoot()
    {
        TransformData VFXdata = new TransformData(spawn, CalculateLaunchDirection(spawn, target, Power) * Power);
        int spawnCount = Count;
        TransformData[] bufferData = new TransformData[spawnCount];
        projectileBuffer?.Release();
        projectileBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, spawnCount, Marshal.SizeOf(typeof(TransformData)));
        for (int i = 0; i < spawnCount; i++)
        {
            bufferData[i] = VFXdata;
        }
        projectileBuffer.SetData(bufferData);
        vfx.SetGraphicsBuffer("LaunchData", projectileBuffer);
        vfx.SendEvent("Launch");
        Debug.Log($"spawns a cube at {spawn}");
    }
    private Vector3 CalculateLaunchDirection(Vector3 start, Vector3 target, float velocity)
    {
        Vector3 displacement = target - start;
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
        float verticalDistance = displacement.y;

        float velocitySquared = velocity * velocity;
        float determinant = velocitySquared * velocitySquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * velocitySquared);

        if (determinant < 0)
        {
            Debug.Log("didnt calc angle");
            return Vector3.zero; // No valid launch direction
        }

        float sqrtDet = Mathf.Sqrt(determinant);
        float angle = Mathf.Atan2(velocitySquared - sqrtDet, gravity * horizontalDistance); // Using the lower angle

        Vector3 horizontalDirection = new Vector3(displacement.x, 0, displacement.z).normalized;
        float horizontalSpeed = Mathf.Cos(angle) * velocity;
        float verticalSpeed = Mathf.Sin(angle) * velocity;
        Debug.Log("calculated angle");
        return (horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed).normalized;
    }
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    struct TransformData
    {
        public Vector3 Position;
        public Vector3 Direction;

        public TransformData(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}
