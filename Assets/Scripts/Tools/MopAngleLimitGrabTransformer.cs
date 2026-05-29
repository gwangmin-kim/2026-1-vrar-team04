using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

[AddComponentMenu("XR/Transformers/Mop Angle Limit Grab Transformer (VR&AR Team 04)")]
public class MopAngleLimitGrabTransformer : XRGeneralGrabTransformer
{
    [Header("Grab Point Gating")]
    [SerializeField]
    private bool _limitOnlyWhenLowerGripIsHeld = true;

    [SerializeField]
    private Transform _upperGrip;

    [SerializeField]
    private Transform _lowerGrip;

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
    private bool _wasLowerGripHeld;

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        _grabStartRotation = grabInteractable.transform.rotation;
        _wasLowerGripHeld = false;
    }

    public override void Process(
        XRGrabInteractable grabInteractable,
        XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose,
        ref Vector3 localScale)
    {
        base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);

        bool lowerGripHeld = !_limitOnlyWhenLowerGripIsHeld || IsLowerGripHeld(grabInteractable);
        if (!lowerGripHeld)
        {
            _wasLowerGripHeld = false;
            return;
        }

        if (!_wasLowerGripHeld)
        {
            _grabStartRotation = grabInteractable.transform.rotation;
            _wasLowerGripHeld = true;
        }

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

    private bool IsLowerGripHeld(XRGrabInteractable grabInteractable)
    {
        Transform upperGrip = _upperGrip != null ? _upperGrip : grabInteractable.attachTransform;
        Transform lowerGrip = _lowerGrip != null ? _lowerGrip : grabInteractable.secondaryAttachTransform;

        if (lowerGrip == null)
            return true;

        if (upperGrip == null)
            return true;

        foreach (var interactor in grabInteractable.interactorsSelecting)
        {
            if (interactor == null)
                continue;

            Transform interactorAttach = interactor.GetAttachTransform(grabInteractable);
            Vector3 interactorPosition = interactorAttach != null
                ? interactorAttach.position
                : interactor.transform.position;

            float upperDistance = Vector3.SqrMagnitude(interactorPosition - upperGrip.position);
            float lowerDistance = Vector3.SqrMagnitude(interactorPosition - lowerGrip.position);

            if (lowerDistance <= upperDistance)
                return true;
        }

        return false;
    }

    private static float ClampSignedAngle(float angle, float maxAbsAngle)
    {
        float signedAngle = Mathf.DeltaAngle(0f, angle);
        return Mathf.Clamp(signedAngle, -maxAbsAngle, maxAbsAngle);
    }
}
