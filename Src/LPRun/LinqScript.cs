﻿using System.IO;

// ReSharper disable UnusedType.Global
// ReSharper disable once UnusedMember.Global

namespace LPRun
{
    public static class LinqScript
    {
        public static string Create(string file, string connection)
        {
            Directory.CreateDirectory(Context.FilesDir);

            file = Context.GetTemplatesFullPath(file);

            var outFile = Context.GetFilesFullPath(Path.GetFileName(file));

            using var textWriter = File.CreateText(outFile);

            textWriter.Write(connection);
            textWriter.Write(File.ReadAllText(file));

            return outFile;
        }
    }
}
