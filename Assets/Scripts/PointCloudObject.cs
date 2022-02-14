using DataStructures.ViliWonka.KDTree;
using System.Collections;
using System.Collections.Generic;
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
	internal float displayRadius;
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

	internal void setPointCloud(LasStruct file)
		{
		init();
		var vertices = new Vector3[file.points.Length];
		var colors = new Color[file.points.Length];
		var indices = new int[file.points.Length];

		if (displayNormals)
			calcNormals(file);

		setMatDefaults();

		// Scale down
		if (file.points.Length > 0)
			{
			for (int i = 0; i < vertices.Length; i++)
				{
				vertices[i] = file.points[i].xyz * 0.1f;
				colors[i] = file.points[i].col;
				indices[i] = i;
				}
			}

		mf.mesh.SetVertices(vertices);
		mf.mesh.SetIndices(indices, MeshTopology.Points, 0);

		vertBuffer.SetData(vertices);
		colBuffer.SetData(colors);

		StartScript.ui.updateStatistics("Point Cloud", file.points.Length);
		}

	// Based on: https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
	internal void calcNormals(LasStruct file)
		{
		Vector3[] normals = new Vector3[file.points.Length];
		Vector3[] vertices = new Vector3[file.points.Length];


		if (file.points.Length == 0) return;

		for (int i = 0; i < vertices.Length; i++)
			vertices[i] = file.points[i].xyz;

		KDTree tree = new KDTree();
		tree.Build(vertices);

		int k = 8;
		float kInverse = 1f / k;
		KDQuery query = new KDQuery();
		var resultIndices = new List<int>(8);
		for (int i = 0; i < vertices.Length; i++)
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

		normBuffer.SetData(normals);
		}

	internal void setMatDefaults()
		{
		setMatDisplayRad(0.02f);
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
		mr.material.SetFloat("_EditRadius", rad * (1f /StartScript.sceneRootScale));
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
		for (int i = 0; i < files[curtFileIx].points.Length; i++)
			files[curtFileIx].points[i].col = tempColBuffer[i];
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
