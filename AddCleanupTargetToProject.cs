public void AddCleanupTargetToProject(List<string> unusedDependencies)
        {
            if (unusedDependencies.Count == 0)
            {
                Console.WriteLine("No unused dependencies to remove.");
                return;
            }

            var projectContent = File.ReadAllText(_projectPath);
            
            // Check if cleanup target already exists
            if (projectContent.Contains("<Target Name=\"RemoveUnusedDependencies\""))
            {
                Console.WriteLine("Cleanup target already exists. Removing existing target...");
                projectContent = RemoveExistingCleanupTarget(projectContent);
            }

            var cleanupTarget = GenerateCleanupTarget(unusedDependencies);
            
            // Insert the target before the closing </Project> tag
            var insertIndex = projectContent.LastIndexOf("</Project>");
            if (insertIndex == -1)
            {
                throw new InvalidOperationException("Invalid project file format - missing </Project> tag");
            }

            var modifiedContent = projectContent.Insert(insertIndex, cleanupTarget + Environment.NewLine + Environment.NewLine);
            
            // Create backup
            var backupPath = _projectPath + ".backup";
            File.Copy(_projectPath, backupPath, true);
            Console.WriteLine($"Project backup created: {backupPath}");
            
            // Write modified project file
            File.WriteAllText(_projectPath, modifiedContent);
            Console.WriteLine($"Cleanup target added to project file: {_projectPath}");
        }

        private string RemoveExistingCleanupTarget(string projectContent)
        {
            var startTag = "<Target Name=\"RemoveUnusedDependencies\"";
            var endTag = "</Target>";
            
            var startIndex = projectContent.IndexOf(startTag);
            if (startIndex == -1) return projectContent;
            
            var endIndex = projectContent.IndexOf(endTag, startIndex) + endTag.Length;
            if (endIndex < endTag.Length) return projectContent;
            
            // Remove the entire target block including surrounding whitespace
            var beforeTarget = projectContent.Substring(0, startIndex).TrimEnd();
            var afterTarget = projectContent.Substring(endIndex).TrimStart();
            
            return beforeTarget + Environment.NewLine + afterTarget;
        }

        private string GenerateCleanupTarget(List<string> unusedDependencies)
        {
            var target = new List<string>
            {
                "  <!-- Auto-generated target to remove unused dependencies -->",
                "  <Target Name=\"RemoveUnusedDependencies\" AfterTargets=\"Build\">",
                "    <Message Text=\"Removing unused dependency files from output directory...\" Importance=\"high\" />",
                "    "
            };

            // Group all files to delete into ItemGroups for better organization
            var filesToDelete = new List<string>();
            
            foreach (var dependency in unusedDependencies)
            {
                filesToDelete.Add($"$(OutputPath){dependency}.dll");
                filesToDelete.Add($"$(OutputPath){dependency}.pdb");
                filesToDelete.Add($"$(OutputPath){dependency}.xml");
                filesToDelete.Add($"$(OutputPath){dependency}.deps.json");
                
                // Also handle common transitive dependency patterns
                filesToDelete.Add($"$(OutputPath){dependency}.*.dll");
                filesToDelete.Add($"$(OutputPath){dependency}.*.pdb");
            }

            // Create ItemGroup for files to delete
            target.Add("    <ItemGroup>");
            target.Add("      <UnusedDependencyFiles Include=\"" + string.Join(";", filesToDelete) + "\" />");
            target.Add("    </ItemGroup>");
            target.Add("");

            // Add the Delete task
            target.Add("    <Delete Files=\"@(UnusedDependencyFiles)\" ContinueOnError=\"true\" />");
            target.Add("");

            // Add informational messages
            target.Add("    <Message Text=\"Removed unused dependencies:\" Importance=\"high\" />");
            foreach (var dependency in unusedDependencies)
            {
                target.Add($"    <Message Text=\"  - {dependency}\" Importance=\"high\" />");
            }
            
            target.Add("  </Target>");

            return string.Join(Environment.NewLine, target);
        }
