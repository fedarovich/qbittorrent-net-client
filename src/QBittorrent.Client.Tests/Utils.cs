using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace QBittorrent.Client.Tests
{
    public static class Utils
    {
        public static string StartupFolder => Path.GetDirectoryName(
            Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName);

        public static string TorrentsFolder => Path.Combine(StartupFolder, "torrents");

        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static async Task Retry(Func<Task> action, int attempts = 5, int delayMs = 3000)
        {
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch
                {
                    if (--attempts <= 0)
                        throw;
                    await Task.Delay(delayMs);
                }
            }
        }
        
        public static async Task<T> Retry<T>(Func<Task<T>> func, int attempts = 5, int delayMs = 3000)
        {
            while (true)
            {
                try
                {
                    return await func();
                }
                catch
                {
                    if (--attempts <= 0)
                        throw;
                    await Task.Delay(delayMs);
                }
            }
        }

        public static async Task<(T1, T2)> WhenAll<T1, T2>(
            Task<T1> task1, Task<T2> task2)
        {
            await Task.WhenAll(task1, task2).ConfigureAwait(false);
            return (task1.Result, task2.Result);
        }
        
        public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3)
        {
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return (task1.Result, task2.Result, task3.Result);
        }
        
        public static async Task<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(
            Task<T1> task1, Task<T2> task2, Task<T3> task3, Task<T4> task4)
        {
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
            return (task1.Result, task2.Result, task3.Result, task4.Result);
        }
        
        public static void CreateTarGz(string tgzFilename, string sourceDirectory)
        {
            var currDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = sourceDirectory;

            try
            {
                Stream outStream = File.Create(tgzFilename);
                Stream gzoStream = new GZipOutputStream(outStream);
                TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

                // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                // and must not end with a slash, otherwise cuts off first char of filename
                // This is scheduled for fix in next release
                tarArchive.RootPath = sourceDirectory;

                AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);

                tarArchive.Close();
            }
            finally
            {
                Environment.CurrentDirectory = currDir;
            }
        }

        private static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {

            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            //
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            //
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }
    }
}
