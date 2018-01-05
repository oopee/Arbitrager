using System;
namespace Interface.Entities
{
    public abstract class EntityBase
    {
        // For some reason we don't need the [Key] annotation described in the tutorials, this increments as expected
        public int Id { get; set; }
    }
}
