﻿using System;
using System.IO;
using System.Windows.Forms;

namespace WolvenKit
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0)
                if (File.Exists(args[0]))
                    switch (Path.GetExtension(args[0]))
                    {
                        case ".w3modproj":
                            {
                                MainController.Get().InitialModProject = args[0];
                                break;
                            }
                        case ".wkp":
                            {
                                MainController.Get().InitialWKP = args[0];
                                break;
                            }
                    }

            Application.Run(MainController.Get().Window);
        }

    }
}