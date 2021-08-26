﻿using System;
using System.Collections.Generic;
using Calamari.Common.Plumbing.Deployment.PackageRetention;

namespace Calamari.Deployment.PackageRetention.Model
{
    public class PackageLocks
    {
        readonly Dictionary<ServerTaskID, DateTime> packageLocks;

        internal PackageLocks(Dictionary<ServerTaskID, DateTime> packageLocks)
        {
            this.packageLocks = packageLocks ?? new Dictionary<ServerTaskID, DateTime>();
        }

        public PackageLocks() : this(new Dictionary<ServerTaskID, DateTime>())
        {
        }

        public void AddLock(ServerTaskID deploymentID)
        {
            if (packageLocks.ContainsKey(deploymentID))
                packageLocks[deploymentID] = DateTime.Now;
            else
                packageLocks.Add(deploymentID, DateTime.Now);
        }

        public void RemoveLock(ServerTaskID deploymentID)
        {
            packageLocks.Remove(deploymentID);
        }

        public bool HasLock() => packageLocks.Count > 0;
    }
}