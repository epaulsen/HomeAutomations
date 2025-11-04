using System.Reactive.Subjects;
using HomeAutomations.Extensions;
using HomeAutomations.Models;

namespace HomeAutomations.Hosts;

public class UnifiData
{
    private List<ClientDevice> _currentDevices = new();
    private Subject<List<ClientDevice>> _clientDevices { get; } = new();

    public IObservable<List<ClientDevice>> ClientDevices => _clientDevices;

    public void SetCurrent(List<ClientDevice> devices)
    {
        if (_currentDevices.IsIdenticalTo(devices))
        {
            return;
        }

        _currentDevices = devices;
        _clientDevices.OnNext(_currentDevices);
    }
}