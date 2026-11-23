using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    [Header("Portal Settings")]
    [SerializeField] private float exitForce = 5f;

    private GameObject otherPortal;

    public void SetOtherPortalPosition(GameObject _otherPortal)
    {
        if (_otherPortal == null)
            return;

        otherPortal = _otherPortal;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Object"))
            return;

        if (otherPortal == null)
            return;

        // Find the object that owns the collider.
        /*PickUpObject pickupObject = other.GetComponentInParent<PickUpObject>();

        if (pickupObject == null)
            return;

        // Release the object from the controller.
        pickupObject.RelinquishControlOverObject();

        // Teleport the complete object.
        SendObjectToOtherPortal(pickupObject.gameObject);*/
    }

    private void SendObjectToOtherPortal(GameObject _object)
    {
        // Get the destination portal's transform.
        Transform destination = otherPortal.transform;

        // Get the object's Rigidbody.
        Rigidbody objectRB = _object.GetComponent<Rigidbody>();

        // Teleport the object.
        _object.transform.position = destination.position;

        // Reset previous velocity.
        if (objectRB != null)
        {
            objectRB.linearVelocity = Vector3.zero;
            objectRB.angularVelocity = Vector3.zero;

            // Apply an impulse in the direction the portal faces.
            objectRB.AddForce(destination.forward * exitForce, ForceMode.Impulse);
        }
    }
}
