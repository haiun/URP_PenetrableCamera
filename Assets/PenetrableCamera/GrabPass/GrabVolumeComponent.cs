using System;
using UnityEngine.Rendering;

[Serializable]
public class GrabVolumeComponent : VolumeComponent
{
    public BoolParameter isActive = new(false);
}
