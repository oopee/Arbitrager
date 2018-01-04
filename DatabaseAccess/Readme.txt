### General 
1) Database is created into a arbitrager.sqlite-file in Arbitrager-folder
2) Database is created automatically when DatabaseAccess.GetContext() is called
TODO: migrations

### Usage
1) Database is used through Interface.IDatabaseAccess implemented by DatabaseAccess.DatabaseAccess
2) Implemented methods in DatabaseAccess use GetMigration()-method that creates/update the database when necessary

### Creating a new table
1. Create a new class in Interface/Entities and derive it from EntityBase
2. Create a DbSet<class> for it in the DatabaseAccess.DbContext
TODO: add any necessary annotations

### Adding columns
1. Simple data columns are done as properties
TODO: complex properties

### Scaffolding a new migration
1. TODO: Creating the migration
2. TODO: Migrations is automatically run when GetContext is called