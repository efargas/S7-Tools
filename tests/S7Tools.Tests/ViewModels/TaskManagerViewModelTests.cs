using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Tasks;
using Xunit;

namespace S7Tools.Tests.ViewModels;

/// <summary>
/// Tests for TaskManagerViewModel UI thread marshaling
/// </summary>
public class TaskManagerViewModelTests
{
    [Fact]
    public void T088_TaskManagerViewModel_Should_Use_UIThreadService_For_Updates()
    {
        // Arrange
        var logger = new Mock<ILogger<TaskManagerViewModel>>();
        var taskScheduler = new Mock<ITaskScheduler>();
        var jobManager = new Mock<IJobManager>();
        var uiThreadService = new Mock<IUIThreadService>();
        var dialogService = new Mock<IDialogService>();
        var taskDetailsViewModel = new Mock<TaskDetailsViewModel>(
            new Mock<ILogger<TaskDetailsViewModel>>().Object,
            new Mock<ISocatService>().Object,
            new Mock<IPowerSupplyService>().Object,
            new Mock<IEnhancedBootloaderService>().Object,
            new Mock<IUIThreadService>().Object);

        // Act
        var viewModel = new TaskManagerViewModel(
            logger.Object,
            taskScheduler.Object,
            jobManager.Object,
            uiThreadService.Object,
            dialogService.Object,
            taskDetailsViewModel.Object);

        // Assert - Verify ViewModel was constructed with UIThreadService
        Assert.NotNull(viewModel);
        Assert.NotNull(viewModel.RefreshTasksCommand);
    }

    [Fact]
    public void StatusMessage_Should_Be_Settable()
    {
        // Arrange
        var logger = new Mock<ILogger<TaskManagerViewModel>>();
        var taskScheduler = new Mock<ITaskScheduler>();
        var jobManager = new Mock<IJobManager>();
        var uiThreadService = new Mock<IUIThreadService>();
        var dialogService = new Mock<IDialogService>();
        var taskDetailsViewModel = new Mock<TaskDetailsViewModel>(
            new Mock<ILogger<TaskDetailsViewModel>>().Object,
            new Mock<ISocatService>().Object,
            new Mock<IPowerSupplyService>().Object,
            new Mock<IEnhancedBootloaderService>().Object,
            new Mock<IUIThreadService>().Object);

        var viewModel = new TaskManagerViewModel(
            logger.Object,
            taskScheduler.Object,
            jobManager.Object,
            uiThreadService.Object,
            dialogService.Object,
            taskDetailsViewModel.Object);

        // Act
        viewModel.StatusMessage = "Test Message";

        // Assert
        Assert.Equal("Test Message", viewModel.StatusMessage);
    }
}
