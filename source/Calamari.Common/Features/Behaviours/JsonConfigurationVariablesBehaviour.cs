﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Calamari.Common.Commands;
using Calamari.Common.Features.StructuredVariables;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Common.Plumbing.Logging;
using Calamari.Common.Plumbing.Pipeline;
using Calamari.Common.Plumbing.Variables;

namespace Calamari.Common.Features.Behaviours
{
    class JsonConfigurationVariablesBehaviour : IBehaviour
    {
        readonly IStructuredConfigVariableReplacer structuredConfigVariableReplacer;
        readonly ICalamariFileSystem fileSystem;
        readonly ILog log;

        public JsonConfigurationVariablesBehaviour(IStructuredConfigVariableReplacer structuredConfigVariableReplacer, ICalamariFileSystem fileSystem, ILog log)
        {
            this.structuredConfigVariableReplacer = structuredConfigVariableReplacer;
            this.fileSystem = fileSystem;
            this.log = log;
        }

        public bool IsEnabled(RunningDeployment context)
        {
            return context.Variables.GetFlag(KnownVariables.Package.JsonConfigurationVariablesEnabled);
        }

        public Task Execute(RunningDeployment context)
        {
            foreach (var target in context.Variables.GetPaths(KnownVariables.Package.JsonConfigurationVariablesTargets))
            {
                if (fileSystem.DirectoryExists(target))
                {
                    log.Warn($"Skipping JSON variable replacement on '{target}' because it is a directory.");
                    continue;
                }

                var matchingFiles = MatchingFiles(context, target);

                if (!matchingFiles.Any())
                {
                    log.Warn($"No files were found that match the replacement target pattern '{target}'");
                    continue;
                }

                foreach (var file in matchingFiles)
                {
                    log.Info($"Performing JSON variable replacement on '{file}'");
                    structuredConfigVariableReplacer.ModifyFile(file, context.Variables);
                }
            }

            return this.CompletedTask();
        }

        List<string> MatchingFiles(RunningDeployment deployment, string target)
        {
            var files = fileSystem.EnumerateFilesWithGlob(deployment.CurrentDirectory, target).Select(Path.GetFullPath).ToList();

            foreach (var path in deployment.Variables.GetStrings(ActionVariables.AdditionalPaths).Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var pathFiles = fileSystem.EnumerateFilesWithGlob(path, target).Select(Path.GetFullPath);
                files.AddRange(pathFiles);
            }

            return files;
        }
    }
}