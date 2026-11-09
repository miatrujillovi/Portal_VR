
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using UnityEngine;

public class PortalPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RayInteractor _rightRayInteractor;
    [SerializeField] private GameObject _portalPrefab;

    [Header("Portal Settings")]
    [SerializeField] private Vector2 _wallSize = new(0.9f, 1.4f);
    [SerializeField] private Vector2 _floorSize = new(0.9f, 0.9f);
    [SerializeField] private float _maxDistance = 8f;
    [SerializeField] private float _surfaceOffset = 0.02f;
    [SerializeField] private float _edgeMargin = 0.03f;

    private GameObject _previewInstance;
    private GameObject _portalInstance;

    private void Update()
    {
        if (_rightRayInteractor == null || _portalPrefab == null)
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

        Ray ray = _rightRayInteractor.Ray;

        if (!TryFindSurface(room, ray, out MRUKAnchor anchor,
                out RaycastHit hit, out bool isWall))
        {
            HidePreview();
            return;
        }

        Vector2 portalSize = isWall ? _wallSize : _floorSize;

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

        Vector3 position = hit.point + normal * _surfaceOffset;

        UpdatePreview(position, rotation, portalSize);

        if (OVRInput.GetDown(
                OVRInput.Button.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch))
        {
            PlacePortal(position, rotation, portalSize);
        }
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

        float closestDistance = _maxDistance;

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

        float halfWidth = size.x * 0.5f + _edgeMargin;
        float halfHeight = size.y * 0.5f + _edgeMargin;

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
        if (_previewInstance == null)
        {
            _previewInstance = Instantiate(_portalPrefab);
            _previewInstance.name = "PortalPreview";
        }

        _previewInstance.SetActive(true);

        ApplyTransform(_previewInstance, position, rotation, size);
    }

    private void HidePreview()
    {
        if (_previewInstance != null)
        {
            _previewInstance.SetActive(false);
        }
    }

    private void PlacePortal(
        Vector3 position, Quaternion rotation, Vector2 size)
    {
        if (_portalInstance != null)
        {
            Destroy(_portalInstance);
        }

        _portalInstance = Instantiate(_portalPrefab);
        _portalInstance.name = "PlacedPortal";

        ApplyTransform(_portalInstance, position, rotation, size);

        Debug.Log($"Portal placed at {position}");
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
