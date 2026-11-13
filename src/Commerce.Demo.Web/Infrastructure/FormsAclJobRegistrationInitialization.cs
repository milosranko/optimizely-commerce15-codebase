using System.Reflection;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Commerce.Demo.Web.Infrastructure;

/// <summary>
/// Workaround for EPiServer.Forms 6.0.2 on a fresh (or never-scheduled) CMS 13 database.
/// <para>
/// Forms' <c>EPiServer.Forms.EditView.InitializationModule</c> runs <c>UpdateUploadFolderACLJob</c> when its job row
/// is missing, and the job's <c>DisableJob()</c> then dereferences that same missing row → NullReferenceException and
/// the site does not start. In CMS 13 job rows are only registered later by the <c>SchedulerService</c> hosted service
/// (after all initialization modules), so the row is always missing on a new database.
/// </para>
/// <para>
/// This module registers the row up front, exactly as the scheduler would (settings copied from the job's own
/// <see cref="ScheduledJobAttribute"/>), so Forms runs its one-off ACL migration and disables the job as designed.
/// It runs before Forms' module because it declares the same two dependencies: the initialization engine
/// pre-sorts by dependency count, then by full name, and emits modules pass by pass as their dependencies resolve,
/// so with identical dependencies "Commerce.Demo…" is always emitted before "EPiServer.Forms…".
/// Remove once Forms handles the missing row itself.
/// </para>
/// </summary>
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Shell.UI.InitializationModule))]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class FormsAclJobRegistrationInitialization : IInitializableModule
{
    private const string FormsAclJobTypeName = "EPiServer.Forms.EditView.JobScheduler.UpdateUploadFolderACLJob, EPiServer.Forms.UI";

    public void Initialize(InitializationEngine context)
    {
        var logger = context.Services.GetInstance<ILogger<FormsAclJobRegistrationInitialization>>();

        try
        {
            var jobType = Type.GetType(FormsAclJobTypeName, throwOnError: false);
            var attribute = jobType?.GetCustomAttribute<ScheduledJobAttribute>();
            if (jobType is null || attribute is null || !Guid.TryParse(attribute.GUID, out var jobId))
            {
                logger.LogInformation("Forms upload-folder ACL job not found in this Forms version; nothing to register.");
                return;
            }

            var repository = context.Services.GetInstance<IScheduledJobRepository>();
            if (repository.Get(jobId) is not null)
            {
                return;
            }

            repository.Save(new ScheduledJob
            {
                ID = jobId,
                Name = jobType.Name,
                TypeName = jobType.FullName,
                AssemblyName = jobType.Assembly.GetName().Name,
                IsEnabled = attribute.DefaultEnabled,
                IntervalType = attribute.IntervalType,
                IntervalLength = attribute.IntervalLength
            });

            logger.LogInformation("Registered missing Forms scheduled job {JobType} ({JobId}) before Forms initialization.", jobType.Name, jobId);
        }
        catch (Exception ex)
        {
            // Never block startup from here; Forms will then fail with its own, clearer error.
            logger.LogError(ex, "Could not pre-register the Forms upload-folder ACL scheduled job.");
        }
    }

    public void Uninitialize(InitializationEngine context)
    {
    }
}
