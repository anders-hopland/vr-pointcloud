using DataStructures.ViliWonka.KDTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StartScript : MonoBehaviour
	{

	internal static GameObject selectedObj;
	internal static Material selectedObjMat;

	internal ComputeBuffer vertBuffer;
	internal ComputeBuffer normBuffer;
	internal ComputeBuffer colBuffer;
	void Start()
		{
		var ret = LasReader.readLASFile("C:\\Users\\Anders\\Documents\\trd15K.las");

		var vertices = new Vector3[ret.points.Length];
		var colors = new Color[ret.points.Length];
		var indices = new int[ret.points.Length];
		Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
		if (ret.points.Length > 0)
			{
			// Reinitialize if we see that we have points
			bounds = new Bounds(ret.points[0].xyz, Vector3.zero);
			for (int i = 0; i < vertices.Length; i++)
				{
				vertices[i] = ret.points[i].xyz * 0.1f;
				bounds.Encapsulate(vertices[i]);
				colors[i] = ret.points[i].col;
				indices[i] = i;
				}
			}

		var normals = getNormals(vertices);

		vertBuffer = new ComputeBuffer(vertices.Length, 4 * 3); // 4 bytes per float, 3 floats
		vertBuffer.SetData(vertices);
		Graphics.SetRandomWriteTarget(1, vertBuffer);

		normBuffer = new ComputeBuffer(normals.Length, 4 * 3);
		normBuffer.SetData(normals);
		Graphics.SetRandomWriteTarget(2, normBuffer);

		colBuffer = new ComputeBuffer(colors.Length, 4 * 4);
		colBuffer.SetData(colors);
		Graphics.SetRandomWriteTarget(3, colBuffer);

		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane); // Uses a primitive to get meshfilter and meshrenderer
		selectedObj = go;
		go.GetComponent<MeshFilter>().mesh.SetVertices(vertices);
		go.GetComponent<MeshFilter>().mesh.SetIndices(indices, MeshTopology.Points, 0);
		selectedObjMat = new Material(Shader.Find("Custom/PointCloudShader"));
		go.GetComponent<MeshRenderer>().material = selectedObjMat;
		go.transform.parent = this.transform.parent;
		}

	// Update is called once per frame
	void Update()
		{

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
	}
