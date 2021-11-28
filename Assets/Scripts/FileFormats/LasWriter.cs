using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class LasWriter
	{
	public static void writeLASFile(LasStruct lasFile, string fileName)
		{
		//if (File.Exists(fileName)) return;
		FileStream fs = File.Open(fileName, FileMode.Create);
		writeLASHeader(fs, lasFile.header);
		writeLASPoints(fs, lasFile.header, lasFile.points);

		fs.Close();
		}

	private static void writeLASHeader(FileStream file, LasHeader header)
		{
		byte[] bytes = new byte[header.headerSize]; // Size of header

		for (int i = 0; i < 4; i++)
			bytes[0 + i] = header.fileSignature[i];

		var temp = BitConverter.GetBytes(header.fileSourceId);
		Array.Copy(temp, 0, bytes, 4, 2);
		temp = BitConverter.GetBytes(header.fileSourceId);
		Array.Copy(temp, 0, bytes, 6, 2);
		temp = BitConverter.GetBytes(header.projectId1);
		Array.Copy(temp, 0, bytes, 8, 4);
		temp = BitConverter.GetBytes(header.projectId2);
		Array.Copy(temp, 0, bytes, 12, 2);
		for (int i = 0; i < 8; i++)
			bytes[i + 16] = header.projectId4[i];

		bytes[24] = header.versionMajor;
		bytes[25] = header.versionMinor;

		for (int i = 0; i < 32; i++)
			bytes[26 + i] = header.systemIdentifier[i];

		for (int i = 0; i < 32; i++)
			bytes[58 + i] = header.generatingSoftware[i];


		temp = BitConverter.GetBytes((ushort)System.DateTime.Today.Day);
		Array.Copy(temp, 0, bytes, 90, 2);
		temp = BitConverter.GetBytes((ushort)System.DateTime.Today.Year);
		Array.Copy(temp, 0, bytes, 92, 2);
		temp = BitConverter.GetBytes(header.headerSize);
		Array.Copy(temp, 0, bytes, 94, 2);
		temp = BitConverter.GetBytes((uint)header.headerSize); // TODO: Is this correct
		Array.Copy(temp, 0, bytes, 96, 2);
		temp = BitConverter.GetBytes((uint)0); // Does not support variable length records
		Array.Copy(temp, 0, bytes, 100, 4);
		bytes[104] = header.pointDataFormat;
		temp = BitConverter.GetBytes(header.pointDataLength);
		Array.Copy(temp, 0, bytes, 105, 2);
		temp = BitConverter.GetBytes(header.numPoints); // Should not have changed since load
		Array.Copy(temp, 0, bytes, 107, 4);

		for (int i = 0; i < 5; i++)
			{
			temp = BitConverter.GetBytes(header.numPointsByReturn[i]);
			Array.Copy(temp, 0, bytes, 111 + i * 4, 4);
			}

		// Scalefactor xyz
		temp = BitConverter.GetBytes(header.xScaleFactor);
		Array.Copy(temp, 0, bytes, 131, 8);
		temp = BitConverter.GetBytes(header.yScaleFactor);
		Array.Copy(temp, 0, bytes, 139, 8);
		temp = BitConverter.GetBytes(header.zScaleFactor);
		Array.Copy(temp, 0, bytes, 147, 8);

		// Offset xyz
		temp = BitConverter.GetBytes(header.xOffset);
		Array.Copy(temp, 0, bytes, 155, 8);
		temp = BitConverter.GetBytes(header.yOffset);
		Array.Copy(temp, 0, bytes, 163, 8);
		temp = BitConverter.GetBytes(header.zOffset);
		Array.Copy(temp, 0, bytes, 171, 8);

		// Min xyz
		temp = BitConverter.GetBytes(header.maxX);
		Array.Copy(temp, 0, bytes, 179, 8);
		temp = BitConverter.GetBytes(header.maxY);
		Array.Copy(temp, 0, bytes, 187, 8);
		temp = BitConverter.GetBytes(header.maxZ);
		Array.Copy(temp, 0, bytes, 195, 8);

		// Max xyz
		temp = BitConverter.GetBytes(header.minX);
		Array.Copy(temp, 0, bytes, 203, 8);
		temp = BitConverter.GetBytes(header.minY);
		Array.Copy(temp, 0, bytes, 211, 8);
		temp = BitConverter.GetBytes(header.minZ);
		Array.Copy(temp, 0, bytes, 219, 8);

		file.Write(bytes, 0, bytes.Length);
		}

	private static void writeLASPoints(FileStream file, LasHeader header, LasPoint[] points)
		{
		int lasFormat = header.pointDataFormat;

		for (int i = 0; i < points.Length; i++)
			{
			points[i].xyz.x /= (float)header.xScaleFactor;
			points[i].xyz.y /= (float)header.yScaleFactor;
			points[i].xyz.z /= (float)header.zScaleFactor;
			}

		for (int i = 0; i < points.Length; i++)
			{
			if (lasFormat == 0) writeLasPointV0(header, points[i], file);
			else if (lasFormat == 1) writeLasPointV1(header, points[i], file);
			else if (lasFormat == 2) writeLasPointV2(header, points[i], file);
			else if (lasFormat == 3) writeLasPointV3(header, points[i], file);
			}
		}

	private static void writeLasPointV0(LasHeader header, LasPoint point, FileStream file)
		{
		byte[] bytes = new byte[header.pointDataLength];
		var temp = BitConverter.GetBytes((int)point.xyz.x);
		Array.Copy(temp, 0, bytes, 0, 4);
		temp = BitConverter.GetBytes((int)point.xyz.y);
		Array.Copy(temp, 0, bytes, 4, 4);
		temp = BitConverter.GetBytes((int)point.xyz.z);
		Array.Copy(temp, 0, bytes, 8, 4);

		temp = BitConverter.GetBytes(point.instensity);
		Array.Copy(temp, 0, bytes, 12, 2);
		bytes[15] = point.classification;
		bytes[16] = point.scanAngleRank;
		bytes[17] = point.scanDirectionFlag;
		temp = BitConverter.GetBytes(point.pointSourceId);
		Array.Copy(temp, 0, bytes, 18, 2);

		file.Write(bytes, 0, bytes.Length);
		}

	private static void writeLasPointV1(LasHeader header, LasPoint point, FileStream file)
		{
		byte[] bytes = new byte[header.pointDataLength];
		var temp = BitConverter.GetBytes((int)point.xyz.x);
		Array.Copy(temp, 0, bytes, 0, 4);
		temp = BitConverter.GetBytes((int)point.xyz.y);
		Array.Copy(temp, 0, bytes, 4, 4);
		temp = BitConverter.GetBytes((int)point.xyz.z);
		Array.Copy(temp, 0, bytes, 8, 4);

		temp = BitConverter.GetBytes(point.instensity);
		Array.Copy(temp, 0, bytes, 12, 2);
		bytes[15] = point.classification;
		bytes[16] = point.scanAngleRank;
		bytes[17] = point.userData;
		temp = BitConverter.GetBytes(point.pointSourceId);
		Array.Copy(temp, 0, bytes, 18, 2);
		temp = BitConverter.GetBytes(point.GPSTime);
		Array.Copy(temp, 0, bytes, 20, 8);

		file.Write(bytes, 0, bytes.Length);
		}

	private static void writeLasPointV2(LasHeader header, LasPoint point, FileStream file)
		{
		byte[] bytes = new byte[header.pointDataLength];
		var temp = BitConverter.GetBytes((int)point.xyz.x);
		Array.Copy(temp, 0, bytes, 0, 4);
		temp = BitConverter.GetBytes((int)point.xyz.y);
		Array.Copy(temp, 0, bytes, 4, 4);
		temp = BitConverter.GetBytes((int)point.xyz.z);
		Array.Copy(temp, 0, bytes, 8, 4);

		temp = BitConverter.GetBytes(point.instensity);
		Array.Copy(temp, 0, bytes, 12, 2);
		bytes[15] = point.classification;
		bytes[16] = point.scanAngleRank;
		bytes[17] = point.userData;
		temp = BitConverter.GetBytes(point.pointSourceId);
		Array.Copy(temp, 0, bytes, 18, 2);

		temp = BitConverter.GetBytes((ushort)point.col.r);
		Array.Copy(temp, 0, bytes, 20, 2);
		temp = BitConverter.GetBytes((ushort)point.col.g);
		Array.Copy(temp, 0, bytes, 22, 2);
		temp = BitConverter.GetBytes((ushort)point.col.b);
		Array.Copy(temp, 0, bytes, 24, 2);

		file.Write(bytes, 0, bytes.Length);
		}

	private static void writeLasPointV3(LasHeader header, LasPoint point, FileStream file)
		{
		byte[] bytes = new byte[header.pointDataLength];
		var temp = BitConverter.GetBytes((int)point.xyz.x);
		Array.Copy(temp, 0, bytes, 0, 4);
		temp = BitConverter.GetBytes((int)point.xyz.y);
		Array.Copy(temp, 0, bytes, 4, 4);
		temp = BitConverter.GetBytes((int)point.xyz.z);
		Array.Copy(temp, 0, bytes, 8, 4);

		temp = BitConverter.GetBytes(point.instensity);
		Array.Copy(temp, 0, bytes, 12, 2);
		bytes[15] = point.classification;
		bytes[16] = point.scanAngleRank;
		bytes[17] = point.userData;
		temp = BitConverter.GetBytes(point.pointSourceId);
		Array.Copy(temp, 0, bytes, 18, 2);
		temp = BitConverter.GetBytes(point.GPSTime);
		Array.Copy(temp, 0, bytes, 20, 8);
		temp = BitConverter.GetBytes((ushort)point.col.r);
		Array.Copy(temp, 0, bytes, 28, 2);
		temp = BitConverter.GetBytes((ushort)point.col.g);
		Array.Copy(temp, 0, bytes, 30, 2);
		temp = BitConverter.GetBytes((ushort)point.col.b);
		Array.Copy(temp, 0, bytes, 32, 2);

		file.Write(bytes, 0, bytes.Length);
		}
	}

