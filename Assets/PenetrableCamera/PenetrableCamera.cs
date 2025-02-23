using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PenetrableCamera : MonoBehaviour
{
    [SerializeField]
    private Transform _target;

    [SerializeField]
    private float _sphareCastRadius;

    [SerializeField]
    private List<PenetrableGameObject> _penetrableGameObjects;
    
    [SerializeField]
    private bool _penetrableCameraActive = true;

    [SerializeField]
    private Volume _volume;
    
    [SerializeField]
    private GameObject _penetrableCameraMaskRoot;
    
    private readonly Rect _toggleButtonRect = new(0, 0, 200, 30);

    private const int HitBufferSize = 32;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];
    private readonly HashSet<PenetrableGameObject> _hitOnFrame = new();
    private void LateUpdate()
    {
        if (!_penetrableCameraActive)
            return;
        
        var camPos = transform.position;
        var sphereCastDirection = (_target.position - camPos);
        sphereCastDirection.Normalize();
        var sphereCastDistance = (_target.position - camPos).magnitude - _sphareCastRadius;
        var hitCount = Physics.SphereCastNonAlloc(camPos, _sphareCastRadius, sphereCastDirection, _hitBuffer, sphereCastDistance,
            LayerMaskDefine.PenetrableLayerMask, QueryTriggerInteraction.Ignore);

        _hitOnFrame.Clear();
        for (var i = 0; i < hitCount; i++)
        {
            var hit = _hitBuffer[i];
            var penetrableGameObject = hit.transform.gameObject.GetComponent<PenetrableGameObject>();
            if (penetrableGameObject == null)
                continue;

            _hitOnFrame.Add(penetrableGameObject);
        }

        foreach (var penetrableGameObject in _penetrableGameObjects)
        {
            penetrableGameObject.SetPenetrated(_hitOnFrame.Contains(penetrableGameObject));
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(_toggleButtonRect, "Toggle Penetrable Camera"))
        {
            _penetrableCameraActive = !_penetrableCameraActive;
            _penetrableCameraMaskRoot.SetActive(_penetrableCameraActive);
            _volume.weight = _penetrableCameraActive ? 1.0f : 0.0f;

            if (!_penetrableCameraActive)
            {
                foreach (var penetrableGameObject in _penetrableGameObjects)
                {
                    penetrableGameObject.SetPenetrated(false);
                }
            }
        }
    }
}