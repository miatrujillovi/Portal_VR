
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using UnityEngine;

public class PortalPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RayInteractor rightRayInteractor;
    [SerializeField] private RayInteractor leftRayInteractor;
    [SerializeField] private GameObject portalPreviewPrefab;
    [SerializeField] private GameObject rightPortalPrefab;
    [SerializeField] private GameObject leftPortalPrefab;

    public bool rightIsLeft = false;

    [Header("Portal Settings")]
    [SerializeField] private Vector2 wallSize = new(0.9f, 1.4f);
    [SerializeField] private Vector2 floorSize = new(0.9f, 0.9f);
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float surfaceOffset = 0.02f;
    [SerializeField] private float edgeMargin = 0.03f;

    private GameObject previewInstance;
    private GameObject rightPortalInstance;
    private GameObject leftPortalInstance;

    enum PortalType
    {
        Right, Left
    }

    private void Update()
    {
        if (rightRayInteractor == null || portalPreviewPrefab == null)
        {
            return;
        }

        if (MRUK.Instance == null)
        {
            HidePreview();
            return;
        }

        MRUKRoom room = MRUK.Instance.GetCurrentRoom();

        if (room == null)
        {
            HidePreview();
            return;
        }

        Ray ray = rightRayInteractor.Ray;

        if (!TryFindSurface(room, ray, out MRUKAnchor anchor,
                out RaycastHit hit, out bool isWall))
        {
            HidePreview();
            return;
        }

        Vector2 portalSize = isWall ? wallSize : floorSize;

        if (!CanPlacePortal(anchor, hit.point, portalSize))
        {
            HidePreview();
            return;
        }

        Vector3 normal = anchor.transform.forward;
        Quaternion rotation = anchor.transform.rotation;

        // Keep the front of the portal facing the user.
        if (Vector3.Dot(normal, ray.direction) > 0f)
        {
            normal = -normal;

            rotation = Quaternion.AngleAxis(
                180f, anchor.transform.up) * rotation;
        }

        Vector3 position = hit.point + normal * surfaceOffset;

        UpdatePreview(position, rotation, portalSize);

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            if (rightIsLeft)
            {
                PlacePortal(position, rotation, portalSize, PortalType.Left);
            }
            else
            {
                PlacePortal(position, rotation, portalSize, PortalType.Right);
            }
        } 
        /*else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            PlacePortal(position, rotation, portalSize, PortalType.Left);
        }*/
    }

    private bool TryFindSurface(
        MRUKRoom room,
        Ray ray,
        out MRUKAnchor selectedAnchor,
        out RaycastHit selectedHit,
        out bool isWall)
    {
        selectedAnchor = null;
        selectedHit = default;
        isWall = false;

        float closestDistance = maxDistance;

        foreach (MRUKAnchor wall in room.WallAnchors)
        {
            if (wall.Raycast(ray, closestDistance, out RaycastHit hit))
            {
                float distance = Vector3.Distance(
                    ray.origin, hit.point);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    selectedAnchor = wall;
                    selectedHit = hit;
                    isWall = true;
                }
            }
        }

        MRUKAnchor floor = room.FloorAnchor;

        if (floor != null &&
            floor.Raycast(ray, closestDistance, out RaycastHit floorHit))
        {
            float distance = Vector3.Distance(
                ray.origin, floorHit.point);

            if (distance < closestDistance)
            {
                selectedAnchor = floor;
                selectedHit = floorHit;
                isWall = false;
            }
        }

        return selectedAnchor != null;
    }

    private bool CanPlacePortal(
        MRUKAnchor anchor, Vector3 worldPoint, Vector2 size)
    {
        if (!anchor.PlaneRect.HasValue)
        {
            return false;
        }

        Rect rect = anchor.PlaneRect.Value;
        Vector3 local = anchor.transform.InverseTransformPoint(worldPoint);

        float halfWidth = size.x * 0.5f + edgeMargin;
        float halfHeight = size.y * 0.5f + edgeMargin;

        Vector2[] corners =
        {
            new(local.x - halfWidth, local.y - halfHeight),
            new(local.x + halfWidth, local.y - halfHeight),
            new(local.x + halfWidth, local.y + halfHeight),
            new(local.x - halfWidth, local.y + halfHeight)
        };

        foreach (Vector2 corner in corners)
        {
            if (!rect.Contains(corner))
            {
                return false;
            }

            if (anchor.PlaneBoundary2D != null &&
                anchor.PlaneBoundary2D.Count >= 3 &&
                !anchor.IsPositionInBoundary(corner))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdatePreview(
        Vector3 position, Quaternion rotation, Vector2 size)
    {
        if (previewInstance == null)
        {
            previewInstance = Instantiate(portalPreviewPrefab);
            previewInstance.name = "PortalPreview";
        }

        previewInstance.SetActive(true);

        ApplyTransform(previewInstance, position, rotation, size);
    }

    private void HidePreview()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);
        }
    }

    private void PlacePortal(Vector3 position, Quaternion rotation, Vector2 size, PortalType _portalType)
    {
        if (_portalType == PortalType.Right)
        {
            if (rightPortalInstance != null)
            {
                Destroy(rightPortalInstance);
            }

            rightPortalInstance = Instantiate(rightPortalPrefab);
            rightPortalInstance.name = "RightPlacedPortal";

            ApplyTransform(rightPortalInstance, position, rotation, size);

            rightPortalInstance.GetComponent<PortalTrigger>().SetOtherPortalPosition(leftPortalInstance);

            Debug.Log($"Right Portal placed at {position}");
        } 
        else if (_portalType == PortalType.Left)
        {
            if (leftPortalInstance != null)
            {
                Destroy(leftPortalInstance);
            }

            leftPortalInstance = Instantiate(leftPortalPrefab);
            leftPortalInstance.name = "LeftPlacedPortal";

            ApplyTransform(leftPortalInstance, position, rotation, size);

            leftPortalInstance.GetComponent<PortalTrigger>().SetOtherPortalPosition(rightPortalInstance);

            Debug.Log($"Left Portal placed at {position}");
        }
    }

    private void ApplyTransform(
        GameObject target,
        Vector3 position,
        Quaternion rotation,
        Vector2 size)
    {
        target.transform.SetPositionAndRotation(position, rotation);
        target.transform.localScale = new Vector3(
            size.x, size.y, 1f);
    }
}
