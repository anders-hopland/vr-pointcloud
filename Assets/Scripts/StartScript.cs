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
	internal static LasStruct[] lasFiles;
	internal static PointCloudObject display;
	internal static float curscale;

	void Start()
		{
		// Open file with filter
		var extensions = new[] {
		    new ExtensionFilter("Point Cloud", "las"),
		};

		var fileNames = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
		if (fileNames.Length == 0) return;
		fileNames = fileNames.Where(x => x.EndsWith(".las")).ToArray();

		// Sorting on filename, filenames should be on the form filnamefilter<1> ... filenameFilter<N>
		Array.Sort(fileNames);

		lasFiles = new LasStruct[fileNames.Length];
		for (int i = 0; i < fileNames.Length; i++)
			lasFiles[i] = LasReader.readLASFile(fileNames[i]);

		var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
		go.transform.parent = this.transform.parent;
		go.GetComponent<MeshFilter>().mesh = new Mesh();
		go.name = "PointCloud";
		go.AddComponent<PointCloudObject>();
		go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/PointCloudShader"));
		display = go.GetComponent<PointCloudObject>();
		display.setFile(lasFiles);
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
	}
