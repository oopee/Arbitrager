### General 
1) Database is created into a arbitrager.sqlite-file in Arbitrager-folder
2) Database is created automatically when DatabaseAccess.GetContext() is called
TODO: migrations

### Usage
1) Database is used through Interface.IDatabaseAccess implemented by DatabaseAccess.DatabaseAccess
2) Implemented methods in DatabaseAccess use GetMigration(db => {})-method that creates/update the database when necessary
3) Adding to database: db.TableName.Add(new TableRow() {}) (NOTE: No need to setu Id-value!)
4) TODO: Query, Query with Include, Update etc.

### Creating a new table
1. Create a new class in Interface/Entities and derive it from EntityBase
2. Create a DbSet<class> for it in the DatabaseAccess.DbContext
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


### Scaffolding a new migration
1. TODO: Creating the migration
2. TODO: Migrations is automatically run when GetContext is called