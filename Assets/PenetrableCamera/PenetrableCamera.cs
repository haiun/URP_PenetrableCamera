using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PenetrableCamera : MonoBehaviour
{
    [SerializeField]
    private Transform _target;
    
    [SerializeField]
    private Transform _mask;

    [SerializeField]
    private float _sphareCastRadius;

    [SerializeField]
    private List<PenetrableGameObject> _penetrableGameObjects;
    
    [SerializeField]
    private Volume _volume;
    
    [SerializeField]
    private GameObject _penetrableCameraMaskRoot;
    
    [SerializeField]
    private Material _penetrableCameraMaskMaterial;
    
    private bool _penetrableCameraActive = true;
    private float _maskAlphaSliderValue = 0.5f;
    private float _maskScaleSliderValue = 4f;
    private readonly Rect _stateTextAreaRect = new(10, 0, 200, 40);
    private readonly Rect _toggleButtonRect = new(10, 40, 200, 30);
    private readonly Rect _alphaSliderTextRect = new(10, 70, 45, 25);
    private readonly Rect _scaleSliderTextRect = new(10, 100, 45, 25);
    private readonly Rect _alphaSliderRect = new(60, 75, 140, 30);
    private readonly Rect _scaleSliderRect = new(60, 105, 140, 30);
    private readonly int _materialPropertyAlphaId = Shader.PropertyToID("_Alpha");
    
    private const int HitBufferSize = 32;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[HitBufferSize];
    private readonly HashSet<PenetrableGameObject> _hitOnFrame = new();
    private void LateUpdate()
    {
        if (!_penetrableCameraActive)
            return;

        FilteringNearbyObject();
    }

    private void FilteringNearbyObject()
    {
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
        if (GUI.Button(_toggleButtonRect, _penetrableCameraActive ? "Penetrable Camera On" : "Penetrable Camera Off"))
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

        GUI.TextArea(_alphaSliderTextRect, "Alpha");
        _maskAlphaSliderValue = GUI.HorizontalSlider(_alphaSliderRect, _maskAlphaSliderValue, 0.0f, 1.0f);
        _penetrableCameraMaskMaterial.SetFloat(_materialPropertyAlphaId, _maskAlphaSliderValue);
        
        GUI.TextArea(_scaleSliderTextRect, "Scale");
        _maskScaleSliderValue = GUI.HorizontalSlider(_scaleSliderRect, _maskScaleSliderValue, 0.0f, 4.0f);
        _mask.localScale = new Vector3(_maskScaleSliderValue, _maskScaleSliderValue, _maskScaleSliderValue);
        
        GUI.TextField(_stateTextAreaRect, $"Mask Alpha: {_maskAlphaSliderValue:0.00}\nMask Scale: {_maskScaleSliderValue:0.00}");
    }
}