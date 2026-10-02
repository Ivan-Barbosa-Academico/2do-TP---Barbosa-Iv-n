using UnityEngine;

public class Plataformas : MonoBehaviour
{
    public GameObject[] nodos;

    public float platformSpeed = 2f;

    int waypointIndex = 0;

    void Update()
    {
        MovePlatform();
    }

    void MovePlatform()
    {
        if(Vector3.Distance(transform.position, nodos[waypointIndex].transform.position) < 0.1f)
        {
            waypointIndex++;
            if(waypointIndex >= nodos.Length)
            {
                waypointIndex = 0;
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, nodos[waypointIndex].transform.position, platformSpeed * Time.deltaTime);

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.SetParent(transform);
        }
        Debug.Log("Collision detected with: " + collision.gameObject.name);

    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.SetParent(null);
        }
    }
}
