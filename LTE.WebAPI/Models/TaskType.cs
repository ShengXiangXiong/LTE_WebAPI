﻿using System;
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
        RayRecordAdj = 10,
        RayRecordLoc = 11,
<<<<<<< HEAD
        Fishnet=30,
        BuildingOverlay=31,
        WaterOverlay=32,
        GrassOverlay=33,
        ScenePart=34,
        ClusterShp=35,
        AdjCoefficient=36

=======
        RayRecordAdjBatchMode = 12,
        Calibration =13
>>>>>>> 32b6b44cd958ba7d717103eb43e5adafe8e1089b
    }
}