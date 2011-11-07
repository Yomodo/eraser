﻿/* 
 * $Id: FileSystem.cs 2161 2010-06-13 02:30:16Z lowjoel $
 * Copyright 2008-2010 The Eraser Project
 * Original Author: Joel Low <lowjoel@users.sourceforge.net>
 * Modified By: 
 * 
 * This file is part of Eraser.
 * 
 * Eraser is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 * 
 * Eraser is distributed in the hope that it will be useful, but WITHOUT ANY
 * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
 * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * 
 * A copy of the GNU General Public License can be found at
 * <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

using Eraser.Util;

namespace Eraser.Plugins.ExtensionPoints
{
	/// <summary>
	/// Provides functions to handle erasures specfic to file systems.
	/// </summary>
	public abstract class FileSystem : IRegisterable
	{
		/// <summary>
		/// Generates a random file name with the given length.
		/// </summary>
		/// <remarks>The generated file name is guaranteed not to exist.</remarks>
		/// <param name="info">The directory to generate the file name in. This
		/// parameter can be null to indicate merely a random file name</param>
		/// <param name="length">The length of the file name to generate.</param>
		/// <returns>A full path to a file containing random file name.</returns>
		public static string GenerateRandomFileName(DirectoryInfo info, int length)
		{
			//Get the PRNG we are going to use
			Prng prng = Host.Instance.Prngs.ActivePrng;

			//Initialsie the base name, if any.
			string resultPrefix = info == null ? string.Empty : info.FullName +
				Path.DirectorySeparatorChar;

			//Variables to store the intermediates.
			byte[] resultAry = new byte[length];
			string result = string.Empty;
			List<string> prohibitedFileNames = new List<string>(new string[] {
				"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
				"COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3",
				"LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
			});

			do
			{
				prng.NextBytes(resultAry);

				//Validate the name
				string validFileNameChars = "0123456789abcdefghijklmnopqrstuvwxyz" +
					"ABCDEFGHIJKLMNOPQRSTUVWXYZ _+=-()[]{}',`~!";
				for (int j = 0, k = resultAry.Length; j < k; ++j)
				{
					resultAry[j] = (byte)validFileNameChars[
						(int)resultAry[j] % validFileNameChars.Length];

					if (j == 0 || j == k - 1)
						//The first or last character cannot be whitespace
						while (Char.IsWhiteSpace((char)resultAry[j]))
							resultAry[j] = (byte)validFileNameChars[
								(int)resultAry[j] % validFileNameChars.Length];
				}

				result = Encoding.UTF8.GetString(resultAry);
			}
			while (info != null &&
				prohibitedFileNames.IndexOf(Path.GetFileNameWithoutExtension(result)) != -1 ||
				(Directory.Exists(resultPrefix + result) || File.Exists(resultPrefix + result)));
			return resultPrefix + result;
		}

		/// <summary>
		/// Gets a random file from within the provided directory.
		/// </summary>
		/// <param name="info">The directory to get a random file name from.</param>
		/// <returns>A string containing the full path to the file.</returns>
		public static string GetRandomFile(DirectoryInfo info)
		{
			//First retrieve the list of files and folders in the provided directory.
			FileSystemInfo[] entries = null;
			try
			{
				entries = info.GetFileSystemInfos();
			}
			catch (DirectoryNotFoundException)
			{
				return string.Empty;
			}
			if (entries.Length == 0)
				return string.Empty;

			//Find a random entry.
			Prng prng = Host.Instance.Prngs.ActivePrng;
			string result = string.Empty;
			while (result.Length == 0)
			{
				int index = prng.Next(entries.Length - 1);
				if (entries[index] is DirectoryInfo)
					result = GetRandomFile((DirectoryInfo)entries[index]);
				else
					result = ((FileInfo)entries[index]).FullName;
			}

			return result;
		}

		/// <summary>
		/// The Guid of the current filesystem.
		/// </summary>
		public abstract Guid Guid
		{
			get;
		}

		/// <summary>
		/// The name of the current filesystem.
		/// </summary>
		public abstract string Name
		{
			get;
		}

		/// <summary>
		/// Resets the created, modified, accessed and last update times for the given
		/// <paramref name="info"/>.
		/// </summary>
		/// <param name="info">The file to reset times.</param>
		public abstract void ResetFileTimes(FileSystemInfo info);

		/// <summary>
		/// Securely deletes the file reference from the directory structures
		/// as well as resetting the Date Created, Date Accessed and Date Modified
		/// records.
		/// </summary>
		/// <param name="info">The file to delete.</param>
		public abstract void DeleteFile(FileInfo info);

		/// <summary>
		/// Securely deletes the folder reference from the directory structures
		/// as well as all subfolders and files, resetting the Date Created, Date
		/// Accessed and Date Modified records.
		/// </summary>
		/// <param name="info">The folder to delete</param>
		/// <param name="recursive">True if the folder and all its subfolders and
		/// files to be securely deleted.</param>
		public abstract void DeleteFolder(DirectoryInfo info, bool recursive);

		/// <seealso cref="DeleteFolder"/>
		/// <param name="info">The folder to delete.</param>
		public void DeleteFolder(DirectoryInfo info)
		{
			DeleteFolder(info, true);
		}

		/// <summary>
		/// The function prototype for cluster tip search progress callbacks. This is
		/// called when the cluster tips are being searched.
		/// </summary>
		/// <param name="currentPath">The directory being searched</param>
		public delegate void ClusterTipsSearchProgress(string currentPath);

		/// <summary>
		/// The function prototype for cluster tip erasure callbacks. This is called when
		/// the cluster tips are being erased.
		/// </summary>
		/// <param name="currentFile">The current file index being erased.</param>
		/// <param name="totalFiles">The total number of files to be erased.</param>
		/// <param name="currentFilePath">The path to the current file being erased.</param>
		public delegate void ClusterTipsEraseProgress(int currentFile, int totalFiles,
			string currentFilePath);

		/// <summary>
		/// The prototype of callbacks handling the file system table erase progress.
		/// </summary>
		/// <param name="currentFile">The current file being erased.</param>
		/// <param name="totalFiles">The estimated number of files that must be
		/// erased.</param>
		public delegate void FileSystemEntriesEraseProgress(int currentFile, int totalFiles);

		/// <summary>
		/// Erases all file cluster tips in the given volume.
		/// </summary>
		/// <param name="info">The volume to search for file cluster tips and erase them.</param>
		/// <param name="method">The erasure method being employed.</param>
		/// <param name="log">The log manager instance that tracks log messages.</param>
		/// <param name="searchCallback">The callback function for search progress.</param>
		/// <param name="eraseCallback">The callback function for erasure progress.</param>
		public abstract void EraseClusterTips(VolumeInfo info, ErasureMethod method,
			ClusterTipsSearchProgress searchCallback, ClusterTipsEraseProgress eraseCallback);

		/// <summary>
		/// Erases old file system table-resident files. This creates small one-byte
		/// files until disk is full. This will erase unused space which was used for
		/// files resident in the file system table.
		/// </summary>
		/// <param name="volume">The directory information structure containing
		/// the path to store the temporary one-byte files. The file system table
		/// of that drive will be erased.</param>
		/// <param name="tempDirectory">The directory structure containing the path
		/// to store temporary files used for resident file cleaning.</param>
		/// <param name="method">The method used to erase the files.</param>
		public abstract void EraseOldFileSystemResidentFiles(VolumeInfo volume,
			DirectoryInfo tempDirectory, ErasureMethod method,
			FileSystemEntriesEraseProgress callback);

		/// <summary>
		/// Erases the unused space in the main filesystem structures by creating,
		/// files until the table grows.
		/// 
		/// This will overwrite unused portions of the table which were previously
		/// used to store file entries.
		/// </summary>
		/// <param name="info">The directory information structure containing
		/// the path to store the temporary files.</param>
		/// <param name="callback">The callback function to handle the progress
		/// of the file system entry erasure.</param>
		public abstract void EraseDirectoryStructures(VolumeInfo info,
			FileSystemEntriesEraseProgress callback);

		/// <summary>
		/// Erases the file system object from the drive.
		/// </summary>
		/// <param name="info"></param>
		public abstract void EraseFileSystemObject(StreamInfo info, ErasureMethod method,
			ErasureMethod.ErasureMethodProgressFunction callback);

		//TODO: This is supposed to be in VolumeInfo!
		/// <summary>
		/// Retrieves the size of the file on disk, calculated by the amount of
		/// clusters allocated by it.
		/// </summary>
		/// <param name="streamInfo">The Stream to get the area for.</param>
		/// <returns>The area of the file.</returns>
		public abstract long GetFileArea(StreamInfo filePath);

		/// <summary>
		/// The number of times file names are renamed to erase the file name from
		/// the file system table.
		/// </summary>
		public const int FileNameErasePasses = 7;

		/// <summary>
		/// The maximum number of times Eraser tries to erase a file/folder before
		/// it gives up.
		/// </summary>
		public const int FileNameEraseTries = 50;
	}
}