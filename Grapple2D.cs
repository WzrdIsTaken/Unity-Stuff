using UnityEngine;

/// <summary>
/// A simple script that allows a 2D player to grapple around!
/// I would recommend that the player is a order in layer behind the stuff they will grapple onto, because then it won't seem like the grappling hook is going through walls.
/// </summary>

[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class Grapple2D : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] DistanceJoint2D distanceJoint;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform grapplePoint; // The point you want the grapple hook to be attached to the player.

    Vector2 grappledPosition;
    bool isGrappling; // Can be used for animations, restricting input (eg: can't attack while grappling), etc. Used in script to toggle on / off the line renderer.

    void Update ()
    {
        // Input
        if (Input.GetMouseButtonDown(1) && !isGrappling) Grapple(true);
        if (Input.GetMouseButtonUp(1)) Grapple(false);

        // Grappling
        if (isGrappling)
        {
            lineRenderer.SetPosition(0, grapplePoint.position);
            lineRenderer.SetPosition(1, grappledPosition);
        }
    }

    // Grapple!
    void Grapple (bool grappling)
    {
        if (grappling)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider == null) return; // Can easily limit what the player can grapple onto with a tag check here.

            distanceJoint.enabled = true;
            distanceJoint.connectedAnchor = mousePos;

            lineRenderer.positionCount = 2;
            grappledPosition = mousePos;

            isGrappling = true;
        }
        else
        {
            distanceJoint.enabled = false;
            lineRenderer.positionCount = 0;
            isGrappling = false;
        }
    }
}
