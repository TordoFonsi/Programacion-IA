using UnityEngine;

public enum ZoneType
{
    Initial,
    HuntingGround,
    SafeZone,
    Exit
}

public class Zone : MonoBehaviour
{
    [SerializeField] private ZoneType type;
    [SerializeField] private Vector3 size = new Vector3(10f, 1f, 10f);
    [SerializeField] private Color color = Color.white;

    public ZoneType Type => type;
    public Vector3 Center => transform.position;
    public Vector3 Size => size;

    public void Configure(ZoneType zoneType, Vector3 zoneSize, Color zoneColor)
    {
        type = zoneType;
        size = zoneSize;
        color = zoneColor;
    }

    public bool Contains(Vector3 position)
    {
        Vector3 local = position - transform.position;
        return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
    }

    private void OnEnable()
    {
        ZoneRegistry.Register(this);
    }

    private void OnDisable()
    {
        ZoneRegistry.Unregister(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, size);
    }
}
