using LivestreamViewer.Constants;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LivestreamViewer
{
    /// <summary>
    /// Represents the current state of livestream viewer instances on the current computer.
    /// </summary>
    internal class LivestreamViewerState
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        public List<Process> ViewerInstances { get; set; }
        public List<Process> PlayerInstances { get; set; }
        public List<Process> ProcessorInstances { get; set; }

        /// <summary>
        /// Status of livestream viewer instances on the current computer, aggregated by an inspection of currently-running processes.
        /// </summary>
        public LivestreamViewerStatus Status
        {
            get
            {
                // The environment contains a "NotRunning" instance if there is no instance running and none of the players are running.
                if ((ViewerInstances == null || !ViewerInstances.Any()) && (PlayerInstances == null || !PlayerInstances.Any()) && (ProcessorInstances == null || !ProcessorInstances.Any()))
                {
                    return LivestreamViewerStatus.NotRunning;
                }
                // The environment contains a "Healthy" instance if there is one instance running, one of the players is running, and no more than one instance of a video processor is running.
                // TODO: Could we use some kind of tag on the player process to know it is associated with the viewer?
                if(ViewerInstances != null && ViewerInstances.Count == 1 && PlayerInstances != null && PlayerInstances.Count == 1 && (ProcessorInstances == null || ProcessorInstances.Count < 2))
                {
                    return LivestreamViewerStatus.Healthy;
                }
                return LivestreamViewerStatus.Unhealthy;
            }
        }

        /// <summary>
        /// Prepares the environment for a new instance of this application by
        /// terminating anything associated with a previous instance.
        /// </summary>
        public void CleanEnvironment()
        {
            Log.Info("Cleaning environment for a new viewer instance.");

            // Terminate other instances of this app.
            foreach(var viewerProcess in ViewerInstances ?? new List<Process>())
            {
                Log.Info($"Stopping viewer instance (PID: {viewerProcess.Id})");
                viewerProcess.Kill();
            }
            // Terminate other video players.
            foreach (var playerProcess in PlayerInstances ?? new List<Process>())
            {
                Log.Info($"Stopping player instance (PID: {playerProcess.Id})");
                playerProcess.Kill();
            }
            // Terminate other video processes.
            foreach (var processorProcess in ProcessorInstances ?? new List<Process>())
            {
                Log.Info($"Stopping processor instance (PID: {processorProcess.Id})");
                processorProcess.Kill();
            }
        }

        /// <summary>
        /// Constructs a new LivestreamViewerState instance from the current list of processes.
        /// </summary>
        public static LivestreamViewerState FromEnvironment(VideoCommandResolver playerResolver)
        {
            Log.Info("Loading livestream viewer state.");

            var environmentInfo = new LivestreamViewerState
            {
                ViewerInstances = new List<Process>(),
                PlayerInstances = new List<Process>(),
                ProcessorInstances = new List<Process>()
            };

            // Get instances of this application and any player applications.
            var currentProcess = Process.GetCurrentProcess();
            var playerNames = playerResolver.GetKnownVisibleVideoPlayers();
            var processorNames = playerResolver.GetKnownVideoProcessors();
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals(currentProcess.ProcessName, StringComparison.OrdinalIgnoreCase) && process.Id != currentProcess.Id)
                {
                    environmentInfo.ViewerInstances.Add(process);
                }
                else
                {
                    foreach (var playerName in playerNames)
                    {
                        if (process.ProcessName.Contains(playerName, StringComparison.OrdinalIgnoreCase))
                        {
                            environmentInfo.PlayerInstances.Add(process);
                            continue;
                        }
                    }
                    foreach(var processorName in processorNames)
                    {
                        if (process.ProcessName.Contains(processorName, StringComparison.OrdinalIgnoreCase))
                        {
                            environmentInfo.ProcessorInstances.Add(process);
                            continue;
                        }
                    }
                }
            }

            return environmentInfo;
        }
    }
}
