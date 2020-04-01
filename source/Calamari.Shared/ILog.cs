using Octopus.Versioning;

namespace Calamari
{
    public interface ILog
    {
        void Verbose(string message);
        void VerboseFormat(string messageFormat, params object[] args);
        void Info(string message);
        void InfoFormat(string messageFormat, params object[] args);
        void Warn(string message);
        void WarnFormat(string messageFormat, params object[] args);
        void Error(string message);
        void ErrorFormat(string messageFormat, params object[] args);
        void SetOutputVariableButDoNotAddToVariables(string name, string value, bool isSensitive = false);
        void SetOutputVariable(string name, string value, IVariables variables, bool isSensitive = false);
        void NewOctopusArtifact(string fullPath, string name, long fileLength);

        void PackageFound(string packageId, IVersion packageVersion, string packageHash,
            string packageFileExtension, string packageFullPath, bool exactMatchExists = false);

        void Progress(int percentage, string message);
        void DeltaVerification(string remotePath, string hash, long size);
        void DeltaVerificationError(string error);
        string FormatLink(string uri, string description = null);
    }
}