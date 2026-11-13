using Commerce.Demo.Optimizely.Core.Infrastructure.Catalog;
using EPiServer.Commerce.Routing;
using EPiServer.Events;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog.Dto;
using Mediachase.Commerce.Catalog.Events;
using Mediachase.Commerce.Engine.Events;
using System.Text.Json;

namespace Commerce.Demo.Web.Infrastructure;

[InitializableModule]
[ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
public class CommerceInitialization : IInitializableModule
{
    private bool _eventsAttached = false;

    public void Initialize(InitializationEngine context)
    {
        CatalogRouteHelper.MapDefaultHierarchialRouter(false);

        var catalogRoot = context.Services.GetRequiredService<EnableCatalogRoot>();
        catalogRoot.SetCatalogAccessRights();

        if (!_eventsAttached)
        {
            //var ev = Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid);
            //ev.Raised += CatalogKeyEventUpdated;
            context.Services.GetInstance<ICatalogEvents>().AssociationUpdating += AssociationUpdating;
            context.Services.GetInstance<ICatalogEvents>().EntryUpdating += EntryUpdating;
            context.Services.GetInstance<ICatalogEvents>().RelationUpdating += RelationUpdating;
        }
    }

    private void CatalogKeyEventUpdated(object sender, EventNotificationEventArgs e)
    {
        var eventArgs = DeSerialize((byte[])e.Param);
        var inventoryUpdatedEventArgs = eventArgs as InventoryUpdateEventArgs;

        if (inventoryUpdatedEventArgs != null)
        {
            RemoteInventoryUpdated(sender, inventoryUpdatedEventArgs);
        }
    }

    private void RemoteInventoryUpdated(object sender, InventoryUpdateEventArgs inventoryUpdatedEventArgs)
    {
        //Your action when inventories are updated remotely.
    }

    protected virtual CatalogKeyEventArgs? DeSerialize(byte[] buffer)
    {
        var stream = new MemoryStream(buffer);
        return JsonSerializer.Deserialize<CatalogKeyEventArgs>(stream);
    }

    private void RelationUpdating(object? sender, RelationEventArgs e)
    {
        if (e.EventName.Equals("updating"))
        {

        }
    }

    private void EntryUpdating(object? sender, EntryEventArgs e)
    {
        if (e.EventName.Equals("updating"))
        {

        }
    }

    private void AssociationUpdating(object? sender, AssociationEventArgs e)
    {
        if (sender is CatalogAssociationDto association && e.EventName.Equals("updating"))
        {

        }
    }

    public void Uninitialize(InitializationEngine context)
    {
        if (_eventsAttached)
        {
            // Attach event handler to when a page has been created
            context.Services.GetInstance<ICatalogEvents>().AssociationUpdating -= AssociationUpdating;
            context.Services.GetInstance<ICatalogEvents>().EntryUpdating -= EntryUpdating;
            context.Services.GetInstance<ICatalogEvents>().RelationUpdating -= RelationUpdating;

            _eventsAttached = false;
        }
    }
}
