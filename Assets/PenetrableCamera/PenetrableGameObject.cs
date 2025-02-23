using UnityEngine;

public class PenetrableGameObject : MonoBehaviour
{
    private void Awake()
    {
        gameObject.layer = LayerMaskDefine.PenetrableLayer;
    }

    public void SetPenetrated(bool isPenetrated)
    {
        gameObject.layer = isPenetrated ? LayerMaskDefine.PenetratedLayer : LayerMaskDefine.PenetrableLayer;
    }
}
