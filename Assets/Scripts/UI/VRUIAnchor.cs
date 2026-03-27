using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Positions a WorldSpace Canvas in front of the VR camera and smoothly follows
/// head movement. Call VRUIAnchor.Setup() to configure any Canvas for VR.
/// </summary>
public class VRUIAnchor : MonoBehaviour
{
    [Header("Follow Settings")]
    public float distance = 2.5f;
    public float smoothSpeed = 3f;

    private Camera vrCamera;

    /// <summary>
    /// Configures a Canvas for VR: WorldSpace rendering, XR raycaster, and camera follow.
    /// Returns the VRUIAnchor component for further configuration.
    /// </summary>
    public static VRUIAnchor Setup(Canvas canvas, float dist = 2.5f, int sortOrder = 10)
    {
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = sortOrder;

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920, 1080);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Scale: 0.0015 = 1.5x larger than base (0.001). Adjust this value to resize all UI.
        canvas.transform.localScale = Vector3.one * 0.0015f;

        // XR raycaster (reflection-based so it compiles without XRI package reference)
        AddXRRaycaster(canvas.gameObject);

        // Camera follower
        var anchor = canvas.gameObject.AddComponent<VRUIAnchor>();
        anchor.distance = dist;
        anchor.SnapToCamera();

        return anchor;
    }

    private void Start()
    {
        vrCamera = Camera.main;
        if (vrCamera != null) SnapToCamera();
    }

    private void LateUpdate()
    {
        if (vrCamera == null) { vrCamera = Camera.main; return; }

        Vector3 camPos = vrCamera.transform.position;
        Vector3 camForward = vrCamera.transform.forward;

        // Keep UI level — remove pitch so it doesn't tilt with the head
        Vector3 levelForward = new Vector3(camForward.x, 0f, camForward.z).normalized;
        if (levelForward.sqrMagnitude < 0.001f) levelForward = Vector3.forward;

        Vector3 targetPos = camPos + levelForward * distance;
        Quaternion targetRot = Quaternion.LookRotation(levelForward, Vector3.up);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
    }

    /// <summary>Instantly snaps to the camera position (no lerp).</summary>
    public void SnapToCamera()
    {
        if (vrCamera == null) vrCamera = Camera.main;
        if (vrCamera == null) return;

        Vector3 camForward = vrCamera.transform.forward;
        Vector3 levelForward = new Vector3(camForward.x, 0f, camForward.z).normalized;
        if (levelForward.sqrMagnitude < 0.001f) levelForward = Vector3.forward;

        transform.position = vrCamera.transform.position + levelForward * distance;
        transform.rotation = Quaternion.LookRotation(levelForward, Vector3.up);
    }

    /// <summary>
    /// Adds TrackedDeviceGraphicRaycaster if XRI is available, otherwise GraphicRaycaster.
    /// Uses reflection so it compiles regardless of whether XRI package is installed.
    /// </summary>
    private static void AddXRRaycaster(GameObject obj)
    {
        var type = System.Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (type != null)
            obj.AddComponent(type);
        else
            obj.AddComponent<GraphicRaycaster>();
    }
}
