using LibNexus.Core;
using LibNexus.Files;
using Mono.Options;
using System;
using System.Linq;

public class Program
{
	public static List<(string, string)> inputFiles = new();
	public static string outputPath = "";
	public static string originalIndexFile = "";
	public static string currentBasePath = "";

	static void Main(string[] args)
	{
		ParseArgs(args);

		CompileIndexFile();
	}

	public static void ParseArgs(string[] args)
	{
		var p = new OptionSet()
		{
			{ "outputfile=", "the output index file", v => outputPath = v },
			{ "basepath=", "the current base path for input files", v => currentBasePath = v },
			//{ "inputpath=", "a path to add to the index", v => inputFiles.AddRange(Directory.GetFiles(v, "*.*", SearchOption.AllDirectories)) },
			{ "inputxml=", "an xml file containing the input files", v => ReadInputXML(v) },
			{ "indexfile=", "the original index file to modify", v => originalIndexFile = v }
		};

		p.Parse(args);

		bool exit = false;
		if (!inputFiles.Any())
		{
			Console.WriteLine("No input files found!");
			exit = true;
		}
		if (string.IsNullOrEmpty(outputPath))
		{
			Console.WriteLine("Output file not specified!");
			exit = true;
		}
		if (exit) return;

		CompileIndexFile();
	}

	public static void CompileIndexFile()
	{
		if (originalIndexFile != null)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
			File.Copy(originalIndexFile, outputPath, true);
		}
		else
		{
			if (!File.Exists(outputPath)) return;
		}
		FileSystem fs = FileSystem.Create(new Progress(), outputPath.Replace(".index", ""), false, null, CancellationToken.None).Result;
		foreach(var file in inputFiles)
		{
			fs.Write(file.Item1, File.ReadAllBytes(file.Item2), File.GetLastWriteTimeUtc(file.Item2));
		}
		fs.Dispose();
		fs.Close(); // these don't do exactly the same thing.
	}

	public static void ReadInputXML(string xmlFile)
	{
		IndexToolXML xml = IndexToolXML.Read(xmlFile);
		foreach(var dir in xml.folders)
		{
			foreach(var file in dir.files)
			{
				inputFiles.Add((file.filePath, Path.Combine(dir.basePath, file.filePath)));
			}
		}
	}
}

public class ArchiveFilePair
{
	FileSystem fs = null;
	Task<FileSystem> task = null;
	Progress progress;

	public ArchiveFilePair(string index, bool hasArchive, CancellationToken token)
	{
		progress = new Progress();
		task = Task.Run(() => FileSystem.Create(progress, index, hasArchive, null, token));
	}

	public FileSystem GetFileSystem()
	{
		if (fs == null && task != null)
		{
			task.Wait();
			fs = task.Result;
			task = null;
		}
		return fs;
	}

	public bool IsReady()
	{
		return task == null || task.IsCompleted;
	}
}