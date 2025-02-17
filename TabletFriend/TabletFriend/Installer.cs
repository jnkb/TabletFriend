﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace TabletFriend
{
	public static class Installer
	{
		private static readonly string _preferredDirectory =
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TabletFriend");

		public static void TryInstall()
		{
			if (!AppState.Settings.FirstLaunch)
			{
				return;
			}
			if (AppState.CurrentDirectory.TrimEnd('\\') == _preferredDirectory.TrimEnd('\\'))
			{
				return;
			}
			var result = MessageBox.Show(
				"Welcome to Tablet Friend! It seems that you are running from a regular directory. "
				+ "Would you like Tablet Friend to move itself to AppData?",
				"Hi!",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question
			);
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					if (Directory.Exists(_preferredDirectory))
					{
						MessageBox.Show(
							"Another version of Tablet Friend detected. 'files' directory will be overwritten. " +
							"Previous version's layouts, themes and icons will be moved to 'files.backup'",
							"Update",
							MessageBoxButton.OK
						);
						if (Directory.Exists(Path.Combine(_preferredDirectory, "files")))
						{
							DirectoryCopy(
								Path.Combine(_preferredDirectory, "files"),
								Path.Combine(_preferredDirectory, "files.backup")
							);
						}
					}
					DirectoryCopy(AppState.CurrentDirectory, _preferredDirectory, "*.dll");
					DirectoryCopy(AppState.CurrentDirectory, _preferredDirectory, "*.exe");
					DirectoryCopy(AppState.CurrentDirectory, _preferredDirectory, "*.json");
					Process.Start(Path.Combine(_preferredDirectory, "TabletFriend.exe"));
					Application.Current.Shutdown();
				}
				catch (Exception e)
				{
					MessageBox.Show(
						"Failed to copy the files: '" + e.Message + "'. Make sure all other instances of Tablet Friend are closed and try again.",
						"Error!",
						MessageBoxButton.OK,
						MessageBoxImage.Error
					);
					Environment.Exit(0);
				}
			}
		}


		private static void DirectoryCopy(string sourceDirName, string destDirName, string pattern = "*.*")
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();

			// If the destination directory doesn't exist, create it.       
			Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles(pattern);
			foreach (FileInfo file in files)
			{
				string tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, true);
			}


			foreach (DirectoryInfo subdir in dirs)
			{
				string tempPath = Path.Combine(destDirName, subdir.Name);
				DirectoryCopy(subdir.FullName, tempPath);
			}
		}
	}
}
