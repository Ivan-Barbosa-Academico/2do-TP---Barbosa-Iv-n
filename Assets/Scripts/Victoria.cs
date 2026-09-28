using Unity.VisualScripting;
using UnityEngine;

public class Victoria : MonoBehaviour
{
    [SerializeField] private AudioClip victoriaClip;
    [SerializeField] private float volume = 1f;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !hasPlayed)
        {
            Debug.Log("¡Victoria!");

            if (victoriaClip != null)
            {
                AudioSource.PlayClipAtPoint(victoriaClip, transform.position, volume);
            }
            else
            {
                Debug.LogWarning("Victoria: falta asignar 'victoriaClip' en el Inspector.");
            }

            hasPlayed = true;
        }
    }
}
