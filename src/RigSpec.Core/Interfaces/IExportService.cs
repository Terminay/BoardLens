namespace RigSpec.Core.Interfaces;

public interface IExportService
{
    string ToJson(HardwareSnapshot snapshot);

    string ToText(HardwareSnapshot snapshot);
}
