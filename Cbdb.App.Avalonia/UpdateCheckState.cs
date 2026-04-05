namespace Cbdb.App.Avalonia;

internal sealed class UpdateCheckState {
    private readonly GitHubReleaseUpdateChecker _updateChecker;

    public UpdateCheckState(GitHubReleaseUpdateChecker? updateChecker = null) {
        _updateChecker = updateChecker ?? new GitHubReleaseUpdateChecker();
    }

    public event EventHandler? Changed;

    public bool IsChecking { get; private set; }

    public UpdateCheckResult? LastResult { get; private set; }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default) {
        if (IsChecking) {
            return LastResult ?? UpdateCheckResult.Checking(AppVersionInfo.GetDisplayVersion());
        }

        IsChecking = true;
        OnChanged();

        try {
            LastResult = await _updateChecker.CheckForUpdatesAsync(cancellationToken);
            return LastResult;
        } catch (Exception ex) {
            LastResult = UpdateCheckResult.Failed(AppVersionInfo.GetDisplayVersion(), ex.Message);
            return LastResult;
        } finally {
            IsChecking = false;
            OnChanged();
        }
    }

    private void OnChanged() {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
