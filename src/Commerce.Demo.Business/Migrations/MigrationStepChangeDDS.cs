using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Commerce.Demo.Business.Migrations;

[EPiServerDataStore(AutomaticallyRemapStore = true)]
public class MigrationStepChange : IDynamicData
{
    public Identity Id { get; set; }

    public string Name { get; set; }
}
