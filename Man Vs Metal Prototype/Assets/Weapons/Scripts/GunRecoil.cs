using UnityEngine;

public class GunRecoil
{
    private Vector3 _currentRotation;
    private Vector3 _targetRotation;

    public float RecoilX { get; set; }
    public float RecoilY { get; set; }
    public float RecoilZ { get; set; }
    public float Snappiness { get; set; }
    public float ReturnSpeed { get; set; }

    public GunRecoil(float recoilX, float recoilY, float recoilZ, float snappiness, float returnSpeed)
    {
        RecoilX = recoilX;
        RecoilY = recoilY;
        RecoilZ = recoilZ;
        Snappiness = snappiness;
        ReturnSpeed = returnSpeed;
    }

    public bool CalculateRecoil(PlayerAim _playerAimScript)
    {
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, ReturnSpeed / 25f);
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, Snappiness * Time.deltaTime);
        _playerAimScript.RecoilRotation = Quaternion.Euler(_currentRotation);
        //gameObjectTransform.localRotation = Quaternion.Euler(_currentRotation);

        if (_currentRotation != _targetRotation)
            return true;
        else
            return false;
    }
    public void InitiateRecoil() => _targetRotation += new Vector3(RecoilX, UnityEngine.Random.Range(-RecoilY, RecoilY), UnityEngine.Random.Range(-RecoilZ, RecoilZ));
}
