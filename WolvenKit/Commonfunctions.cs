﻿using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace WolvenKit
{
    static class Commonfunctions
    {
        /// <summary>
        /// Send a popup notification.
        /// </summary>
        /// <param name="msg">The string to display in the notification.</param>
        public static void SendNotification(string msg)
        {
            Version win8version = new Version(6, 2, 9200, 0);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= win8version)
            {
                // its win8 or higher so we can use toas notifications TODO: Add actual rich toast notifications
                using (var ni = new NotifyIcon())
                {
                    ni.Icon = SystemIcons.Information;
                    ni.Visible = true;
                    ni.ShowBalloonTip(3000, "WolvenKit", msg, ToolTipIcon.Info);
                }

            }
            else
            {
                using (var ni = new NotifyIcon())
                {
                    ni.Icon = SystemIcons.Information;
                    ni.Visible = true;
                    ni.ShowBalloonTip(3000, "WolvenKit", msg, ToolTipIcon.Info);
                }
            }
        }

        /// <summary>
        /// Show a messagebox that the feature is work in progress.
        /// </summary>
        public static void ShowWIPMessage()
        {
            MessageBox.Show("Work in progress.", "Coming soon(tm)", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// Show the given path in the windows explorer.
        /// </summary>
        /// <param name="path">The file/folder to show.</param>
        public static void ShowFileInExplorer(string path)
        {
            Process.Start("explorer.exe", "/select, \"" + path + "\"");
        }

        /// <summary>
        /// Copies the contents of a directory.
        /// </summary>
        /// <param name="sourceDirName">The source directory.</param>
        /// <param name="destDirName">The destination.</param>
        /// <param name="copySubDirs">Whether to copy subdirectories.</param>
        /// <returns>A log of copied files.</returns>
        public static List<XElement> DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            List<XElement> ret = new List<XElement>();
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                ret.Add(new XElement("file", temppath));
                file.CopyTo(temppath, true);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = Path.Combine(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    if (Directory.GetFiles(subdir.FullName, "*", SearchOption.AllDirectories).Any())
                        ret.Add(new XElement("Directory", new XAttribute("Path", temppath), DirectoryCopy(subdir.FullName, temppath, copySubDirs)));
                }
            }
            return ret;
        }

        /// <summary>
        /// Compresses a file into a zipstream.
        /// </summary>
        /// <param name="filename">Path to the file.</param>
        /// <param name="zipStream">The zipstream to output to.</param>
        /// <param name="nameOverride">Rename the file to a costum name.</param>
        public static void CompressFile(string filename, ZipOutputStream zipStream, string nameOverride = "")
        {
            FileInfo fi = new FileInfo(filename);

            string entryName = Path.GetFileName(filename);
            if (nameOverride != string.Empty)
                entryName = nameOverride;
            entryName = ZipEntry.CleanName(entryName);
            ZipEntry newEntry = new ZipEntry(entryName) { DateTime = fi.LastWriteTime, Size = fi.Length };
            zipStream.PutNextEntry(newEntry);
            byte[] buffer = new byte[4096];
            using (FileStream streamReader = File.OpenRead(filename))
            {
                StreamUtils.Copy(streamReader, zipStream, buffer);
            }
            zipStream.CloseEntry();
        }

        /// <summary>
        /// Converts an XDocuments to a byte array.
        /// </summary>
        /// <param name="xdoc">The xdocument which we want to convert.</param>
        /// <returns>The byte contents of the array.</returns>
        public static byte[] XDocToByteArray(XDocument xdoc)
        {
            MemoryStream ms = new MemoryStream();
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = true;

            using (XmlWriter xw = XmlWriter.Create(ms, xws))
            {
                xdoc.WriteTo(xw);
            }
            return ms.ToArray();
        }

        /// <summary>
        /// Compresses a byte array to a zipstream.
        /// </summary>
        /// <param name="file">The byte array to compress.</param>
        /// <param name="filename">The entry name.</param>
        /// <param name="zipStream">The zipstream which we want to output this file to.</param>
        public static void CompressStream(byte[] file, string filename, ZipOutputStream zipStream)
        {
            filename = ZipEntry.CleanName(filename);
            ZipEntry newEntry = new ZipEntry(filename) { DateTime = DateTime.Now, Size = file.Length };
            zipStream.PutNextEntry(newEntry);
            byte[] buffer = new byte[4096];
            StreamUtils.Copy(new MemoryStream(file), zipStream, buffer);
            zipStream.CloseEntry();
        }

        /// <summary>
        /// Compresses a folder of files into a zipstream.
        /// </summary>
        /// <param name="path">The path of the folder.</param>
        /// <param name="zipStream">The output zipstream.</param>
        /// <param name="folderOffset">The folderoffset.</param>
        public static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {

            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {
                FileInfo fi = new FileInfo(filename);
                string entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                ZipEntry newEntry = new ZipEntry(entryName) { DateTime = fi.LastWriteTime, Size = fi.Length };
                zipStream.PutNextEntry(newEntry);
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }

        /// <summary>
        /// Deletes files and folders in given folder.
        /// </summary>
        /// <param name="target_dir">Targed directory.</param>
        public static void DeleteFilesAndFoldersRecursively(string target_dir)
        {
            foreach (string file in Directory.EnumerateFiles(target_dir))
            {
                File.Delete(file);
            }

            foreach (string subDir in Directory.GetDirectories(target_dir))
            {
                DeleteFilesAndFoldersRecursively(subDir);
            }

            Thread.Sleep(1); // This makes the difference between whether it works or not. Sleep(0) is not enough.
            Directory.Delete(target_dir);
        }

        /// <summary>
        /// Gets relative path from absolute path.
        /// </summary>
        /// <param name="filespec">A files path.</param>
        /// <param name="folder">The folder's path.</param>
        /// <returns></returns>
        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Moves a directory's contents to anothet directory
        /// </summary>
        /// <param name="SourcePath">The old dir (will be deleted)</param>
        /// <param name="DestinationPath">The new dir (will be created)</param>
        public static void DirectoryMove(string SourcePath, string DestinationPath)
        {
            var oi = new DirectoryInfo(SourcePath);
            var newdir = Path.Combine(oi.Parent.FullName, oi.Name + "_old");
            oi.MoveTo(newdir);
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(newdir, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(newdir, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(newdir, "*.*",
                SearchOption.AllDirectories))
            {
                File.Move(newPath, newPath.Replace(newdir, DestinationPath));
            }

            //Delete the old directory
            DeleteDirectory(newdir);
        }

        /// <summary>
        /// Deletes a non empty directory
        /// </summary>
        /// <param name="targetDir">The directory to delete.</param>
        public static void DeleteDirectory(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }
    }
}
