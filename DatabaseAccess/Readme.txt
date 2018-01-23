### General 
1) Database is created into a arbitrager_<configurationname>.sqlite-file in Arbitrager-folder
2) Database is created and migrations run automatically when DatabaseAccess.GetContext() is called

### Usage
1) Database is used through Interface.IDatabaseAccess implemented by DatabaseAccess.DatabaseAccess
2) Implemented methods in DatabaseAccess use GetMigration(db => {})-method that creates/update the database when necessary
3) Adding to database: db.TableName.Add(new TableRow() {}) (NOTE: No need to setu Id-value!)
4) TODO: Query, Query with Include, Update etc.

### Creating a new table
1. Create a new class in Interface/Entities and derive it from EntityBase
2. Create a DbSet<class> for it in the DatabaseAccess.DbContext
3. Create a DbConfigurationBase derived configuration file for it
TODO: add any necessary annotations, what actually are necessary? how to add them?

### Adding columns
1. Simple data columns are done as properties
2. References to other tables are a abit more complex
  - Create the other table
  - For example in one-to-many reference add OtherTableNameId-column to the "many" side
  - Add "public virtual List<OtherTableName> ManyItems { get; set; }" property to "many" side
  - Add "public virtual OtherTableName SingleItem { get; set; }" property to "one" side
  - Add configuration for the item if it requires foreignkey etc informaton during model building
    - Configurations are found in DatabaseAccess/Configurations

### Scaffolding a new migration after changes to database have been made
1. Scaffold the migration to a .cs file
	- On Windows:
		- Open VisualStudio -> Tools -> Nuget Package Manager -> Package Manager Console
		- Set Default project to "DatabaseAccess"
		- Run only on first time: "Install-Package Microsoft.EntityFrameworkCore.Tools"
		- Run "Add-Migration -Name <Name>"
	- On OSX:
		- Do pre-requisites: https://medium.com/@yostane/entity-framework-core-and-sqlite-database-migration-using-vs2017-macos-28812c64e7ef
		- Open console and run "dotnet ef migrations add <Name>"
2. Work around any SQLite limitations
	- https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations
	- ForeignKeys are not supported in migrations:
		- Comment out any "AddForeignKey" calls in the migrations Up-method
		- Make a SQLite call that creates the foreign key

