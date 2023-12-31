﻿using System.Threading.Tasks;
using IDE.Core.Build;
using IDE.Core.Interfaces;

namespace IDE.Core.Presentation.Builders
{
    public interface IBoardBuilder
    {
        Task<BuildResult> Build(IBoardDesigner board, string folderOutput);
    }
}
