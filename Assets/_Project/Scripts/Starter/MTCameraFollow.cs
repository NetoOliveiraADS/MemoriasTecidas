using UnityEngine;

public sealed class MTCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 position = transform.position;
        position.x = target.position.x;
        position.y = target.position.y;
        transform.position = position;
    }
}
