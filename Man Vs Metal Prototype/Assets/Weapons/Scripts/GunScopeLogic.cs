using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunScopeLogic
{
    private GunSO _gunData;
    private Camera _scopeCam;
    private Camera _gunCam;
    private GameObject _scopeOverlay;
    private Transform _gunAnchorPoint;
    private float _normalFov;
    private int _oldMask;

    public bool IsScopedIn { get; set; }

    public GunScopeLogic(GunSO gunData, Camera scopeCam, Camera gunCam, GameObject scopeOverlay, Transform gunAnchorPoint)
    {
        _gunData = gunData;
        _scopeCam = scopeCam;
        _gunCam = gunCam;
        _scopeOverlay = scopeOverlay;
        _gunAnchorPoint = gunAnchorPoint;
    }
    public IEnumerator ExecuteScopeEffect(bool scopingIn, Vector3 endValue, float duration)
    {
        var startValue = _gunAnchorPoint.localPosition;

        var time = 0f;

        while (time < duration)
        {
            _gunAnchorPoint.localPosition = Vector3.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        _gunAnchorPoint.localPosition = endValue;

        if (scopingIn)
        {
            if (_gunData != null && _gunData.ScopeOverlay)
                ZoomInAndApplyScope();
        }
    }
    public void ZoomOutAndRemoveScope()
    {
        _scopeCam.fieldOfView = _normalFov;
        _scopeOverlay.SetActive(false);
        _gunCam.cullingMask = _oldMask;
        _gunCam.gameObject.SetActive(true);
    }
    private void ZoomInAndApplyScope()
    {
        _normalFov = _scopeCam.fieldOfView;
        _scopeCam.fieldOfView = _gunData.ScopedFov;
        _scopeOverlay.SetActive(true);
        _oldMask = _gunCam.cullingMask;
        _gunCam.cullingMask = 1 << LayerMask.NameToLayer("Nothing");
    }
}
