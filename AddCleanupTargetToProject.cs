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
/* FInal update */



        public void AddCleanupTargetToProject(List<string> unusedDependencies)
        {
            var projectContent = File.ReadAllText(_projectPath);
            
            // Get currently used dependencies to compare against existing removal list
            var usedDependencies = GetActuallyUsedAssemblies().Result;
            
            List<string> existingRemovalList = new List<string>();
            List<string> finalRemovalList;
            
            // Check if cleanup target already exists
            if (projectContent.Contains("<Target Name=\"RemoveUnusedDependencies\""))
            {
                Console.WriteLine("Existing cleanup target found. Analyzing changes...");
                existingRemovalList = ExtractExistingRemovalList(projectContent);
                
                Console.WriteLine($"Existing removal list has {existingRemovalList.Count} dependencies");
                foreach (var dep in existingRemovalList)
                {
                    Console.WriteLine($"  Currently marked for removal: {dep}");
                }
            }
            
            // Compare and update the removal list
            finalRemovalList = UpdateRemovalList(unusedDependencies, usedDependencies, existingRemovalList);
            
            if (finalRemovalList.Count == 0)
            {
                Console.WriteLine("No dependencies need to be removed. Removing cleanup target if it exists.");
                if (existingRemovalList.Count > 0)
                {
                    var contentWithoutTarget = RemoveExistingCleanupTarget(projectContent);
                    File.WriteAllText(_projectPath, contentWithoutTarget);
                    Console.WriteLine("Cleanup target removed from project file.");
                }
                return;
            }

            // Remove existing target if present
            if (existingRemovalList.Count > 0)
            {
                projectContent = RemoveExistingCleanupTarget(projectContent);
            }

            var cleanupTarget = GenerateCleanupTarget(finalRemovalList);
            
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
            Console.WriteLine($"Cleanup target updated in project file: {_projectPath}");
        }

        private List<string> ExtractExistingRemovalList(string projectContent)
        {
            var removalList = new List<string>();
            
            try
            {
                // Find the target block
                var targetStart = projectContent.IndexOf("<Target Name=\"RemoveUnusedDependencies\"");
                var targetEnd = projectContent.IndexOf("</Target>", targetStart) + "</Target>".Length;
                
                if (targetStart == -1 || targetEnd < "</Target>".Length)
                {
                    return removalList;
                }
                
                var targetContent = projectContent.Substring(targetStart, targetEnd - targetStart);
                
                // Extract dependencies from Message elements
                // Look for patterns like: <Message Text="  - DependencyName" Importance="high" />
                var messagePattern = @"<Message\s+Text=""\s*-\s*([^""]+)""\s+Importance=""high""\s*/>";
                var matches = Regex.Matches(targetContent, messagePattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    var dependencyName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(dependencyName))
                    {
                        removalList.Add(dependencyName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not parse existing removal list: {ex.Message}");
            }
            
            return removalList;
        }

        private List<string> UpdateRemovalList(List<string> unusedDependencies, List<string> usedDependencies, List<string> existingRemovalList)
        {
            var finalList = new List<string>(existingRemovalList);
            
            // Step 1: Remove dependencies that are now being used (were marked for removal but are now used)
            var dependenciesToRestore = new List<string>();
            foreach (var existingDep in existingRemovalList)
            {
                // Check if this dependency is now being used
                if (usedDependencies.Any(used => used.Contains(existingDep) || existingDep.Contains(used)))
                {
                    dependenciesToRestore.Add(existingDep);
                    finalList.Remove(existingDep);
                }
            }
            
            // Step 2: Add new unused dependencies that aren't already in the removal list
            var dependenciesToAdd = new List<string>();
            foreach (var unusedDep in unusedDependencies)
            {
                if (!finalList.Contains(unusedDep))
                {
                    dependenciesToAdd.Add(unusedDep);
                    finalList.Add(unusedDep);
                }
            }
            
            // Display changes
            if (dependenciesToRestore.Count > 0)
            {
                Console.WriteLine($"\n✓ Dependencies restored (now being used):");
                foreach (var dep in dependenciesToRestore)
                {
                    Console.WriteLine($"  + {dep}");
                }
            }
            
            if (dependenciesToAdd.Count > 0)
            {
                Console.WriteLine($"\n✓ New unused dependencies found:");
                foreach (var dep in dependenciesToAdd)
                {
                    Console.WriteLine($"  - {dep}");
                }
            }
            
            if (dependenciesToRestore.Count == 0 && dependenciesToAdd.Count == 0)
            {
                Console.WriteLine("\n→ No changes needed in removal list.");
            }
            
            Console.WriteLine($"\nFinal removal list: {finalList.Count} dependencies");
            foreach (var dep in finalList)
            {
                Console.WriteLine($"  - {dep}");
            }
            
            return finalList;
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
