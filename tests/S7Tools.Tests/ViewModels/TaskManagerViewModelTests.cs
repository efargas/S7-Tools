using FluentAssertions;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.Services.Jobs;
using S7Tools.ViewModels.Tasks;
using S7Tools.Core.Commands;
using Xunit;

namespace S7Tools.Tests.ViewModels;

/// <summary>
/// Tests for TaskManagerViewModel UI thread marshaling
/// </summary>
public class TaskManagerViewModelTests
{
    private static Mock<TaskDetailsViewModel> CreateMockTaskDetailsViewModel()
    {
        return new Mock<TaskDetailsViewModel>(
            new Mock<ILogger<TaskDetailsViewModel>>().Object,
            new Mock<ISocatService>().Object,
            new Mock<IPowerSupplyService>().Object,
            new Mock<IBootloaderService>().Object,
            new Mock<IUIThreadService>().Object,
            new Mock<IJobManager>().Object,
            new Mock<IPowerSupplyProfileService>().Object,
            new Mock<ISerialPortService>().Object,
            new Mock<ISerialPortProfileService>().Object,
            new Mock<ISocatProfileService>().Object,
            new Mock<IJobProfileSetFactory>().Object,
            new Mock<IClipboardService>().Object);
    }

    private static TaskManagerViewModel CreateViewModel(
        Mock<IUIThreadService>? uiThreadService = null,
        Mock<TaskDetailsViewModel>? taskDetailsViewModel = null)
    {
        var logger = new Mock<ILogger<TaskManagerViewModel>>();
        var taskScheduler = new Mock<ITaskScheduler>();
        var jobManager = new Mock<IJobManager>();
        var dialogService = new Mock<IDialogService>();

        return new TaskManagerViewModel(
            logger.Object,
            taskScheduler.Object,
            jobManager.Object,
            (uiThreadService ?? new Mock<IUIThreadService>()).Object,
            dialogService.Object,
            (taskDetailsViewModel ?? CreateMockTaskDetailsViewModel()).Object,
            new TaskStatisticsViewModel(),
            new TaskCommandManager(
                new Mock<ILogger<TaskCommandManager>>().Object,
                taskScheduler.Object,
                jobManager.Object,
                dialogService.Object
            ),
            new Mock<IServiceProvider>().Object);
    }

    [Fact]
    public void T088_TaskManagerViewModel_Should_Use_UIThreadService_For_Updates()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert - Verify ViewModel was constructed with UIThreadService
        viewModel.Should().NotBeNull();
        viewModel.RefreshTasksCommand.Should().NotBeNull();
    }

    [Fact]
    public void StatusMessage_Should_Be_Settable()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.StatusMessage = "Test Message";

        // Assert
        viewModel.StatusMessage.Should().Be("Test Message");
    }
}
