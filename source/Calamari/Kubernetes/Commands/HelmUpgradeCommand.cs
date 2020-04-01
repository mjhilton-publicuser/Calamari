﻿using System;
using System.Collections.Generic;
using System.IO;
using Calamari.Commands.Support;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;
using Calamari.Deployment.Journal;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Packages;
using Calamari.Integration.Processes;
using Calamari.Integration.Scripting;
using Calamari.Integration.ServiceMessages;
using Calamari.Integration.Substitutions;
using Calamari.Kubernetes.Conventions;

namespace Calamari.Kubernetes.Commands
{
    [Command("helm-upgrade", Description = "Performs Helm Upgrade with Chart while performing variable replacement")]
    public class HelmUpgradeCommand : Command
    {
        private string packageFile;
        private readonly CombinedScriptEngine scriptEngine;
        private readonly IDeploymentJournalWriter deploymentJournalWriter;
        readonly IVariables variables;
        readonly ICalamariFileSystem fileSystem;

        public HelmUpgradeCommand(CombinedScriptEngine scriptEngine, IDeploymentJournalWriter deploymentJournalWriter, IVariables variables, ICalamariFileSystem fileSystem)
        {
            Options.Add("package=", "Path to the NuGet package to install.", v => packageFile = Path.GetFullPath(v));
            this.scriptEngine = scriptEngine;
            this.deploymentJournalWriter = deploymentJournalWriter;
            this.variables = variables;
            this.fileSystem = fileSystem;
        }
        
        public override int Execute(string[] commandLineArguments)
        {
              Options.Parse(commandLineArguments);

            if (!File.Exists(packageFile))
                throw new CommandException("Could not find package file: " + packageFile);
            var commandLineRunner = new CommandLineRunner(new SplitCommandOutput(new ConsoleCommandOutput(), new ServiceMessageCommandOutput(variables)));
            var substituter = new FileSubstituter(fileSystem);
            var extractor = new GenericPackageExtractorFactory().createStandardGenericPackageExtractor();
            ValidateRequiredVariables();
            
            var conventions = new List<IConvention>
            {
                new ContributeEnvironmentVariablesConvention(),
                new LogVariablesConvention(),
                new ExtractPackageToStagingDirectoryConvention(extractor, fileSystem),
                new StageScriptPackagesConvention(null, fileSystem, extractor, true),
                new ConfiguredScriptConvention(DeploymentStages.PreDeploy, fileSystem, scriptEngine, commandLineRunner),
                new SubstituteInFilesConvention(fileSystem, substituter, _ => true, FileTargetFactory),
                new ConfiguredScriptConvention(DeploymentStages.Deploy, fileSystem, scriptEngine, commandLineRunner),
                new HelmUpgradeConvention(scriptEngine, commandLineRunner, fileSystem),
                new ConfiguredScriptConvention(DeploymentStages.PostDeploy, fileSystem, scriptEngine, commandLineRunner),
            };
            var deployment = new RunningDeployment(packageFile, variables);
            var conventionRunner = new ConventionProcessor(deployment, conventions);
            
            try
            {
                conventionRunner.RunConventions();
                deploymentJournalWriter.AddJournalEntry(deployment, true, packageFile);
            }
            catch (Exception)
            {
                deploymentJournalWriter.AddJournalEntry(deployment, false, packageFile);
                throw;
            }

            return 0;
        }

        private void ValidateRequiredVariables()
        {
            if (!variables.IsSet(SpecialVariables.ClusterUrl))
            {
                throw new CommandException($"The variable `{SpecialVariables.ClusterUrl}` is not provided.");
            }
        }
        
        private IEnumerable<string> FileTargetFactory(RunningDeployment deployment)
        {
            var variables = deployment.Variables;
            var packageReferenceNames = variables.GetIndexes(Deployment.SpecialVariables.Packages.PackageCollection);
            foreach (var packageReferenceName in packageReferenceNames)
            {
                var sanitizedPackageReferenceName = fileSystem.RemoveInvalidFileNameChars(packageReferenceName);
                var paths = variables.GetPaths(SpecialVariables.Helm.Packages.ValuesFilePath(packageReferenceName));
                
                foreach (var path in paths)
                {
                    yield return Path.Combine(sanitizedPackageReferenceName, path);    
                }
            }
        }
    }
}