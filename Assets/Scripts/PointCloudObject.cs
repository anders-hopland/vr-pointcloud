using DataStructures.ViliWonka.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class PointCloudObject : MonoBehaviour
	{
	internal ComputeBuffer vertBuffer;
	internal ComputeBuffer normBuffer;
	internal ComputeBuffer colBuffer;
	internal MeshFilter mf;
	internal MeshRenderer mr;
	internal LasStruct[] files;
	internal int curtFileIx;
	internal bool displayNormals;
	internal bool displayRoundPoints;
	internal float displayRadius = 0.25f;
	internal bool initialized;


	void Start()
		{
		init();
		}

	void Update()
		{

		}

	public static PointCloudObject newPointCloudObject(LasStruct[] lasFiles)
		{
		var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
		go.GetComponent<MeshFilter>().mesh = new Mesh(); // Clear mesh
		go.name = "PointCloud";
		go.AddComponent<PointCloudObject>();
		go.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Custom/PointCloudShader"));
		go.transform.parent = StartScript.sceneRoot.transform;
		var display = go.GetComponent<PointCloudObject>();
		display.displayNormals = StartScript.displayNormals;
		display.setFile(lasFiles);

		StartScript.sceneFloor.transform.position = new Vector3(
			(float)(lasFiles[0].header.maxX + lasFiles[0].header.minX) / 2f,
			(float)(lasFiles[0].header.maxY + lasFiles[0].header.minY) / 2f,
			(float)lasFiles[0].header.minZ
			);

		return display;
		}

	internal void init()
		{
		if (initialized) return;
		// Initialize compute buffers
		// Big initial size, as point clouds in sequence can be of variable length
		vertBuffer = new ComputeBuffer(2 * 1000 * 1000, 4 * 3); // 4 bytes per float, 3 floats		
		Graphics.SetRandomWriteTarget(1, vertBuffer);

		normBuffer = new ComputeBuffer(2 * 1000 * 1000, 4 * 3);
		Graphics.SetRandomWriteTarget(2, normBuffer);

		colBuffer = new ComputeBuffer(2 * 1000 * 1000, 4 * 4);
		Graphics.SetRandomWriteTarget(3, colBuffer);

		mf = this.GetComponent<MeshFilter>();
		mr = this.GetComponent<MeshRenderer>();

		initialized = true;
		}

	public void setFile(LasStruct[] files)
		{
		if (files.Length == 0)
			{
			throw new System.Exception("setfile length is 0");
			}

		this.files = files;
		setPointCloud(this.files[0]);
		}

	// Work arrays for pushing data to GPU
	internal static Vector3[] workvertices = new Vector3[250000];
	internal static Color[] workcolors = new Color[250000];
	internal void setPointCloud(LasStruct file)
		{
		init();

		if (displayNormals)
			calcNormals(file);

		int[] indices = new int[file.points.Length];

		if (workvertices.Length < file.points.Length)
			{
			workvertices = new Vector3[(int)(file.points.Length * 1.25f)];
			workcolors = new Color[(int)(file.points.Length * 1.25f)];
			}

		setMatDefaults();

		for (int i = 0; i < file.points.Length; i++)
			{
			workvertices[i] = file.points[i].xyz;
			workcolors[i] = file.points[i].col;
			indices[i] = i;
			}

		mf.mesh.SetVertices(workvertices, 0, file.points.Length);
		mf.mesh.SetIndices(indices, 0, file.points.Length, MeshTopology.Points, 0);

		vertBuffer.SetData(workvertices, 0, 0, file.points.Length);
		colBuffer.SetData(workcolors, 0, 0, file.points.Length);

		ThreadPool.QueueUserWorkItem((object data) =>
		{
			StartScript.ui.updateStatistics(Path.GetFileName(file.fullFileName), file.points.Length);
		});
		}

	// Based on: https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
	internal void calcNormals(LasStruct file)
		{
		normals = new Vector3[file.points.Length];
		vertices = new Vector3[file.points.Length];

		if (file.points.Length == 0) return;

		for (int i = 0; i < vertices.Length; i++)
			vertices[i] = file.points[i].xyz;

		tree = new KDTree();
		tree.Build(vertices);

		int numThreads = Environment.ProcessorCount * 2;
		var doneEvents = new ManualResetEvent[numThreads];
		for (int tIx = 0; tIx < numThreads; tIx++)
			{
			doneEvents[tIx] = new ManualResetEvent(false);
			var start = (vertices.Length / numThreads) * tIx;
			var end = (vertices.Length / numThreads) * (tIx + 1);
			if (tIx == numThreads - 1) end = vertices.Length;
			normCallbackData data = new normCallbackData();
			data.start = start;
			data.end = end;
			data.resetEvent = doneEvents[tIx];
			ThreadPool.QueueUserWorkItem(calcNormalCallback, data);
			}

		WaitHandle.WaitAll(doneEvents);
		normBuffer.SetData(normals);
		}

	internal static KDTree tree;
	internal static Vector3[] normals;
	internal static Vector3[] vertices;
	internal class normCallbackData
		{
		public int start;
		public int end;
		public ManualResetEvent resetEvent;
		}
	internal static void calcNormalCallback(object o)
		{
		var data = (normCallbackData)o;

		int k = 8;
		float kInverse = 1f / k;
		KDQuery query = new KDQuery();
		var resultIndices = new List<int>(8);
		for (int i = data.start; i < data.end; i++)
			{
			resultIndices.Clear();
			query.KNearest(tree, vertices[i], k, resultIndices);
			Vector3 sum = Vector3.zero;
			for (int j = 0; j < k; j++)
				sum += vertices[resultIndices[j]];

			Vector3 centroid = sum * kInverse;

			// 3x3 covariance matrix
			float xx = 0; float xy = 0; float xz = 0;
			float yy = 0; float yz = 0; float zz = 0;

			for (int j = 0; j < k; j++)
				{
				var p = vertices[resultIndices[j]] - centroid;
				xx += p.x * p.x;
				xy += p.x * p.y;
				xz += p.x * p.z;
				yy += p.y * p.y;
				yz += p.y * p.z;
				zz += p.z * p.z;
				}

			// Find determinants
			var detX = yy * zz - yz * yz;
			var detY = xx * zz - xz * xz;
			var detZ = xx * yy - xy * xy;

			// Find max determinant
			var detMax = Mathf.Max(detX, Mathf.Max(detY, detZ));
			if (detMax <= 0) continue; // Cannot create a plane

			Vector3 normal = Vector3.zero;
			if (detMax == detX)
				{ normal = new Vector3(detX, xz * yz - xy * zz, xy * yz - xz * yy); }
			else if (detMax == detY)
				{ normal = new Vector3(xz * yz - xy * zz, detY, xy * xz - yz * xx); }
			else if (detMax == detZ)
				{ normal = new Vector3(xy * yz - xz * yy, xy * xz - yz * xx, detZ); }

			normal = normal.normalized;
			normals[i] = normal;
			}

		data.resetEvent.Set();
		}

	internal void setMatDefaults()
		{
		setMatDisplayRad(displayRadius);
		setMatEditCol(Color.yellow);
		setMatDisplayNormals(displayNormals);

		if (StartScript.ui.layerDropdownDesktop != null)
			setMatEditCol(UIManager.layerColors[StartScript.ui.layerDropdownDesktop.value]);
		else
			setMatEditCol(UIManager.layerColors[0]);
		}

	internal void setMatEditCol(Color col)
		{
		if (mr == null) return;
		mr.material.SetColor("_EditCol", col);
		}

	internal void setMatEditPos(Vector3 pos)
		{
		if (mr == null) return;
		mr.material.SetVector("_EditPos", pos);
		}
	internal void setMatEditRad(float rad)
		{
		if (mr == null) return;
		mr.material.SetFloat("_EditRadius", rad * (1f / StartScript.sceneRootScale));
		}
	internal void setMatDisplayRad(float rad)
		{
		if (mr == null) return;
		displayRadius = rad;
		mr.material.SetFloat("_DisplayRadius", displayRadius * (1 / StartScript.sceneRootScale));
		}
	internal void updateMatDisplayRad()
		{
		if (mr == null) return;
		mr.material.SetFloat("_DisplayRadius", displayRadius * StartScript.sceneRootScale);
		}

	private const float displayRadStepSize = 1.16f;
	internal void increaseDisplayRad()
		{
		if (mr == null) return;
		displayRadius *= displayRadStepSize;
		mr.material.SetFloat("_DisplayRadius", displayRadius);
		}
	internal void decreaseDisplayRad()
		{
		if (mr == null) return;
		displayRadius *= (1f / displayRadStepSize);
		mr.material.SetFloat("_DisplayRadius", displayRadius);
		}
	internal void setMatDisplayNormals(bool displayNormals)
		{
		if (mr == null) return;
		this.displayNormals = displayNormals;
		mr.material.SetFloat("_DisplayNormals", displayNormals ? 1 : 0);
		}

	internal void setMatDisplayRoundPoints(bool displayRoundPoints)
		{
		if (mr == null) return;
		this.displayRoundPoints = displayRoundPoints;
		mr.material.SetFloat("_DisplayRoundPoints", displayRoundPoints ? 1 : 0);
		}

	internal void setMatTriggerPress(bool press)
		{
		if (mr == null) return;
		mr.material.SetInt("_TriggerPress", press ? 1 : 0);
		}

	internal void nextPointCloud()
		{
		StartScript.ui.updateStatistics("Point Cloud", files[curtFileIx].points.Length);
		if (curtFileIx >= files.Length - 1) return;
		downloadCurPointCloud();
		curtFileIx++;
		setPointCloud(files[curtFileIx]);
		}

	internal void prevPointCloud()
		{
		StartScript.ui.updateStatistics("Point Cloud", files[curtFileIx].points.Length);
		if (curtFileIx <= 0) return;
		downloadCurPointCloud();
		curtFileIx--;
		setPointCloud(files[curtFileIx]);
		}

	internal Color[] tempColBuffer; // Buffer for intermediate storage of GPU download data
	private void downloadCurPointCloud()
		{
		// Download and update color data
		if (tempColBuffer == null || tempColBuffer.Length < files[curtFileIx].points.Length)
			{
			tempColBuffer = new Color[(int)(files[curtFileIx].points.Length * 1.5f)];
			}

		colBuffer.GetData(tempColBuffer, 0, 0, files[curtFileIx].points.Length);
		ThreadPool.QueueUserWorkItem((object data) =>
			{
				lock (tempColBuffer)
					{
					Color[] buf = (Color[])data;
					for (int i = 0; i < files[curtFileIx].points.Length; i++)
						files[curtFileIx].points[i].col = buf[i];
					}
			},
			tempColBuffer);

		}

	private void OnDisable()
		{
		dispose();
		}

	private void OnDestroy()
		{
		dispose();
		}

	internal void dispose()
		{
		if (vertBuffer != null)
			vertBuffer.Release();
		if (normBuffer != null)
			normBuffer.Release();
		if (colBuffer != null)
			colBuffer.Release();
		}
	}
