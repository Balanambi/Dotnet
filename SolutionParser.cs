public class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public Guid ProjectGuid { get; set; }
    }

    public class SolutionParser
    {
        public static List<ProjectInfo> GetProjectsFromSolution(string solutionPath)
        {
            if (!File.Exists(solutionPath))
            {
                throw new FileNotFoundException($"Solution file not found: {solutionPath}");
            }

            var projects = new List<ProjectInfo>();
            var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
            var solutionContent = File.ReadAllText(solutionPath);

            // Regex pattern to match C# project entries in .sln file
            // Format: Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectName", "RelativePath\ProjectName.csproj", "{PROJECT-GUID}"
            var projectPattern = @"Project\(""\{[A-F0-9\-]+\}""\)\s*=\s*""([^""]+)"",\s*""([^""]+)"",\s*""\{([A-F0-9\-]+)\}""";
            var matches = Regex.Matches(solutionContent, projectPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                var projectName = match.Groups[1].Value;
                var relativePath = match.Groups[2].Value;
                var projectGuidString = match.Groups[3].Value;

                // Only include .csproj files (skip solution folders and other project types)
                if (!relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Convert relative path to absolute path
                var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativePath));

                // Verify the project file exists
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Warning: Project file not found: {fullPath}");
                    continue;
                }

                if (Guid.TryParse(projectGuidString, out var projectGuid))
                {
                    projects.Add(new ProjectInfo
                    {
                        Name = projectName,
                        RelativePath = relativePath,
                        FullPath = fullPath,
                        ProjectGuid = projectGuid
                    });
                }
            }

            return projects;
        }

      
        public static void DisplayProjects(List<ProjectInfo> projects)
        {
            Console.WriteLine($"\nFound {projects.Count} C# projects in solution:");
            Console.WriteLine(new string('=', 50));
            
            for (int i = 0; i < projects.Count; i++)
            {
                var project = projects[i];
                Console.WriteLine($"{i + 1}. {project.Name}");
                Console.WriteLine($"   Path: {project.RelativePath}");
                Console.WriteLine($"   Full Path: {project.FullPath}");
                Console.WriteLine();
            }
        }
    }


var inputPath = args[0];
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"File not found: {inputPath}");
                return;
            }

            var fileExtension = Path.GetExtension(inputPath).ToLowerInvariant();

            if (fileExtension == ".sln")
            {
                await AnalyzeSolution(inputPath);
            }
            else if (fileExtension == ".csproj")
            {
                await AnalyzeSingleProject(inputPath);
            }
            else
            {
                Console.WriteLine("Unsupported file type. Please provide a .sln or .csproj file.");
            }


 static async Task AnalyzeSolution(string solutionPath)
        {
            Console.WriteLine($"Analyzing solution: {Path.GetFileName(solutionPath)}");
            Console.WriteLine(new string('=', 60));

            try
            {
                // Parse solution file to get all projects
                var projects = SolutionParser.GetProjectsFromSolution(solutionPath);
                
                if (projects.Count == 0)
                {
                    Console.WriteLine("No C# projects found in the solution.");
                    return;
                }

                // Display found projects
                SolutionParser.DisplayProjects(projects);

                // Analyze all projects
                var analysisResults = await UnusedDependencyAnalyzer.AnalyzeMultipleProjects(projects);

                // Display summary
                Console.WriteLine("=== Analysis Summary ===");
                var totalUnusedDeps = analysisResults.Values.SelectMany(deps => deps).Count();
                Console.WriteLine($"Total unused dependencies found: {totalUnusedDeps}");
                
                if (totalUnusedDeps > 0)
                {
                    Console.WriteLine("\nDo you want to add cleanup targets to all projects? (y/n): ");
                    var response = Console.ReadLine();
                    
                    if (response?.ToLowerInvariant() == "y" || response?.ToLowerInvariant() == "yes")
                    {
                        UnusedDependencyAnalyzer.AddCleanupTargetsToMultipleProjects(analysisResults);
                        Console.WriteLine("✓ Cleanup targets added to all applicable projects!");
                        Console.WriteLine("\nNote: Project backup files (.csproj.backup) have been created.");
                    }
                }
                else
                {
                    Console.WriteLine("No cleanup needed - all dependencies appear to be in use!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing solution: {ex.Message}");
            }
        }

        static async Task AnalyzeSingleProject(string projectPath)
        {
            Console.WriteLine($"Analyzing single project: {Path.GetFileName(projectPath)}");
            Console.WriteLine(new string('=', 60));

            var analyzer = new UnusedDependencyAnalyzer(projectPath);
            
            try
            {
                Console.WriteLine("Analyzing dependencies...");
                var unusedDeps = await analyzer.AnalyzeUnusedDependencies();
                
                Console.WriteLine("\n=== Analysis Results ===");
                if (unusedDeps.Count == 0)
                {
                    Console.WriteLine("No unused dependencies found!");
                }
                else
                {
                    Console.WriteLine("Unused dependencies found:");
                    foreach (var dep in unusedDeps)
                    {
                        Console.WriteLine($"  - {dep}");
                    }
                    
                    Console.WriteLine("\nDo you want to add cleanup target to the project? (y/n): ");
                    var response = Console.ReadLine();
                    
                    if (response?.ToLowerInvariant() == "y" || response?.ToLowerInvariant() == "yes")
                    {
                        analyzer.AddCleanupTargetToProject(unusedDeps);
                        Console.WriteLine("✓ Cleanup target added to project!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing project: {ex.Message}");
            }
        }


 public static void AddCleanupTargetsToMultipleProjects(Dictionary<string, List<string>> analysisResults)
        {
            Console.WriteLine("Adding cleanup targets to projects...\n");
            
            foreach (var result in analysisResults)
            {
                var projectPath = result.Key;
                var unusedDeps = result.Value;
                var projectName = Path.GetFileNameWithoutExtension(projectPath);
                
                Console.WriteLine($"Processing project: {projectName}");
                
                try
                {
                    var analyzer = new UnusedDependencyAnalyzer(projectPath);
                    analyzer.AddCleanupTargetToProject(unusedDeps);
                    Console.WriteLine($"  ✓ Cleanup target added successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error adding cleanup target: {ex.Message}");
                }
                Console.WriteLine();
            }
        }
