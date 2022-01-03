using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
	{
	[TestFixture]
	public class NewTestScript
		{
		private LasStruct file;

		[OneTimeSetUp]
		public void SetUp()
			{
			file = LasReader.readLASFile("Assets/Tests/Data/pointcloud1.las");
			}

		[OneTimeTearDown]
		public void TearDown()
			{
			
			}

		[Test]
		public void numPointsIsCorrect()
			{
			Assert.IsTrue(file.points.Length == 131072);
			}

		[Test]
		public void correctPointDataFormat()
			{
			Assert.IsTrue((int)file.header.pointDataFormat == 3);
			}

		[Test]
		public void boundsIsCorrect()
			{
			// Max
			Assert.IsTrue(Mathf.Abs((float)file.header.maxX - 255.78961f) < 0.001f);
			Assert.IsTrue(Mathf.Abs((float)file.header.maxY - 214.02217f) < 0.001f);
			Assert.IsTrue(Mathf.Abs((float)file.header.maxZ - 24.877889f) < 0.001f);

			// Min
			Assert.IsTrue(Mathf.Abs((float)file.header.minX - -158.87103f) < 0.001f);
			Assert.IsTrue(Mathf.Abs((float)file.header.minY - -45.937194f) < 0.001f);
			Assert.IsTrue(Mathf.Abs((float)file.header.minZ - -17.032073f) < 0.001f);
			}

		}
	}
