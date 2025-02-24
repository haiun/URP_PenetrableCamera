using UnityEngine;

public static class LayerMaskDefine
{
    // 카메라와 타겟 사이에 있을 가능성이 있는 모든 오브젝트
    public static readonly int PenetrableLayerMask = LayerMask.GetMask("Penetrable", "Penetrated");
    
    // 카메라와 타겟 사이에 배치되어 타겟을 가릴 가능성이 있는 오브젝트
    public static readonly int PenetrableLayer = LayerMask.NameToLayer("Penetrable");
    
    // 카메라와 타겟 사이에 배치되어 타겟을 가린 오브젝트
    public static readonly int PenetratedLayer = LayerMask.NameToLayer("Penetrated");
}
