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
	internal static int cbPointSize = UnsafeUtility.SizeOf<cbPoint>();
	internal static int numElemsPerSlot = 750000; // Not dynamic for now, as we only work with small point clouds for now
	internal static List<bufferSlot> slots;
	internal static Vector4[] data;
	private static bool initialized;
	internal class bufferSlot
		{
		public PointCloudObject pc;
		public int lastUpdate;
		public int startIx;
        public bool hasNormals;

		public bufferSlot(PointCloudObject pc, int lastUpdate, int startIx)
			{
			this.pc = pc;
			this.lastUpdate = lastUpdate;
			this.startIx = startIx;
            this.hasNormals = false;
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

		slots = new List<bufferSlot>();
        resizePointBuffer (numElemsPerSlot);

		// Buffer for various data
		data = new Vector4[1024];
		dataBuffer = new ComputeBuffer(1024, sizeof(float) * 4);
		Graphics.SetRandomWriteTarget(2, dataBuffer);

		for (int i = 0; i < numSlots; i++)
			slots.Add(new bufferSlot(null, updateCounter, (pointBuffer.count / 3) * i));

		initialized = true;
		}

    internal static void resizePointBuffer(int numElemsPerSlot)
        {
        if (pointBuffer != null) pointBuffer.Dispose ();

        pointBuffer = new ComputeBuffer (numSlots * numElemsPerSlot, cbPointSize); // 4 bytes per float, 3 floats		
        Graphics.SetRandomWriteTarget (1, pointBuffer);

        slots.Clear ();
        for (int i = 0; i < numSlots; i++)
            slots.Add (new bufferSlot (null, updateCounter, (pointBuffer.count / 3) * i));
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

    /// <summary>
    /// Uploads normals
    /// </summary>
    internal static void uploadNormal(PointCloudObject pc)
        {
        bufferSlot slot = null;
        foreach(var s in slots)
            if (s.pc == pc)
                { slot = s; break; }
        if (slot != null && !slot.hasNormals) uploadPointCloud (slot, true);
        }

	/// <summary>
	/// Checks if point cloud has uncommitted data on GPU, returns true if download 
	/// must be done, where it downloads and saves on download completion, if ready
	/// returns false
	/// </summary>
	/// <returns></returns>
	internal static bool downLoadBeforeSave(PointCloudObject pc)
		{
		StartScript.paused = true;
		foreach (var slot in slots)
			if (slot.pc == pc)
				{
				AsyncGPUReadback.Request(pointBuffer, slot.pc.file.points.Length * cbPointSize, slot.startIx, slot.pc.downloadAndSaveCallback);
				return true;
				}

		return false;
		}

	internal static void downloadPointCloudAsync(bufferSlot slot)
		{
		slot.pc.downloading = true;
		AsyncGPUReadback.Request(pointBuffer, slot.pc.file.points.Length * cbPointSize, slot.startIx, slot.pc.downloadCallback);
		}

	internal static void uploadPointCloud(bufferSlot slot, bool displayNormals)
		{
		// Points, cols and norms have same length
		cbPoint[] cbPoints = new cbPoint[slot.pc.file.points.Length];

        if (pointBuffer.count < cbPoints.Length * 3)
            {
            resizePointBuffer ((int)(cbPoints.Length * 1.25f / 3));
            }

		for (int i = 0; i < slot.pc.file.points.Length; i++)
			{
			cbPoints[i].vert = slot.pc.file.points[i].xyz;
			cbPoints[i].col = slot.pc.file.points[i].col;
			if (displayNormals) cbPoints[i].norm = slot.pc.normals[i];
			cbPoints[i].classification = (int)slot.pc.file.points[i].classification;
			}

        slot.hasNormals = displayNormals;
		pointBuffer.SetData(cbPoints, 0, slot.startIx, cbPoints.Length);
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
