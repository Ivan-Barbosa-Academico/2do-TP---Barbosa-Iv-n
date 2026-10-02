using System.Collections;
using UnityEngine;

public class InvPotion : MonoBehaviour
{
    [SerializeField] private float effectDuration = 10f;
    [SerializeField] private AudioClip collectSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    public static System.Action<GameObject, bool> OnInvisibilityStateChanged;

    private void OnTriggerEnter(Collider other)
    {
        TryApplyToPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyToPlayer(collision.gameObject);
    }

    private void TryApplyToPlayer(GameObject target)
    {
        if (!target.CompareTag("Player")) return;
        InvisibilityEffect effect = target.GetComponent<InvisibilityEffect>();
        if (effect == null) effect = target.AddComponent<InvisibilityEffect>();

        effect.Activate(effectDuration);

        if (collectSfx != null)
        {
            AudioSource.PlayClipAtPoint(collectSfx, transform.position, sfxVolume);
        }
        Destroy(gameObject);
    }

    private class InvisibilityEffect : MonoBehaviour
    {
        private int originalLayer;

        private Coroutine countdownCoroutine;

        public void Activate(float duration)
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = StartCoroutine(CountdownCoroutine(duration));
                return;
            }

            CacheAndSetLayerToIgnoreRaycast();
            OnInvisibilityStateChanged?.Invoke(gameObject, true);

            countdownCoroutine = StartCoroutine(CountdownCoroutine(duration));
        }

        private void CacheAndSetLayerToIgnoreRaycast()
        {
            originalLayer = gameObject.layer;
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0)
            {
                SetLayerRecursively(gameObject, ignoreLayer);
            }
        }

        private void RestoreLayer()
        {
            SetLayerRecursively(gameObject, originalLayer);
        }

        private void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform)
            {
                SetLayerRecursively(t.gameObject, layer);
            }
        }

        private IEnumerator CountdownCoroutine(float duration)
        {
            float remaining = duration;

            while (remaining > 0f)
            {
                Debug.Log($"Invisibilidad: {remaining:F1} s restantes");
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            Debug.Log("Invisibilidad: 0.0 s restantes — efecto finalizado");
            RestoreLayer();
            OnInvisibilityStateChanged?.Invoke(gameObject, false);
            countdownCoroutine = null;
            Destroy(this);
        }
    }
}
