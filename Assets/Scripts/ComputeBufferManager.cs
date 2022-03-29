using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

public static class ComputeBufferManager
	{
	internal static ComputeBuffer dataBuffer; // Buffer to store arbitrary data
	internal static ComputeBuffer pointBuffer;

	internal static int updateCounter = -1;
	internal static int numSlots = 3;
	internal static int numElemsPerSlot = 1000000; // Not dynamic for now, as we only work with small point clouds for now
	internal static List<bufferSlot> slots;
	internal static Vector4[] data;
	private static bool initialized;
	internal class bufferSlot
		{
		public PointCloudObject pc;
		public int lastUpdate;
		public int startIx;

		public bufferSlot(PointCloudObject pc, int lastUpdate, int startIx)
			{
			this.pc = pc;
			this.lastUpdate = lastUpdate;
			this.startIx = startIx;
			}
		}

	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	internal struct cbPoint
		{
		public Vector3 vert;
		public Vector3 norm;
		public Color col;
		public int classification;
		}

	internal static void init()
		{
		if (initialized) return;

		int cbPointSize = UnsafeUtility.SizeOf<cbPoint>();
		pointBuffer = new ComputeBuffer(numSlots * numElemsPerSlot, cbPointSize); // 4 bytes per float, 3 floats		
		Graphics.SetRandomWriteTarget(1, pointBuffer);

		// Buffer for various data
		data = new Vector4[1024];
		dataBuffer = new ComputeBuffer(1024, sizeof(float) * 4);
		Graphics.SetRandomWriteTarget(2, dataBuffer);

		slots = new List<bufferSlot>();
		for (int i = 0; i < numSlots; i++)
			slots.Add(new bufferSlot(null, updateCounter, (pointBuffer.count / 3) * i));

		initialized = true;
		}

	internal static int setPointCloud(PointCloudObject pc, bool displayNormals)
		{
		bool isOnGpu = false;
		bufferSlot curSlot = null;
		int cbOffset = 0;
		foreach (var s in slots)
			if (s.pc == pc)
				{ isOnGpu = true; curSlot = s; break; }

		if (isOnGpu)
			{
			curSlot.lastUpdate = ++updateCounter;
			cbOffset = curSlot.startIx;
			}
		else
			{
			int smallestUpdateCounter = int.MaxValue;
			foreach (var s in slots)
				if (s.lastUpdate < smallestUpdateCounter)
					smallestUpdateCounter = s.lastUpdate;

			foreach (var s in slots)
				if (s.lastUpdate == smallestUpdateCounter)
					{
					if (s.pc != null)
						downloadPointCloudAsync(s);

					s.pc = pc;
					s.lastUpdate = ++updateCounter;
					uploadPointCloud(s, displayNormals);
					cbOffset = s.startIx;
					break;
					}
			}

		return cbOffset;
		}

	internal static void downloadPointCloudAsync(bufferSlot slot)
		{
		slot.pc.downloading = true;
		AsyncGPUReadback.Request(pointBuffer, pointBuffer.count / numSlots, slot.startIx, slot.pc.downloadPointCloudCallback);
		}

	internal static void uploadPointCloud(bufferSlot slot, bool displayNormals)
		{
		// Points, cols and norms have same length
		cbPoint[] points = new cbPoint[slot.pc.points.Length];
		for (int i = 0; i < slot.pc.points.Length; i++)
			{
			points[i].vert = slot.pc.points[i];
			points[i].col = slot.pc.colors[i];
			if (slot.pc.normals != null) points[i].norm = slot.pc.normals[i];
			points[i].classification = slot.pc.classification[i];
			}

		pointBuffer.SetData(points, 0, slot.startIx, points.Length);
		}

	/// <summary>
	/// Uploads a color look up table (LUT) for label display colors
	/// Maximum 32 colors
	/// </summary>
	/// <param name="colors"></param>
	internal static void uploadLabelColors(Color[] colors)
		{
		// Colors are stored in the first 32 slots opf the data buffer
		var len = colors.Length;
		if (len > 32) len = 32; // Can max upload 32 colors
		dataBuffer.SetData(colors, 0, 0, len);
		}

	internal static void dispose()
		{
		if (pointBuffer != null)
			pointBuffer.Release();
		if (dataBuffer != null)
			dataBuffer.Release();
		}
	}
