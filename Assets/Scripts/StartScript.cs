using DataStructures.ViliWonka.KDTree;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System;
using SFB;

public class StartScript : MonoBehaviour
	{
	internal static int curLasFileIndex = 0;
	internal static PointCloudManager display;
	internal static UIManager ui;
	internal static GameObject sceneRoot;
	internal static GameObject sceneFloor;
	internal static GameObject imageCyl;
	internal static float sceneRootScale = 1f;
	internal static bool paused = false;
	internal static bool _displayNormals = false;
    internal static bool displayNormals
        {
        get
            {
            return _displayNormals;
            }
        set
            {
            _displayNormals = value;
            if (display != null)
                display.setMatDisplayNormals (value);
            }
        }

    internal static bool _displayRoundPoints = false;
	internal static bool displayRoundPoints
		{
		get
			{
			return _displayRoundPoints;
			}
		set
			{
			_displayRoundPoints = value;
			if (display != null)
				display.setMatDisplayRoundPoints(value);
			}
		}

	void Start()
		{
		ui = new UIManager();
		sceneRoot = GameObject.Find("SceneElemRoot");
		sceneFloor = GameObject.Find("SceneFloor");
		imageCyl = GameObject.Find("ImageCylinder");
		ComputeBufferManager.init();
		ComputeBufferManager.uploadLabelColors(UIManager.labelColors);
		}

	void Update()
		{
		// Hide / show desktop menu
		if (Input.GetKeyDown("m"))
			ui.desktopMenuRoot.SetActive(!ui.desktopMenuRoot.activeSelf);
		}

	private void OnDestroy()
		{
		ComputeBufferManager.dispose();
		}
	}
