using UnityEditor;
using UnityEngine;

public class BRPDecalGizmos
{
    /// <summary>
    /// Transform에 따라 큐브형태의 기즈모 표현
    /// </summary>
    public void Draw(Transform transform)
    {
        Gizmos.color = Color.gray;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        Handles.color = Color.gray;
        float scale = transform.lossyScale.y;

        Vector3 start = transform.position + transform.up * 0.25f * scale;
        Vector3 end = start + (-transform.up * 0.5f * scale);
        Handles.DrawLine(start, end);
        float arrowSize = 0.1f * scale;
        Vector3 left = end + (-transform.right + transform.up) * arrowSize;
        Vector3 right = end + (transform.right + transform.up) * arrowSize;

        Handles.DrawLine(end, left);
        Handles.DrawLine(end, right);
    }
}
