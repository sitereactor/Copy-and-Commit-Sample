using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using System.Text;
using System.Threading.Tasks;

namespace CopyCommitTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourcePath = ConfigurationManager.AppSettings["SourcePath"];
            var destination = ConfigurationManager.AppSettings["DestinationPath"];

            var destinations = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var destinationPath = Path.Combine(destination, Guid.NewGuid().ToString("N"));
                if (Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                destinations.Add(destinationPath);
            }

            Console.WriteLine(DateTime.Now.ToString("T"));

            Parallel.ForEach(destinations, destinationPath =>
                                           {
                                               CopyBaselineToRepository(sourcePath, destinationPath);
                                               CommitBaselineToRepository(destinationPath);
                                           });

            Console.WriteLine(DateTime.Now.ToString("T"));
        }

        public static bool CopyBaselineToRepository(string sourcePath, string destinationPath)
        {
            if (!string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("Copying all files from the baseline {0} to the site repository {1}", sourcePath, destinationPath);
                //Now Create all of the directories
                var directories = Directory.GetDirectories(sourcePath, "*.*", SearchOption.AllDirectories);
                foreach (var dirPath in directories)
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                //Copy all the files
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                foreach (var newPath in files)
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath));
                }
                return true;
            }

            return false;
        }

        public static bool CommitBaselineToRepository(string repositoryPath)
        {
            try
            {
                Console.WriteLine("Committing all files to git repository {0}", repositoryPath);
                var init = Repository.Init(repositoryPath);
                using (var repo = new Repository(repositoryPath))
                {
                    var files = Directory.GetFiles(repositoryPath, "*.*", SearchOption.AllDirectories);
                    var relativeFilePaths = files.Select(x => x.Replace(repositoryPath, "").Replace("\\", "/").TrimStart('/'));
                    repo.Stage(relativeFilePaths, new StageOptions { IncludeIgnored = true });
                    repo.Commit("Initial commit",
                        new Signature("John Doe", "john@example.com", new DateTimeOffset(DateTime.UtcNow)));
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("CommitBaselineToRepository throw an exception for {0}, environment {1}", repositoryPath, e.Message);
                return false;
            }
        }
    }
}
