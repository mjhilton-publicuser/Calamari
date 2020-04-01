﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Amazon.CloudFormation;
using Calamari.Aws.Deployment;
using Calamari.Aws.Deployment.Conventions;
using Calamari.Aws.Integration;
using Calamari.Aws.Integration.CloudFormation;
using Calamari.Aws.Util;
using Calamari.Commands.Support;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;
using Calamari.Integration.Processes;

namespace Calamari.Aws.Commands
{
    [Command("delete-aws-cloudformation", Description = "Destroy an existing AWS CloudFormation stack")]
    public class DeleteCloudFormationCommand : Command
    {
        readonly IVariables variables;
        readonly ILog log;
        private string packageFile;
        private bool waitForComplete;
        
        public DeleteCloudFormationCommand(IVariables variables, ILog log)
        {
            this.variables = variables;
            this.log = log;
            Options.Add("package=", "Path to the NuGet package to install.", v => packageFile = Path.GetFullPath(v));
            Options.Add("waitForCompletion=", "True if the deployment process should wait for the stack to complete, and False otherwise.", v => waitForComplete =  
                !bool.FalseString.Equals(v, StringComparison.OrdinalIgnoreCase)); //True by default
        }

        public override int Execute(string[] commandLineArguments)
        {
            Options.Parse(commandLineArguments);

            var environment = AwsEnvironmentGeneration.Create(variables, log).GetAwaiter().GetResult();;
            var stackEventLogger = new StackEventLogger(new LogWrapper());
         
            
            IAmazonCloudFormation ClientFactory () => ClientHelpers.CreateCloudFormationClient(environment);
            StackArn StackProvider (RunningDeployment x) => new StackArn( x.Variables.Get(AwsSpecialVariables.CloudFormation.StackName));
            
            var conventions = new List<IConvention>
            {
                new LogAwsUserInfoConvention(environment),
                new ContributeEnvironmentVariablesConvention(),
                new LogVariablesConvention(),
                new DeleteCloudFormationStackConvention(environment, stackEventLogger, ClientFactory, StackProvider, waitForComplete)
            };
            
            var deployment = new RunningDeployment(packageFile, variables);
            var conventionRunner = new ConventionProcessor(deployment, conventions);
            
            conventionRunner.RunConventions();
            return 0;
        }
    }
}