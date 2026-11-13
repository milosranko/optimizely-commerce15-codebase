using Commerce.Demo.Optimizely.ContentTypes.Cms.Pages;
using Commerce.Demo.Web.Controllers.Base;
using Commerce.Demo.Web.Models.Pages;
using EPiServer.Notification;
using EPiServer.Scheduler;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Demo.Web.Controllers;

public class StartPageController : PageControllerBase<StartPage>
{
    private readonly INotifier _notifier;
    private readonly QueryableNotificationUserService _notificationUserService;
    private readonly IScheduledJobLogRepository _scheduledJobLogRepository;
    private readonly IScheduledJobRepository _scheduledJobRepository;

    public StartPageController(
        INotifier notifier,
        QueryableNotificationUserService notificationUserService,
        IScheduledJobLogRepository scheduledJobLogRepository,
        IScheduledJobRepository scheduledJobRepository)
    {
        _notifier = notifier;
        _notificationUserService = notificationUserService;
        _scheduledJobLogRepository = scheduledJobLogRepository;
        _scheduledJobRepository = scheduledJobRepository;
    }

    public async Task<IActionResult> Index(StartPage currentPage)
    {
        //var test = PriceTypeConfiguration.Instance.PriceTypeDefinitions;
        var model = PageViewModel.Create(currentPage);

        //var user = await _notificationUserService.GetAsync("admin");
        //await _notifier.PostNotificationAsync(new NotificationMessage
        //{
        //    ChannelName = "epi.example",
        //    Content = $"Start page visited: {currentPage.Name}",
        //    Subject = "Start Page Visit",
        //    Recipients = new[] { user },
        //    Sender = user,
        //    TypeName = "StartPageVisitNotification"
        //});

        //var res = await _scheduledJobLogRepository.GetAsync(Guid.Parse(CookieConsentAuditPurgeJob.JobId), 0, 4);

        return View(model);
    }
}
