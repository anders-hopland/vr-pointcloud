using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VR_Actions : MonoBehaviour
	{
	internal SteamVR_Input_Sources leftHand;
	internal SteamVR_Input_Sources rightHand;
	internal GameObject leftHandGo;
	internal GameObject rightHandGo;
	internal GameObject sceneRoot;
	internal GameObject editSphere;
	void Start()
		{
		leftHand = SteamVR_Input_Sources.LeftHand;
		rightHand = SteamVR_Input_Sources.RightHand;
		leftHandGo = GameObject.Find("Controller (left)");
		rightHandGo = GameObject.Find("Controller (right)");
		sceneRoot = GameObject.Find("SceneElemRoot");
		editSphere = GameObject.Find("EditSphere");
		editSphereRad = editSphere.transform.localScale.x / 2f;
		}

	internal float editSphereRad = 1f;
	void Update()
		{
		updateShaderParams();
		checkDragMove();
		checkResize();
		}

	internal bool grabbingRight = false;
	internal bool grabbingLeft = false;
	internal float scaleDist;
	internal void checkResize()
		{
		Vector2 axis;
		if (!grabbingLeft && !grabbingRight)
			{
			axis = SteamVR_Input.GetVector2("joystick", leftHand);
			if (axis.sqrMagnitude > 0)
				{
				sceneRoot.transform.localScale += Vector3.one * axis.x * 0.01f;
				}
			}

		axis = SteamVR_Input.GetVector2("joystick", rightHand);
		if (axis.sqrMagnitude > 0)
			{
			editSphereRad += axis.x * 0.0015f;
			if (editSphereRad < 0.015f) editSphereRad = 0.015f;
			else if (editSphereRad > 0.15f) editSphereRad = 0.15f;
			editSphere.transform.localScale = Vector3.one * editSphereRad * 2; // Diameter is radius * 2
			}

		var grabPinch = SteamVR_Input.GetState("GrabPinch", leftHand);
		}
	internal Vector3 lastGrabPos;
	internal Quaternion lastGrabRot;
	internal void checkDragMove()
		{
		bool grabRight = SteamVR_Input.GetState("GrabGrip", rightHand);
		bool grabLeft = SteamVR_Input.GetState("GrabGrip", leftHand);

		if (grabLeft != grabRight)
			{
			if (grabLeft && !grabbingLeft) // Start grab
				{
				sceneRoot.transform.parent = leftHandGo.transform;
				}
			if (grabRight && !grabbingRight) // Start grab
				{
				sceneRoot.transform.parent = rightHandGo.transform;
				}
			}

		if ((grabbingLeft && !grabLeft) || (grabbingRight && !grabRight))
			sceneRoot.transform.parent = null;

		grabbingLeft = grabLeft;
		grabbingRight = grabRight;
		}

	internal void updateShaderParams()
		{
		if (StartScript.selectedObj == null) return;
		if (StartScript.selectedObjMat == null) return;
		StartScript.selectedObjMat.SetFloat("_EditRadius", editSphereRad * (1 / sceneRoot.transform.localScale.x));
		var newEditPos = StartScript.selectedObj.transform.InverseTransformPoint(editSphere.transform.position);
		StartScript.selectedObjMat.SetVector("_EditPos", newEditPos);
		StartScript.selectedObjMat.SetInt ("_TriggerPress", SteamVR_Input.GetState("GrabPinch", rightHand) ? 1 : 0);
		}
	}

