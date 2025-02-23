using UnityEngine;

public static class LayerMaskDefine
{
    public static readonly int PenetrableLayerMask = LayerMask.GetMask("Penetrable", "Penetrated");
    public static readonly int PenetrableLayer = LayerMask.NameToLayer("Penetrable");
    public static readonly int PenetratedLayer = LayerMask.NameToLayer("Penetrated");
}
