﻿using DataStructures.ViliWonka.KDTree;
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
	internal int curtFileIx = 0;
	internal bool displayNormals = true;
	internal bool initialized;
	void Start()
		{
		init();
		}

	void Update()
		{

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

	internal void setPointCloud(LasStruct file)
		{
		init();
		var vertices = new Vector3[file.points.Length];
		var colors = new Color[file.points.Length];
		var indices = new int[file.points.Length];

		if (displayNormals)
			{
			calcNormals(file);
			}

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
		}

	// Based on: https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
	internal void calcNormals(LasStruct file)
		{
		Vector3[] normals = new Vector3[file.points.Length];
		Vector3[] vertices = new Vector3[file.points.Length];

		// Scale down
		if (file.points.Length > 0)
			{
			for (int i = 0; i < vertices.Length; i++)
				vertices[i] = file.points[i].xyz;
			}

		KDTree tree = new KDTree();
		tree.Build(vertices);

		int k = 8;
		int kInverse = 1 / k;
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

			var detX = yy * zz - yz * yz;
			var detY = xx * zz - xz * xz;
			var detZ = xx * yy - xy * xy;

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

	internal void setEditCol(Color col)
		{
		if (mr == null) return;
		mr.material.SetColor("_EditCol", col);
		}

	internal void setEditPos(Vector3 pos)
		{
		if (mr == null) return;
		mr.material.SetVector("_EditPos", pos); 
		}
	internal void setEditRad(float rad)
		{
		if (mr == null) return;
		mr.material.SetFloat("_EditRadius", rad * (1 / StartScript.curscale));
		}

	internal void setTriggerPress(bool press)
		{
		if (mr == null) return;
		mr.material.SetInt("_TriggerPress", press ? 1 : 0);
		}

	internal void nextPointCloud()
		{
		downloadCurPointCloud();
		if (curtFileIx >= files.Length - 1) return;
		curtFileIx++;
		setPointCloud(files[curtFileIx]);
		}

	internal void prevPointCloud()
		{
		downloadCurPointCloud();
		if (curtFileIx <= 0) return;
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

	private void OnDisable ()
		{
		dispose();
		}

	private void OnDestroy ()
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