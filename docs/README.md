# OptimizelyCommerceCodebase
Code base for Optimizely Commerce based projects

## Optimizely CLI
Optimizely provides a way to use PowerShell or Command Prompt to create DBs or a new template project.
Before running these commands make sure that you have installed Optimizely CLI tools:
https://docs.developers.optimizely.com/content-management-system/docs/installing-optimizely-net-5

dotnet tool install EPiServer.Net.Cli --global --add-source

### Run these commands from PowerShell or Command Prompt with Admin rights and be sure to be positioned in \src\Wisbar.Web folder and that wisbar_user has rights to create a new DB

dotnet-episerver create-cms-database -S localhost -E -dn Optimizely.Cms.Demo -du Optimizely_Demo_user -dp password Commerce.Demo.Web.csproj
dotnet-episerver create-commerce-database -S localhost -E -dn Optimizely.Commerce.Demo -du Optimizely_Demo_user -dp password Commerce.Demo.Web.csproj
dotnet-episerver add-admin-user -u admin -e admin@test.com -p MilosR-25 -c EPiServerDB Commerce.Demo.Web.csproj
dotnet-episerver add-admin-user -u admin -e admin@test.com -p MilosR-25 -c EcfSqlConnection Commerce.Demo.Web.csproj
