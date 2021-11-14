using DataStructures.ViliWonka.KDTree;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System;

public class StartScript : MonoBehaviour
	{

	internal static GameObject selectedObj;
	internal static Material selectedObjMat;
	internal static int curLasFileIndex = 0;
	internal static LasFile[] lasFiles;

	internal static ComputeBuffer vertBuffer;
	internal static ComputeBuffer normBuffer;
	internal static ComputeBuffer colBuffer;
	void Start()
		{
		//var ret = LasReader.readLASFile("C:\\Users\\Anders\\Documents\\36 mill gloes.las");

		string fileNameFilter = "test_pointCloud";
		var fileNames = Directory.GetFiles("C:\\Users\\Anders\\Downloads\\LidarPointClouds")
			.Where(x => x.Contains(fileNameFilter) && x.EndsWith(".las")).ToArray();

		// Sorting on filename, filenames should be on the form filnamefilter<1> ... filenameFilter<N>
		Array.Sort(fileNames);

		lasFiles = new LasFile[fileNames.Length];
		for (int i = 0; i < fileNames.Length; i++)
			lasFiles[i] = LasReader.readLASFile(fileNames[i]);

		//var ret = LasReader.readLASFile("C:\\Users\\Anders\\Documents\\1400 k gloes - Cloud.las");
		//var ret = LasReader.readLASFile("C:\\Users\\Anders\\Documents\\trd15K.las");

		//var normals = getNormals(vertices);

		vertBuffer = new ComputeBuffer(2 * 1000 * 1000, 4 * 3); // 4 bytes per float, 3 floats		
		Graphics.SetRandomWriteTarget(1, vertBuffer);

		//normBuffer = new ComputeBuffer(normals.Length, 4 * 3);
		//normBuffer.SetData(normals);
		//Graphics.SetRandomWriteTarget(2, normBuffer);

		// Big initial size, as point clouds in sequence can be of variable length
		colBuffer = new ComputeBuffer(2 * 1000 * 1000, 4 * 4);
		Graphics.SetRandomWriteTarget(3, colBuffer);

		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane); // Uses a primitive to get meshfilter and meshrenderer
		selectedObj = go;
		selectedObjMat = Resources.Load("Materials/PointCloudMat") as Material;
		go.GetComponent<MeshRenderer>().material = selectedObjMat;
		go.transform.parent = this.transform.parent;
		setPointCloud(lasFiles[curLasFileIndex]);
		}

	// Update is called once per frame
	void Update()
		{
		if (Input.GetKeyDown("up"))
			nextPointCloud();
		if (Input.GetKeyDown("down"))
			prevPointCloud();
		}

	private void OnDisable()
		{
		if (vertBuffer != null)
			vertBuffer.Release();
		if (normBuffer != null)
			normBuffer.Release();
		if (colBuffer != null)
			colBuffer.Release();
		}

	private Vector3[] getNormals(Vector3[] points)
		{
		KDTree tree = new KDTree(32);
		tree.Build(points);

		var normals = new Vector3[points.Length];
		KDQuery query = new KDQuery();
		List<int> nearestIndices = new List<int>(2);
		for (int i = 0; i < points.Length; i++)
			{
			query.KNearest(tree, points[i], 2, nearestIndices);
			var v1 = points[i];
			var v2 = points[nearestIndices[0]];
			var v3 = points[nearestIndices[1]];
			var norm = Vector3.Cross(v2 - v1, v3 - v1);
			normals[i] = norm;
			}
		return normals;
		}

	public static void setPointCloud(LasFile file)
		{
		var vertices = new Vector3[file.points.Length];
		var colors = new Color[file.points.Length];
		var indices = new int[file.points.Length];
		if (file.points.Length > 0)
			{
			for (int i = 0; i < vertices.Length; i++)
				{
				vertices[i] = file.points[i].xyz * 0.1f;
				colors[i] = file.points[i].col;
				indices[i] = i;
				}
			}

		selectedObj.GetComponent<MeshFilter>().mesh.SetVertices(vertices);
		selectedObj.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);

		vertBuffer.SetData(vertices);
		colBuffer.SetData(colors);
		}

	public static void setCurLayer(Color col)
		{
		selectedObjMat.SetColor("_EditCol", col);
		}

	public static void nextPointCloud()
		{
		downloadCurPointCloud();
		if (curLasFileIndex >= lasFiles.Length - 1) return;
		curLasFileIndex++;
		setPointCloud(lasFiles[curLasFileIndex]);
		}

	public static void prevPointCloud ()
		{
		downloadCurPointCloud();
		if (curLasFileIndex <= 0) return;
		curLasFileIndex--;
		setPointCloud(lasFiles[curLasFileIndex]);
		}

	private static void downloadCurPointCloud ()
		{
		// Download data like colors
		}
	}
