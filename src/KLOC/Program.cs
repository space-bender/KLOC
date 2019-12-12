﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KLOC
{
    internal class Program
    {
        private static void WriteHead()
        {
            Console.WriteLine("Kay-LOC");
            Console.WriteLine("<? Kilo Lines Of Code.");
            Console.WriteLine();
        }
        private static void Usage(string message = null)
        {
            WriteHead();
            if (message != null)
                Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("KLOC.exe <path> [-l|k[p]] [-c]");
            Console.WriteLine("<path>: Location of source code directory (required)");
            Console.WriteLine("        Displays Kay-LOC and statistics of the source files in depth.");
            Console.WriteLine("-k:     Displays only Kay-LOC.");
            Console.WriteLine("-l:     Displays only source line count.");
            Console.WriteLine("-kp:    Displays only Kay-LOC and path.");
            Console.WriteLine("-lp:    Displays only source line count and path.");
            Console.WriteLine("-c:     Enumerate and count sub-directories and displays a name-count pairs in a table.");
        }

        public static void Main(string[] args)
        {
            var arguments = Arguments.Parse(args);
            if (arguments == null)
            {
                Usage("Invalid arguments.");
                return;
            }
            if (arguments.IsHelp)
            {
                Usage();
                return;
            }

            var timer = Stopwatch.StartNew();
            Run(arguments);
            if (arguments.WriteDeatails)
            {
                Console.WriteLine();
                Console.WriteLine("Processing time: " + timer.Elapsed);
            }

            if (Debugger.IsAttached)
            {
                Console.Write("-------- Press any key to exit --------");
                if (!System.Reflection.Assembly.GetExecutingAssembly().Location.Contains(@"\KlocTests\"))
                    Console.ReadKey();
            }
        }
        public static void Run(Arguments arguments)
        {

            if (!Directory.Exists(arguments.ProjectDirectory))
            {
                Usage("Location of source code directory does not exist.");
                return;
            }

            if (arguments.IsContainer)
            {
                ProcessContainer(arguments);
                return;
            }

            var ctx = new CounterContext();
            var sourceFileEnumerable = new ProjectDirectory(arguments.ProjectDirectory, ctx);
            var sourceFiles = sourceFileEnumerable.ToArray();
            Counter.CountOfLines(sourceFiles, ctx);

            var result1 = "PATH:    " + arguments.ProjectDirectory;
            var result2 = $"Kay-LOC: {ctx.Lines / 1000:n0}";

            WriteHead();
            Console.WriteLine(result1);
            Console.WriteLine(result2);
            Console.WriteLine(new string('=', Math.Max(result1.Length, result2.Length)));
            Console.WriteLine();
            Console.WriteLine("DETAILS");
            Console.WriteLine("-------");
            Console.WriteLine();
            //Console.WriteLine("Projects:       {0,15:n0}", ctx.Projects);
            Console.WriteLine("Source files:   {0,15:n0}", sourceFiles.Length);
            Console.WriteLine("Bytes length:   {0,15:n0}", ctx.Bytes);
            Console.WriteLine("Longest line:   {0,15:n0}", ctx.LongestLine);
            Console.WriteLine("Count of lines: {0,15:n0}", ctx.Lines);
            Console.WriteLine("Empty lines:    {0,15:n0}", ctx.EmptyLines);
            Console.WriteLine();
            Console.WriteLine("File types:");
            var sorted = ctx.FileTypes.OrderByDescending(x => x.Value);
            foreach (var item in sorted)
                Console.WriteLine("{0,16}{1,15:n0}", item.Key, item.Value);
        }

        private static void ProcessContainer(Arguments arguments)
        {
            var mainCtx = new CounterContext();
            var mainProjectDirectory = new ProjectDirectory(arguments.ProjectDirectory, mainCtx);
            var subDirectories = mainProjectDirectory.GetDirectories();

            var colWidth = subDirectories.Max(x => Path.GetFileName(x).Length) + 2;
            var line = $"{new string('-', colWidth)} -------------";
            WriteHead();
            Console.WriteLine("CONTAINER: " + arguments.ProjectDirectory);
            Console.WriteLine();
            Console.WriteLine($"{"NAME".PadRight(colWidth)} Lines Of Code");
            Console.WriteLine(line);
            var sum = 0;

            foreach (var subDirectory in subDirectories)
            {
                Console.Write($"{Path.GetFileName(subDirectory).PadRight(colWidth)} ");

                var ctx = new CounterContext();
                var sourceFileEnumerable = new ProjectDirectory(subDirectory, ctx);
                var sourceFiles = sourceFileEnumerable.ToArray();
                Counter.CountOfLines(sourceFiles, ctx);

                Console.WriteLine($"{ctx.Lines,13:n0}");
                sum += ctx.Lines;
            }

            Console.WriteLine(line);
            Console.WriteLine($"{"SUMMARY".PadRight(colWidth)} {sum,13:n0}");
        }
    }
}