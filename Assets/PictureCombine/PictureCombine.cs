using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System;
using System.IO;

/*
 * 此脚本用途：把指定文件夹内的所有.gif,.jpg,.png图片全部整合为一个PNG图片，提供图文混合使用表情包
 这里引用的System.Drawing是在C:\Program Files (x86)\Unity\Editor\Data\Mono\lib\mono\2.0目录下，这个是unity安装的目录;
 */
public static class PictureCombine
{
	[MenuItem ("Assets/Create/PictureCombine")]
	static void GetAllTexture ()
	{  
		//扫描文件夹，把所有GIF和JPG路径存储，先把每一个GIF的帧图片分表情依次合成，最后再合成JPG,PNG
		//需要把GIF表情帧图片合成，包括不同的长宽的表情，以及不同尺寸的JPG
		string folderPath = Application.dataPath + "/PictureCombine/photo/";
		List<string> gifList = new List<string> ();
		List<string> pngList = new List<string> ();
		List<string> jpgList = new List<string> ();

		if (Directory.Exists (folderPath)) 
		{
			DirectoryInfo direction = new DirectoryInfo (folderPath);
			FileInfo[] files = direction.GetFiles ("*", SearchOption.AllDirectories);
			//Debug.Log ("文件总数量："+files.Length);

			for (int i = 0; i < files.Length; i++) 
			{
				if (files [i].Name.EndsWith (".meta")) 
				{
					continue;
				}

				if (files [i].ToString ().Contains (".gif"))
					gifList.Add (files [i].ToString ());

				if (files [i].ToString ().Contains (".jpg"))
					jpgList.Add (files [i].ToString ());

				if (files [i].ToString ().Contains (".png"))
					pngList.Add (files [i].ToString ());
			}
		}
		CombineAll (gifList, jpgList, pngList);
	}

	static void CombineAll (List<string> gifs, List<string> jpgs, List<string> pngs)
	{
		Bitmap gifBmp = GetGifCombine (gifs);
		Bitmap jpgBmp = GetJpgCombine (jpgs, gifBmp);
		Bitmap pngBmp = GetPngCombine (pngs, gifBmp);
		CombineAllBitmap (gifBmp, jpgBmp, pngBmp);
	}

	static  Bitmap GetGifCombine (List<string> gifs)
	{
		if (gifs.Count == 0)
			return null;
		
		string path = "";
		Bitmap newBmp = new Bitmap (1, 1);

		for (int i = 0; i < gifs.Count; i++) 
		{
			path = gifs [i];
			Image gif = Image.FromFile (path);
			FrameDimension fd = new FrameDimension (gif.FrameDimensionsList [0]);
			int count = gif.GetFrameCount (fd);
			int bmpHeight = 0;
			int bmpWidth = 0;

			if (newBmp.Height > 1)
				bmpHeight = newBmp.Height;

			if (newBmp.Width > 1)
				bmpWidth = newBmp.Width;

			for (int j = 0; j < count; j++) {
				gif.SelectActiveFrame (fd, j);
				Bitmap gifBmp = new Bitmap (gif);
			
				if (i == gifs.Count - 1 && j == count - 1) 
				{
					newBmp = CombineGifBitmap (newBmp, gifBmp, bmpWidth, bmpHeight, i, j, count, true, "GifCombine");
				} else 
				{
					newBmp = CombineGifBitmap (newBmp, gifBmp, bmpWidth, bmpHeight, i, j, count, false, "GifCombine");
				}	
			}
		}
		return newBmp;
	}

	static Bitmap CombineGifBitmap (Bitmap bmp1, Bitmap bmp2, int width, int height, int row, int index, int count, Boolean isSave, string saveName)
	{   
		if (width < bmp2.Width * count)
			width = bmp2.Width * count;

		if (row > 0)
			row = 1;
		
		//一个系列表情的GIF在同一行合并，第二个表情GIF，需要在下一行进行合并,依次循环,需要每次切换高度，高度需要累加
		Bitmap newBmp = new Bitmap (width, height + bmp2.Height);
		System.Drawing.Graphics g = System.Drawing.Graphics.FromImage (newBmp);
		g.DrawImage (bmp1, 0, 0);
		g.DrawImage (bmp2, bmp2.Width * index, height * row);
		g.Save ();

		string savePath = Application.dataPath + "/PictureCombine/combine/";

		if (isSave)
			newBmp.Save (savePath + saveName + ".png", ImageFormat.Png);

		return newBmp;
	}


	static  Bitmap GetJpgCombine (List<string> jpgs, Bitmap lastBmp)
	{
		if (jpgs.Count == 0)
			return null;

		string path = "";
		Bitmap newBmp = new Bitmap (1, 1);
		int LastWidth = 0;
		int LastHeight = 0;
		int maxWidth = 0;
		int rowIndex = 0; 
		int lastRowIndex = 0; 
		Boolean isNextRow = false;

		if (lastBmp == null)
			maxWidth = 512;
		else
			maxWidth = lastBmp.Width;

		for (int i = 0; i < jpgs.Count; i++) 
		{
			path = jpgs [i];
			Image jpg = Image.FromFile (path);
			Bitmap jpgBmp = new Bitmap (jpg);
			rowIndex = 0;
			isNextRow = false;

			rowIndex = (LastWidth + jpgBmp.Width) / maxWidth;

			if (lastRowIndex < rowIndex) 
			{
				LastWidth = 0;
				LastHeight = newBmp.Height;
				lastRowIndex = 0;
				isNextRow = true;
			}
				

			if (i == jpgs.Count - 1) 
			{
				newBmp = CombineStaticBitmap (newBmp, jpgBmp, maxWidth, LastWidth, LastHeight, isNextRow, true, "JpgCombine");

			} else 
			{
				newBmp = CombineStaticBitmap (newBmp, jpgBmp, maxWidth, LastWidth, LastHeight, isNextRow, false, "JpgCombine");
				//记录当前添加的最大宽度
				LastWidth = LastWidth + jpgBmp.Width;
			}	
		}
		return newBmp;
	}

	static  Bitmap GetPngCombine (List<string> pngs, Bitmap lastBmp)
	{
		if (pngs.Count == 0)
			return null;

		string path = "";
		Bitmap newBmp = new Bitmap (1, 1);
		int LastWidth = 0;
		int LastHeight = 0;
		int maxWidth = 0;
		int rowIndex = 0; 
		int lastRowIndex = 0; 
		Boolean isNextRow = false;

		if (lastBmp == null)
			maxWidth = 512;
		else
			maxWidth = lastBmp.Width;


		for (int i = 0; i < pngs.Count; i++) 
		{
			path = pngs [i];
			Image png = Image.FromFile (path);
			Bitmap pngBmp = new Bitmap (png);
			isNextRow = false;
			rowIndex = (LastWidth + pngBmp.Width) / maxWidth;

			if (lastRowIndex < rowIndex) {   
				//Debug.Log ("换行重置宽度坐标,添加上一行的高度:"+newBmp.Height);
				LastWidth = 0;
				LastHeight = newBmp.Height;
				lastRowIndex = 0;
				isNextRow = true;
			}
				
			if (i == pngs.Count - 1) {
				newBmp = CombineStaticBitmap (newBmp, pngBmp, maxWidth, LastWidth, LastHeight, isNextRow, true, "PngCombine");
			} else {
				newBmp = CombineStaticBitmap (newBmp, pngBmp, maxWidth, LastWidth, LastHeight, isNextRow, false, "PngCombine");
				//记录当前添加的最大宽度
				LastWidth = LastWidth + pngBmp.Width;
			}	
		}
		return newBmp;
	}

	static Bitmap CombineStaticBitmap (Bitmap bmp1, Bitmap bmp2, int maxWidth, int lastWidth, int lastHeight, Boolean isNextRow, Boolean isSave, string saveName)
	{   
		int height = 0;

		if (isNextRow) 
		{
			//Debug.Log ("修改新高度:" + bmp1.Height + "+" + bmp2.Height);
			height = bmp1.Height + bmp2.Height;
		} else 
		{  
			if (bmp1.Height >= bmp2.Height)
				height = bmp1.Height;
			else
				height = bmp2.Height;
		}
			
		Bitmap newBmp = new Bitmap (maxWidth, height);
		System.Drawing.Graphics g = System.Drawing.Graphics.FromImage (newBmp);
		g.DrawImage (bmp1, 0, 0);
		g.DrawImage (bmp2, lastWidth, lastHeight);
		g.Save ();

		string savePath = Application.dataPath + "/PictureCombine/combine/"; 

		if (isSave)
			newBmp.Save (savePath + saveName + ".png", ImageFormat.Png);

		return newBmp;
	}


	static void CombineAllBitmap (Bitmap bmp1, Bitmap bmp2, Bitmap bmp3)
	{   
		int maxWidth = 0;

		if (bmp1.Width >= bmp2.Width)
			maxWidth = bmp1.Width;
		else
			maxWidth = bmp2.Width;

		if (bmp3.Width >= maxWidth)
			maxWidth = bmp3.Width;

		int maxHeight = bmp1.Height + bmp2.Height + bmp3.Height;

		Bitmap newBmp = new Bitmap (maxWidth, maxHeight);

		System.Drawing.Graphics g = System.Drawing.Graphics.FromImage (newBmp);
		g.DrawImage (bmp1, 0, 0);
		g.DrawImage (bmp2, 0, bmp1.Height);
		g.DrawImage (bmp3, 0, bmp1.Height + bmp2.Height);
		g.Save ();

		string savePath = Application.dataPath + "/PictureCombine/combine/";
		newBmp.Save (savePath + "TotalCombine.png", ImageFormat.Png);
		Debug.Log ("Finish");
	}
}
