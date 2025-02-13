﻿using Kinetic.IO;
using Loh.Library;
using Loh.Values;

namespace Loh;

public class _LohTest
{

	public static void Test()
	{
		LohEngine.Init();

		LohState fn = LohEngine.Require(null, StringIO.Read(FileSystem.GetAbsolute("F:/C# Workspace/Loh/TheLohTest.loh")), false);

		for(int i = 0; i < 10; i++)
		{
			var clock1 = DateTime.Now;
			LohEngine.Exec(fn.TopFunc);
			Console.WriteLine((DateTime.Now - clock1).TotalMilliseconds);
		}
	}

}
