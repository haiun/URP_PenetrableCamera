using UnityEngine;

[ExecuteInEditMode]
public class SimpleBillboard : MonoBehaviour
{
    [SerializeField]
    private Transform _target;
    
    private void LateUpdate()
    {
        transform.LookAt(_target);
    }
}
