namespace ProjectK._3PDB.Standalone.BL.Interfaces
{
    /// <summary>
    /// Runtime control surface for the mocked update service, used by test/E2E tooling
    /// to drive the "old version -> update -> new version -> changelog" flow deterministically.
    /// Only registered when mock-update mode is enabled.
    /// </summary>
    public interface IMockUpdateControl
    {
        /// <summary>Sets the version the app reports as currently running.</summary>
        void SetCurrentVersion(string version);

        /// <summary>Sets the version advertised as an available update (null disables the banner).</summary>
        void SetAvailableNewVersion(string? version);

        /// <summary>Sets the changelog/release notes returned for a given version.</summary>
        void SetReleaseNotes(string version, string notes);

        /// <summary>
        /// One-shot: sets notes for <paramref name="toVersion"/> (if provided), marks it as the
        /// current running version and clears any pending update — i.e. the update has been applied.
        /// </summary>
        void SimulateUpdate(string toVersion, string? notes);

        /// <summary>Returns the current mock state (for assertions/debugging).</summary>
        MockUpdateState GetState();
    }

    public record MockUpdateState(string CurrentVersion, string? AvailableNewVersion, IReadOnlyList<string> KnownReleaseNoteVersions);
}
