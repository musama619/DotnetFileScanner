using System.ComponentModel;

namespace Models
{
    public enum FileScanResult
    {
        NoThreatFound,
        ThreatFound,
        FileNotFound,
        Timeout,
        Error
    }
}
