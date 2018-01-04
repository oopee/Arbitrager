using System;
namespace Interface.Entities
{
    public abstract class EntityBase
    {
        // For some reason we don't need the [Key] annotation described in the tutorials, thins increments as usual
        public int Id { get; set; }
    }
}
