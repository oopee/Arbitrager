using System;
namespace DatabaseAccess.Entities
{
    public abstract class DbEntityBase
    {
        // For some reason we don't need the [Key] annotation described in the tutorials, this increments as expected
        public int Id { get; set; }
    }
}
