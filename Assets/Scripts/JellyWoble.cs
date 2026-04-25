using UnityEngine;

public class JellyWobble : MonoBehaviour
{
    public float followSpeed = 12f;
    public float wobbleAmount = 0.2f;
    public float maxWobble = 0.25f;

    private Vector3 originalLocalPosition;
    private Vector3 velocity;

    private Transform parent;
    private Vector3 lastParentPos;

    void Start()
    {
        parent = transform.parent;
        originalLocalPosition = transform.localPosition;
        lastParentPos = parent.position;
    }

    void LateUpdate()
    {
        if (parent == null) return;

        // 📌 հաշվում ենք parent-ի շարժման արագությունը
        Vector3 parentDelta = parent.position - lastParentPos;
        float speed = parentDelta.magnitude / Time.deltaTime;

        lastParentPos = parent.position;

        // 📌 wobble ուժը կախված է արագությունից
        float wobbleStrength = Mathf.Clamp(speed * wobbleAmount, 0, maxWobble);

        // 📌 ուղղությունը հակառակ շարժման
        Vector3 offset = -parentDelta.normalized * wobbleStrength;

        Vector3 target = originalLocalPosition + offset;

        // Smooth follow
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            target,
            ref velocity,
            1f / followSpeed
        );
    }
}