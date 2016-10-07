using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// This script handles transitions from the "hot zones" around the scope object
/// The process is as follows:
///     1) Detection of player camera rig in hot zone
///     2) Indication that a transition is about to occur
///     3) Fade to black (no scene change, but will reduce motion sickness)
///     4) Activate LockedScopePerspective object - set scope camera target to rendertexture
///     5) Enable camera stabilization on lens object(?) - disable positional lens adjustments
///     6) Fade in, resume control
/// This should be added as a component on a gameobject with a hot zone collider
/// </summary>
public class ScopeTransition : MonoBehaviour {

    public float TransitionWaitTime = 2.0f;

    public GameObject ScopeObj, HeadCamRig;

    private bool UITransitionFlag;
    public bool ViewSnapped { private set; get; }

    public void OnTriggerEnter(Collider other) {
        //If no transition is occuring and the triggered collider is a hot zone, start appropriate transition
        if (!UITransitionFlag && 
            !ViewSnapped &&
            (other.gameObject == HeadCamRig)) {
            UITransitionFlag = true;
            StartCoroutine(UITransitionIn(other));
        }
    }

    private IEnumerator UITransitionIn(Collider other) {
        System.DateTime tPointOfNoReturn = System.DateTime.Now.AddSeconds(TransitionWaitTime);
        while (System.DateTime.Now < tPointOfNoReturn) {
            //Ensure colliders are still colliding - if not, cancel transition
            if (!other.bounds.Intersects(GetComponent<Collider>().bounds)) {
                BreakTransition();
            }
            yield return new WaitForEndOfFrame();
        }
        SnapViewToScope();

        UITransitionFlag = false;
        yield return null;
    }

    private IEnumerator UITransitionOut(Collider other) {
        yield return null;
    }

    private void BreakTransition() {
        Debug.Log("Transition canceled.");
        UITransitionFlag = false;
        StopAllCoroutines();
    }

    private void SnapViewToScope() {
        Debug.Log("Snapping to scope view.");
        ViewSnapped = true;
        try {
            GameObject lockedPerspectiveObj = HeadCamRig.transform.FindChild("LockedScopePerspective").gameObject;
            Camera scopeCam = GameObject.FindWithTag("ScopeCam").GetComponent<Camera>();

            //Set scope camera to render to locked perspective texture
            scopeCam.targetTexture = lockedPerspectiveObj.GetComponent<MeshRenderer>().material.mainTexture as RenderTexture;

            //Set head cam to render only micro layer (incl. locked perspective object)
            Camera headCam = HeadCamRig.GetComponent<Camera>();
            DisableCullingMaskLayer("Macro", headCam);
            EnableCullingMaskLayer("Micro", headCam);

        } catch (NullReferenceException) {
            Debug.Log("Locked scope perspective target or scope camera was not found.");
        }
        //Deactivate player camera rig motion

    }

    private void UnsnapViewFromScope() {
        Debug.Log("Unsnapping from scope view.");
        ViewSnapped = false;
    }

    /// <summary>
    /// Directly alter a camera's culling mask over a single layer using a bitwise op
    /// </summary>
    /// <param name="layerName"></param>
    private void EnableCullingMaskLayer(string layerName, Camera cam) {
        cam.cullingMask |= 1 << LayerMask.NameToLayer(layerName);
    }

    /// <summary>
    /// Directly alter a camera's culling mask over a single layer using a bitwise op
    /// </summary>
    /// <param name="layerName"></param>
    private void DisableCullingMaskLayer(string layerName, Camera cam) {
        cam.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
    }

}
