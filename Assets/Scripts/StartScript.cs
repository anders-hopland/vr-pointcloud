﻿using DataStructures.ViliWonka.KDTree;
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
	internal static PointCloudObject display;
	internal static float curscale;
	internal static UIManager ui;

	void Start()
		{
		ui = new UIManager();
		}

	// Update is called once per frame
	void Update()
		{
		if (Input.GetKeyDown("up"))
			display.nextPointCloud();
		if (Input.GetKeyDown("down"))
			display.prevPointCloud();
		}

	private void OnDisable()
		{
		
		}

	internal static void setActiveDisplayObject(PointCloudObject pointCloud)
		{
		display = pointCloud;
		}
	}
