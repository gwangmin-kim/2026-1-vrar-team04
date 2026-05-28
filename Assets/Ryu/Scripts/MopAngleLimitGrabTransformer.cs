using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

[AddComponentMenu("XR/Transformers/Mop Angle Limit Grab Transformer (VR&AR Team 04)")]
public class MopAngleLimitGrabTransformer : XRGeneralGrabTransformer
{
    [Header("Angle Limits From Grab Start")]
    [SerializeField]
    private bool _limitPitch = true;

    [SerializeField, Range(0f, 180f)]
    private float _maxPitch = 55f;

    [SerializeField]
    private bool _limitYaw;

    [SerializeField, Range(0f, 180f)]
    private float _maxYaw = 180f;

    [SerializeField]
    private bool _limitRoll = true;

    [SerializeField, Range(0f, 180f)]
    private float _maxRoll = 30f;

    [Header("Responsiveness")]
    [SerializeField, Min(0f)]
    private float _maxRotationDegreesPerSecond = 540f;

    private Quaternion _grabStartRotation;

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        _grabStartRotation = grabInteractable.transform.rotation;
    }

    public override void Process(
        XRGrabInteractable grabInteractable,
        XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose,
        ref Vector3 localScale)
    {
        base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);

        Quaternion targetDelta = Quaternion.Inverse(_grabStartRotation) * targetPose.rotation;
        Vector3 targetEuler = targetDelta.eulerAngles;

        if (_limitPitch)
            targetEuler.x = ClampSignedAngle(targetEuler.x, _maxPitch);

        if (_limitYaw)
            targetEuler.y = ClampSignedAngle(targetEuler.y, _maxYaw);

        if (_limitRoll)
            targetEuler.z = ClampSignedAngle(targetEuler.z, _maxRoll);

        Quaternion limitedRotation = _grabStartRotation * Quaternion.Euler(targetEuler);

        if (_maxRotationDegreesPerSecond > 0f)
        {
            float maxStep = _maxRotationDegreesPerSecond * Time.deltaTime;
            limitedRotation = Quaternion.RotateTowards(grabInteractable.transform.rotation, limitedRotation, maxStep);
        }

        targetPose.rotation = limitedRotation;
    }

    private static float ClampSignedAngle(float angle, float maxAbsAngle)
    {
        float signedAngle = Mathf.DeltaAngle(0f, angle);
        return Mathf.Clamp(signedAngle, -maxAbsAngle, maxAbsAngle);
    }
}
