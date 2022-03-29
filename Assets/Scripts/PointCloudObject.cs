using DataStructures.ViliWonka.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class PointCloudObject
	{
	internal LasFile file;
	internal Vector3[] normals;
	internal bool downloading;
	internal string wantFileName; // File name top be used on async file write

	public PointCloudObject(LasFile file, bool calculateNormals)
		{
		this.file = file;

		if (calculateNormals)
			calcNormals();
		}

	internal class normCallbackData
		{
		public KDTree tree;
		public LasFile lasFile;
		public Vector3[] normals;
		public int start;
		public int end;
		public ManualResetEvent resetEvent;
		}

	// Based on: https://www.ilikebigbits.com/2015_03_04_plane_from_points.html
	internal void calcNormals()
		{
		normals = new Vector3[file.points.Length];

		var tempPoints = new Vector3[file.points.Length];
		for (int i = 0; i < tempPoints.Length; i++)
			tempPoints[i] = file.points[i].xyz;

		var tree = new KDTree();
		tree.Build(tempPoints);

		int numThreads = Environment.ProcessorCount * 2;
		var doneEvents = new ManualResetEvent[numThreads];
		for (int tIx = 0; tIx < numThreads; tIx++)
			{
			doneEvents[tIx] = new ManualResetEvent(false);
			var start = (file.points.Length / numThreads) * tIx;
			var end = (file.points.Length / numThreads) * (tIx + 1);
			if (tIx == numThreads - 1) end = file.points.Length;
			normCallbackData data = new normCallbackData();
			data.tree = tree;
			data.normals = normals;
			data.lasFile = file;
			data.start = start;
			data.end = end;
			data.resetEvent = doneEvents[tIx];
			ThreadPool.QueueUserWorkItem(calcNormalCallback, data);
			}

		WaitHandle.WaitAll(doneEvents);
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
			query.KNearest(data.tree, data.lasFile.points[i].xyz, k, resultIndices);
			Vector3 sum = Vector3.zero;
			for (int j = 0; j < k; j++)
				sum += data.lasFile.points[resultIndices[j]].xyz;

			Vector3 centroid = sum * kInverse;

			// 3x3 covariance matrix
			float xx = 0; float xy = 0; float xz = 0;
			float yy = 0; float yz = 0; float zz = 0;

			for (int j = 0; j < k; j++)
				{
				var p = data.lasFile.points[resultIndices[j]].xyz - centroid;
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
			data.normals[i] = normal;
			}

		data.resetEvent.Set();
		}

	internal void downloadCallback(AsyncGPUReadbackRequest request)
		{
		if (!request.hasError)
			{
			var data = request.GetData<ComputeBufferManager.cbPoint>();
			for (int i = 0; i < data.Length; i++)
				file.points[i].classification = (byte)data[i].classification;

			downloading = false;

			}
		}

	internal void downloadAndSaveCallback(AsyncGPUReadbackRequest request)
		{
		if (!request.hasError)
			{
			var data = request.GetData<ComputeBufferManager.cbPoint>();
			for (int i = 0; i < data.Length; i++)
				{
				if (data[i].classification != 0)
					{
					var wtf = 0;
					}
				file.points[i].classification = (byte)data[i].classification;
				}


			downloading = false;
			LasWriter.writeLASFile(file, wantFileName);
			}
		}
	}
