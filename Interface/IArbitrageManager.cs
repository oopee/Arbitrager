using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IArbitrageManager
    {
        Task Run();
        bool IsPaused { get; set; }
    }
}
