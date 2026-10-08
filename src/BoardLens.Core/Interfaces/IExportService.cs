namespace BoardLens.Core.Interfaces;

public interface IExportService
{
    string ToJson(HardwareSnapshot snapshot);

    string ToText(HardwareSnapshot snapshot);
}
