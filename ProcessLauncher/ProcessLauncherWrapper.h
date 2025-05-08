#pragma once

namespace WTLayoutManager::Services{
    /// <summary>
    /// Provides methods for launching processes with custom environment configurations.
    /// </summary>
    public ref class ProcessLauncher
    {
    public:
        /// <summary>
        /// Launches a process with a custom environment block.
        /// Returns the exit code of the process.
        /// Throws an exception if the process could not be started.
        /// </summary>
        static int LaunchProcess(System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock, System::String^ hookPath);

        /// <summary>
        /// Launches an elevated process via a launcher executable.
        /// The launcher (with a UAC manifest) starts the target process using the provided encoded environment block.
        /// Returns the exit code of the target process.
        /// Throws an exception if the launcher could not be started.
        /// </summary>
        static int LaunchProcessElevated(System::String^ launcherPath, System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock, System::String^ hookPath);
    };

    /// <summary>
    /// A dummy namespace and class for placeholder or testing purposes.
    /// </summary>
    inline namespace DummyNamespace {
        public ref class DummyClass {
        };
    }
}
