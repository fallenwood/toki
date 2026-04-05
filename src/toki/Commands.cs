namespace Toki;

using System.Diagnostics;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

/// <summary>
/// Toki static site generator commands
/// </summary>
public class Commands {
  /// <summary>
  /// Build the static site
  /// </summary>
  /// <param name="root">-r, Root directory of the site (defaults to current directory)</param>
  [Command("build")]
  public void Build(
    string root = "") {
    var rootPath = string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root;

    using var loggerFactory = LoggerFactory.Create(builder => {
      builder.AddConsole();
    });
    var logger = loggerFactory.CreateLogger("toki");

    logger.LogInformation("Toki starts from root: {Root}", rootPath);

    var manager = new TokiManager(rootPath, logger, loggerFactory);
    manager.Build();
  }

  /// <summary>
  /// Start preview server with file watching
  /// </summary>
  /// <param name="root">-r, Root directory of the site (defaults to current directory)</param>
  /// <param name="port">-p, Port number for the preview server (default: 5000)</param>
  [Command("preview")]
  public async Task Preview(
    string root = "",
    int port = 5000) {
    var rootPath = string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root;

    using var loggerFactory = LoggerFactory.Create(builder => {
      builder.AddConsole();
    });
    var logger = loggerFactory.CreateLogger("toki");

    logger.LogInformation("Toki starts from root: {Root}", rootPath);

    var manager = new TokiManager(rootPath, logger, loggerFactory);

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => {
      e.Cancel = true;
      cts.Cancel();
    };

    try {
      await manager.Watch(port, cts.Token);
    } catch (OperationCanceledException) {
      // Graceful exit
    }
  }

  /// <summary>
  /// Deploy the generated site to a git remote
  /// </summary>
  /// <param name="root">-r, Root directory of the site (defaults to current directory)</param>
  /// <param name="message">-msg, Commit message (default: "Deploy site")</param>
  [Command("deploy")]
  public void Deploy(
    string root = "",
    string message = "Deploy site") {
    var rootPath = string.IsNullOrEmpty(root) ? Directory.GetCurrentDirectory() : root;
    var publicDir = Path.Combine(rootPath, "public");
    var configPath = Path.Combine(rootPath, "site.toml");

    using var loggerFactory = LoggerFactory.Create(builder => {
      builder.AddConsole();
    });
    var logger = loggerFactory.CreateLogger("toki");

    // Load config to get remote and branch
    var siteConfig = Config.LoadSiteConfig(configPath, logger);
    var deployRemote = siteConfig.Deploy.Remote;
    var deployBranch = siteConfig.Deploy.Branch;
    var deployRepo = siteConfig.Deploy.Repo;

    if (!Directory.Exists(publicDir)) {
      logger.LogError("Public directory does not exist: {PublicDir}. Run 'toki build' first.", publicDir);
      return;
    }

    logger.LogInformation("Deploying site from {PublicDir} to {Remote}/{Branch}", publicDir, deployRemote, deployBranch);

    try {
      // Initialize git repo in public directory if not exists
      if (!Directory.Exists(Path.Combine(publicDir, ".git"))) {
        logger.LogInformation("Initializing git repository in {PublicDir}", publicDir);
        RunGitCommand(publicDir, "init", logger);
        RunGitCommand(publicDir, "branch -M " + deployBranch, logger);

        // Set up remote if repo URL is provided
        if (!string.IsNullOrWhiteSpace(deployRepo)) {
          logger.LogInformation("Adding remote {Remote} with URL {Repo}", deployRemote, deployRepo);
          RunGitCommand(publicDir, $"remote add {deployRemote} {deployRepo}", logger);
        }
      } else {
        // If repo is specified and remote exists, ensure it points to the correct URL
        if (!string.IsNullOrWhiteSpace(deployRepo)) {
          try {
            var currentUrl = RunGitCommand(publicDir, $"remote get-url {deployRemote}", logger).Trim();
            if (currentUrl != deployRepo) {
              logger.LogInformation("Updating remote {Remote} URL to {Repo}", deployRemote, deployRepo);
              RunGitCommand(publicDir, $"remote set-url {deployRemote} {deployRepo}", logger);
            }
          } catch {
            // Remote doesn't exist, add it
            logger.LogInformation("Adding remote {Remote} with URL {Repo}", deployRemote, deployRepo);
            RunGitCommand(publicDir, $"remote add {deployRemote} {deployRepo}", logger);
          }
        }
      }

      // Add all files
      RunGitCommand(publicDir, "add -A", logger);

      // Check if there are changes to commit
      var status = RunGitCommand(publicDir, "status --porcelain", logger);
      if (string.IsNullOrWhiteSpace(status)) {
        logger.LogInformation("No changes to deploy");
        return;
      }

      // Commit changes
      RunGitCommand(publicDir, $"commit -m \"{message}\"", logger);

      // Push to remote
      logger.LogInformation("Pushing to {Remote}/{Branch}", deployRemote, deployBranch);
      RunGitCommand(publicDir, $"push {deployRemote} {deployBranch} --force", logger);

      logger.LogInformation("Deployment complete!");
    } catch (Exception ex) {
      logger.LogError(ex, "Deployment failed");
    }
  }

  private static string RunGitCommand(string workingDirectory, string arguments, ILogger logger) {
    var psi = new ProcessStartInfo {
      FileName = "git",
      Arguments = arguments,
      WorkingDirectory = workingDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process == null) {
      throw new InvalidOperationException("Failed to start git process");
    }

    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0) {
      logger.LogError("Git command failed: git {Arguments}\n{Error}", arguments, error);
      throw new InvalidOperationException($"Git command failed with exit code {process.ExitCode}");
    }

    if (!string.IsNullOrWhiteSpace(output)) {
      logger.LogDebug("Git output: {Output}", output.TrimEnd());
    }

    return output;
  }
}
