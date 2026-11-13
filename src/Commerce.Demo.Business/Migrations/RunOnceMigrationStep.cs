using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction.Migration;

namespace Commerce.Demo.Business.Migrations;

public abstract class RunOnceMigrationStep : MigrationStep
{
    public string Name { get; }

    protected RunOnceMigrationStep()
    {
        Name = GetType().Name;
    }

    protected abstract void RunOnce();

    public override void AddChanges()
    {
        var store = DynamicDataStoreFactory.Instance.CreateStore(typeof(MigrationStepChange));
        var record = store.Items<MigrationStepChange>().FirstOrDefault(r => r.Name == Name);

        if (record == null)
        {
            RunOnce();
            store.Save(new MigrationStepChange { Name = Name });
        }
    }
}
