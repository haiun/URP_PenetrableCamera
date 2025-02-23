using UnityEngine;

public class SimpleRotater : MonoBehaviour
{
    [SerializeField]
    private Vector3 _rotateAxis = Vector3.up;
    
    [SerializeField]
    private float _rotateSpeed = 180f;

    // Update is called once per frame
    private void LateUpdate()
    {
        transform.Rotate(_rotateAxis, Time.deltaTime * _rotateSpeed);
    }
}
