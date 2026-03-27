using UnityEngine;

public class LockXROriginY : MonoBehaviour
{
    private float fixedY;

    void Start()
    {
        // 记录一开始 Inspector 里的 Y 值
        fixedY = transform.position.y;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.y = fixedY;
        transform.position = pos;
    }
}