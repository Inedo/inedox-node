using System.Text;
using System.Text.RegularExpressions;
using Inedo.Agents;
using Inedo.ExecutionEngine.Executer;
using Inedo.Extensions.PackageSources;
using Inedo.IO;
using Inedo.ProGet;
using Inedo.Web;

namespace Inedo.Extension.Node.Operations;

public abstract class NpmOperation : ExecuteOperation
{
    [ScriptAlias("SourceDirectory")]
    [DisplayName("Source directory")]
    [PlaceholderText("$WorkingDirectory")]
    public string? SourceDirectory { get; set; }

    [ScriptAlias("PackageSource")]
    [DisplayName("Package source")]
    [SuggestableValue(typeof(NpmPackageSourceSuggestionProvider))]
    [Description("If specified, this npm package source will be used to restore packages when building.  This will generate a local .npmrc file with a connection to this feed.")]
    public string? PackageSource { get; set; }

    [Category("Advanced")]
    [ScriptAlias("Scopes")]
    [FieldEditMode(FieldEditMode.Multiline)]
    [Description("All scopes you would like to wire to your ProGet npm feed.  One scope per line.")]
    public IEnumerable<string>? Scopes { get; set; }

    [Category("Advanced")]
    [ScriptAlias("Verbose")]
    [Description("When true, additional information about staging the script is written to the debug log.")]
    public bool Verbose { get; set; }

    [Category("Advanced")]
    [ScriptAlias("SuccessExitCode")]
    [DisplayName("Success exit code")]
    [Description("Integer exit code which indicates no error. When not specified, the exit code is ignored. This can also be an integer prefixed with an inequality operator.")]
    [Example("SuccessExitCode: 0 # Fail on nonzero.")]
    [Example("SuccessExitCode: >= 0 # Fail on negative numbers.")]
    [DefaultValue("ignored")]
    public string? SuccessExitCode { get; set; }

    [Category("Advanced")]
    [ScriptAlias("NpmPath")]
    [DefaultValue("$NpmPath")]
    [DisplayName("npm path")]
    [PlaceholderText("default")]
    [Description("Full path to npm/npm.cmd on the target server.")]
    public string? NpmPath { get; set; }

    [Category("Advanced")]
    [ScriptAlias("NpmrcPath")]
    [DisplayName(".npmrc path")]
    [PlaceholderText("default")]
    [Description("Override the path to your .npmrc file.  This will be ignored when using a ProGet feed.")]
    public string? NpmrcPath { get; set; }

    [Category("Advanced")]
    [ScriptAlias("AllowSelfSignedCertificate")]
    [DisplayName("Allow Self-Signed Certificate")]
    [Description("Self-signed certificates and internal Certificate Authority (CA) generated certificates are considered invalid by default in npm's certificate validation process.  Enabling this option will by-pass certificate validation in npm.")]
    public bool AllowSelfSignedCertificate { get; set; }

    private static readonly LazyRegex LogMessageRegex = new(@"npm\s(?<1>[a-zA-Z]+!?)(?<2>\s.*)?$");

    protected async Task<int?> ExecuteNpmAsync(string command, string? commandArgs, IOperationExecutionContext context)
    {
        this.LogInformation($"Executing npm {command}");
        var npmPath = await this.GetNpmExePathAsync(context);

        var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>();
        var sourceDirectory = context.ResolvePath(this.SourceDirectory ?? context.WorkingDirectory);

        await fileOps.CreateDirectoryAsync(sourceDirectory);
        var execProcess = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>();
        var npmrcPath = await getNpmrcPathAsync();
        if (this.Verbose)
        {
            commandArgs += " --loglevel verbose";
            this.LogDebug($"Executing {npmPath} {command} {commandArgs} {npmrcPath}");
        }
        using var process = execProcess.CreateProcess(
            new RemoteProcessStartInfo
            {
                FileName = npmPath,
                Arguments = $"{command} {commandArgs} {npmrcPath}",
                WorkingDirectory = sourceDirectory,
                UseUTF8ForStandardOutput = true,
                UseUTF8ForStandardError = true
            }
        );

        process.OutputDataReceived += (s, e) => this.LogDebug(e.Data);
        process.ErrorDataReceived += (s, e) =>
        {
            var m = LogMessageRegex.Match(e.Data);
            if (m.Success)
            {
                var level = m.Groups[1].Value.ToLower() switch
                {
                    "debug" => MessageLevel.Debug,
                    "info" => MessageLevel.Information,
                    "warning" or "warn" => MessageLevel.Warning,
                    "err!" or "error" or "critical" => MessageLevel.Error,
                    _ => MessageLevel.Debug
                };
                if(this.Verbose)
                    this.Log(level, e.Data);
                else
                    this.Log(level, m.Groups[2].Value);
            }
            else if (e.Data != null)
                this.Log(MessageLevel.Debug, e.Data);
        };

        await process.StartAsync(context.CancellationToken);

        await process.WaitAsync(context.CancellationToken);

        var exitCode = process.ExitCode.GetValueOrDefault();

        bool exitCodeLogged = false;

        if (!string.IsNullOrWhiteSpace(this.SuccessExitCode))
        {
            var comparator = ExitCodeComparator.TryParse(this.SuccessExitCode);
            if (comparator != null)
            {
                if (comparator.Evaluate(exitCode))
                    this.LogInformation($"Script exited with code: {exitCode} (success)");
                else
                    this.LogError($"Script exited with code: {exitCode} (failure)");

                exitCodeLogged = true;
            }
        }
        if (!exitCodeLogged)
            this.LogDebug("Script exited with code: " + exitCode);
        
        return exitCode;

        async Task<INpmPackageSource?> getPackageSourceAsync()
        {
            var sourceId = new PackageSourceId(this.PackageSource);
            if (sourceId.Format == PackageSourceIdFormat.Url)
                return new NpmUrlPackageSource(sourceId);
            var source = await AhPackages.GetPackageSourceAsync(sourceId, context, context.CancellationToken);
            if (source == null)
            {
                this.LogError($"Package source \"{this.PackageSource}\" not found.");
                return null;
            }
            if (source is not INpmPackageSource nnnnnnpm)
            {
                this.LogError($"Package source \"{this.PackageSource}\" is a {source.GetType().Name} source; it must be a npm source for use with this operation.");
                return null;
            }
            return nnnnnnpm;
        }

        async Task<string> getNpmVersionAsync()
        {
            var version = string.Empty;
            using var process = execProcess.CreateProcess(
                new RemoteProcessStartInfo
                {
                    FileName = npmPath,
                    Arguments = "--version",
                    WorkingDirectory = sourceDirectory,
                    UseUTF8ForStandardOutput = true,
                    UseUTF8ForStandardError = true,

                }
            );
            process.OutputDataReceived += (s, e) => version += e.Data ?? string.Empty;
            process.ErrorDataReceived += (s, e) => version += e.Data ?? string.Empty;

            await process.StartAsync(context.CancellationToken);
            await process.WaitAsync(context.CancellationToken);

            return version.Trim();
        }

        async Task createNpmrcV8Async(string npmrcPath, INpmPackageSource nnnnnnpm)
        {
            var normalizedRegistry = Regex.Replace(nnnnnnpm.RegistryUrl, @"^https?://", "//");
            var builder = new StringBuilder();
            if (this.AllowSelfSignedCertificate)
                builder.AppendLine("strict-ssl=false");

            builder.AppendLine($"registry={nnnnnnpm.RegistryUrl}");
            if ((this.Scopes?.Count() ?? 0) > 0)
            {
                foreach (var scope in this.Scopes!)
                    builder.AppendLine($"{scope}:registry={nnnnnnpm.RegistryUrl}");
            }
            if (!string.IsNullOrWhiteSpace(nnnnnnpm.UserName) || !string.IsNullOrWhiteSpace(nnnnnnpm.ApiKey))
            {
                builder.AppendLine("always-auth=true");
                if (string.IsNullOrWhiteSpace(nnnnnnpm.UserName))
                {
                    builder.AppendLine($"{normalizedRegistry}:username=api");
                    builder.AppendLine($"{normalizedRegistry}:_password=\"{Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes(nnnnnnpm.ApiKey!))}\"");
                }
                else
                {
                    builder.AppendLine($"{normalizedRegistry}:username={nnnnnnpm.UserName}");
                    builder.AppendLine($"{normalizedRegistry}:_password=\"{Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes(nnnnnnpm.Password!))}\"");
                }
            }
            await fileOps.WriteAllTextAsync(npmrcPath, builder.ToString(), InedoLib.UTF8Encoding);
        }

        async Task createNpmrcV9Async(string npmrcPath, INpmPackageSource nnnnnnpm)
        {
            var normalizedRegistry = Regex.Replace(nnnnnnpm.RegistryUrl, @"^https?://", "//");
            var builder = new StringBuilder();
            if (this.AllowSelfSignedCertificate)
                builder.AppendLine("strict-ssl=false");

            builder.AppendLine($"registry={nnnnnnpm.RegistryUrl}");
            if ((this.Scopes?.Count() ?? 0) > 0)
            {
                foreach (var scope in this.Scopes!)
                    builder.AppendLine($"{scope}:registry={nnnnnnpm.RegistryUrl}");
            }
            if (!string.IsNullOrWhiteSpace(nnnnnnpm.UserName) || !string.IsNullOrWhiteSpace(nnnnnnpm.ApiKey))
            {
                builder.AppendLine("always-auth=true");
                if (string.IsNullOrWhiteSpace(nnnnnnpm.UserName))
                    builder.AppendLine($"{normalizedRegistry}:_auth=\"{Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes($"api:{nnnnnnpm.ApiKey}"))}\"");
                else
                    builder.AppendLine($"{normalizedRegistry}:_auth=\"{Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes($"{nnnnnnpm.UserName}:{nnnnnnpm.Password!}"))}\"");
            }
            await fileOps.WriteAllTextAsync(npmrcPath, builder.ToString(), InedoLib.UTF8Encoding);
        }

        async Task<string> getNpmrcPathAsync()
        {
            if (!string.IsNullOrWhiteSpace(this.PackageSource))
            {
                this.LogDebug("Creating .npmrc....");
                var nnnnnnpm = await getPackageSourceAsync();
                if (nnnnnnpm == null)
                    return string.Empty;

                var version = await getNpmVersionAsync();
                
                // ExtensionVersion is basically a copy SemVer2, using this to parse the npm version to see if it should use the new auth style
                var useV9 = ExtensionVersion.TryParse(version)?.Major >= 9;

                if (this.Verbose)
                    this.LogDebug($"Using npm {version}");

                var npmrcPath = PathEx.Combine(sourceDirectory, ".npmrc");

                if (fileOps.FileExists(npmrcPath))
                    await fileOps.DeleteFileAsync(npmrcPath);

                if (useV9)
                    await createNpmrcV9Async(npmrcPath, nnnnnnpm);
                else
                    await createNpmrcV8Async(npmrcPath, nnnnnnpm);

                this.LogDebug("Created.");
                return $"--userconfig=\"{npmrcPath}\"";
            }
            else if (!string.IsNullOrWhiteSpace(this.NpmrcPath))
                return $"--userconfig=\"{this.NpmrcPath}\"";

            return string.Empty;
        }
    }

    private class NpmUrlPackageSource : INpmPackageSource
    {
        public string? UserName => null;

        public string? Password => null;
        public string? ApiKey => null;

        public string RegistryUrl { get; set; }

        public PackageSourceId SourceId { get; set; }

        public NpmUrlPackageSource(PackageSourceId sourceId)
        {
            this.SourceId = sourceId;
            this.RegistryUrl = this.SourceId.GetUrl();
        }

        public string? GetViewPackageUrl(string packageName, string packageVersion) => null;
    }

    private async Task<string> GetNpmExePathAsync(IOperationExecutionContext context)
    {
        string? foundPath = null;
        if (string.IsNullOrWhiteSpace(this.NpmPath))
        {
            this.LogDebug("NpmPath is not defined; searching for npm...");

            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>();
            if (fileOps.DirectorySeparator == '/')
            {
                if (await fileOps.FileExistsAsync("/usr/lib/npm"))
                    foundPath = "/usr/lib/npm";
                else if (await fileOps.FileExistsAsync("/usr/lib/node_modules/npm"))
                    foundPath = "/usr/lib/node_modules/npm";
            }
            else
            {
                var rubbish = await context.Agent.GetServiceAsync<IRemoteProcessExecuter>();

                string appDataPath = await rubbish.GetEnvironmentVariableValueAsync("AppData") ?? string.Empty;
                string programFilesPath = await rubbish.GetEnvironmentVariableValueAsync("ProgramFiles") ?? string.Empty;
                var searchPaths = new[]
                {
                    PathEx.Combine(appDataPath, "npm"),
                    PathEx.Combine(appDataPath, "npm\\node_modules"),
                    PathEx.Combine(appDataPath, "npm\\node_modules\\npm\\bin"),
                    PathEx.Combine(programFilesPath, "nodejs"),
                    PathEx.Combine(programFilesPath, "nodejs\\node_modules"),
                    PathEx.Combine(programFilesPath, "nodejs\\node_modules\\npm\\bin")
                };
                foreach (var searchPath in searchPaths)
                {
                    var path = PathEx.Combine(searchPath, "npm.cmd");
                    if (await fileOps.FileExistsAsync(path))
                    {
                        foundPath = path;
                        break;
                    }
                }
            }
        }
        else
        {
            foundPath = context.ResolvePath(this.NpmPath);
        }

        if (foundPath == null)
            throw new ExecutionFailureException("Could not find npm and $NpmPath configuration variable is not set.");
        this.LogDebug("Using npm at: " + foundPath);

        return foundPath;
    }

    private sealed class ExitCodeComparator
    {
        private static readonly string[] ValidOperators = new[] { "=", "==", "!=", "<", ">", "<=", ">=" };

        private ExitCodeComparator(string op, int value)
        {
            this.Operator = op;
            this.Value = value;
        }

        public string Operator { get; }
        public int Value { get; }

        public static ExitCodeComparator? TryParse(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            var match = Regex.Match(s, @"^\s*(?<1>[=<>!])*\s*(?<2>[0-9]+)\s*$", RegexOptions.ExplicitCapture);
            if (!match.Success)
                return null;

            var op = match.Groups[1].Value;
            if (string.IsNullOrEmpty(op) || !ValidOperators.Contains(op))
                op = "==";

            return new ExitCodeComparator(op, int.Parse(match.Groups[2].Value));
        }

        public bool Evaluate(int exitCode) => this.Operator switch
        {
            "=" or "==" => exitCode == this.Value,
            "!=" => exitCode != this.Value,
            "<" => exitCode < this.Value,
            ">" => exitCode > this.Value,
            "<=" => exitCode <= this.Value,
            ">=" => exitCode >= this.Value,
            _ => false
        };
    }
}
