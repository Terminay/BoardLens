namespace RigSpec.Core.Interfaces;

public interface IHardwareService
{
    string PlatformName { get; }

    Task<HardwareSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
