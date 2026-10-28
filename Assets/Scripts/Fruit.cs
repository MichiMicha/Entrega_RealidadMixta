using UnityEngine;

// Pon este script en cada fruta. Necesita un Collider con "Is Trigger" activado.
public class Fruit : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 startPos;
    private bool collected;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.GetComponentInParent<CarController>() == null) return;

        collected = true;
        if (LevelManager.Instance != null) LevelManager.Instance.FruitCollected();
        Destroy(gameObject);
    }
}
