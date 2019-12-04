using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LTE.WebAPI.Models
{
    public enum TaskType
    {
        AreaCoverLayer = 0,
        CellCoverLayer = 1,
        AreaGSMLayer = 2,
        AreaBuildingLayer = 3,
        AreaTinLayer = 4,
        RoadTestLayer =5,
        TDDriverTest = 6,

        CellCoverCompu = 7,
        AreaCoverCompu = 8,
        AreaInterference = 9,
    }
}