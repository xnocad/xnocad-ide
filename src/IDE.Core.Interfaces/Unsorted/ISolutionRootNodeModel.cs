﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDE.Core.Interfaces
{
    public interface ISolutionRootNodeModel : ISolutionExplorerNodeModel
    {
        ISolutionDocument Solution { get; }
    }
}
