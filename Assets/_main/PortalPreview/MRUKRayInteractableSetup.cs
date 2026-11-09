
using System.Collections;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;

public class MRUKRayInteractableSetup : MonoBehaviour
{
    [SerializeField] private EffectMesh _effectMesh;

    private IEnumerator Start()
    {
        if (_effectMesh == null)
        {
            Debug.LogError("EffectMesh reference is missing.");
            yield break;
        }

        // Wait until the room meshes are generated.
        while (_effectMesh.EffectMeshObjects.Count == 0)
        {
            yield return null;
        }

        // Wait one frame for initialization.
        yield return null;

        SetupSurfaces();
    }

    private void SetupSurfaces()
    {
        int count = 0;

        foreach (var pair in _effectMesh.EffectMeshObjects)
        {
            MRUKAnchor anchor = pair.Key;

            bool isWall = anchor.HasAnyLabel(
                MRUKAnchor.SceneLabels.WALL_FACE);

            bool isFloor = anchor.HasAnyLabel(
                MRUKAnchor.SceneLabels.FLOOR);

            if (!isWall && !isFloor)
            {
                continue;
            }

            Collider collider = pair.Value.collider;

            if (collider == null)
            {
                Debug.LogWarning(
                    $"Missing collider: {anchor.name}");
                continue;
            }

            GameObject surfaceObject =
                pair.Value.effectMeshGO;

            if (surfaceObject.transform.Find(
                    "RayInteractionTarget") != null)
            {
                continue;
            }

            GameObject target = new GameObject(
                "RayInteractionTarget");

            target.transform.SetParent(
                surfaceObject.transform, false);

            // Configure dependencies before enabling.
            target.SetActive(false);

            ColliderSurface colliderSurface =
                target.AddComponent<ColliderSurface>();

            colliderSurface.InjectAllColliderSurface(
                collider);

            RayInteractable interactable =
                target.AddComponent<RayInteractable>();

            interactable.InjectAllRayInteractable(
                colliderSurface);

            target.SetActive(true);

            count++;
        }

        Debug.Log(
            $"Configured {count} MRUK ray interactables.");
    }
}
