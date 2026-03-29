using UnityEngine;

namespace ProjectZ.Sphere
{
    /// <summary>
    /// Defines a valid bomb plant site (A, B, or C).
    /// Requires a Trigger Collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SphereSite : MonoBehaviour
    {
        [Tooltip("The ID of this site (e.g. 'A', 'B', 'C')")]
        public string SiteID;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var interaction = other.GetComponentInParent<ProjectZ.Player.SphereInteraction>();
            if (interaction != null)
                interaction.EnterSite(this);
        }

        private void OnTriggerExit(Collider other)
        {
            var interaction = other.GetComponentInParent<ProjectZ.Player.SphereInteraction>();
            if (interaction != null)
                interaction.ExitSite(this);
        }
    }
}
