using System.Collections.Generic;

namespace Dan200.Core.Computer
{
    public interface IDevicePort
    {
        IEnumerable<Device> Devices { get; }
    }
}
