// TaskManagerViewModel.cs
namespace S7Tools.ViewModels;

using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Profiles;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;

public sealed class TaskManagerViewModel : ViewModelBase {
    private readonly IJobScheduler _scheduler;
    private readonly IUIThreadService _uiThreadService;
    public ObservableCollection<Job> Jobs { get; } = new();
    public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
    public TaskManagerViewModel(IJobScheduler scheduler, IUIThreadService uiThreadService) {
        _scheduler = scheduler;
        _uiThreadService = uiThreadService;
        _scheduler.JobStateChanged += (id, state, msg) => {
            _uiThreadService.PostToUIThread(() => {
                var job = Jobs.FirstOrDefault(j => j.Id == id);
                if (job != null)
                {
                    var index = Jobs.IndexOf(job);
                    Jobs[index] = job with { State = state };
                }
            });
        };
        CreateJobCommand = ReactiveCommand.Create(EnqueueSampleJob);
        // Load existing jobs
        foreach(var job in _scheduler.GetAll())
        {
            Jobs.Add(job);
        }
    }
    private void EnqueueSampleJob() {
        var profiles = new JobProfileSet(
            new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
            new SocatProfileRef(20000, true),
            new PowerProfileRef("127.0.0.1", 502, 1, 3),
            new MemoryRegionProfile(0x00000000, 0x00010000),
            new PayloadSetProfile("resources/payloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dumps"));
        var resources = new []{
            new ResourceKey("serial","/dev/ttyUSB0"),
            new ResourceKey("tcp","127.0.0.1:20000"),
            new ResourceKey("power","127.0.0.1:502:1")};
        var job = new Job(Guid.NewGuid(), "Dump S7-1200", resources, profiles, JobState.Created, DateTimeOffset.Now);
        Jobs.Add(job);
        _scheduler.Enqueue(job);
    }
}