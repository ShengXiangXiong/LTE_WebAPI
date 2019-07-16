using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LTE.WebAPI.Controllers
{

    public class HomeController : Controller
    {

        public HomeController()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
        }

        //
        // GET: /Home/
        public ActionResult Home()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult CellRayTracing()
        {
            return View();
        }

        public ActionResult Grid()
        {
            return View();
        }

        public ActionResult AreaCoverDefect()
        {
            return View();
        }

        public ActionResult InterferenceLocate()
        {
            return View();
        }

        public ActionResult SmoothBuildingVertex()
        {
            return View();
        }

        public ActionResult CellRadius()
        {
            return View();
        }

        public ActionResult DTdata()
        {
            return View();
        }

        public ActionResult ScenePart()
        {
            return View();
        }

        public ActionResult SingleRayTracingPt()
        {
            return View();
        }

        public ActionResult SingleRayTracingDir()
        {
            return View();
        }

        public ActionResult RayRecordLoc()
        {
            return View();
        }

        public ActionResult RayRecordAdj()
        {
            return View();
        }

        public ActionResult DebugCellRayTracing()
        {
            return View();
        }

        public ActionResult DebugRayRecordAdj()
        {
            return View();
        }

        public ActionResult DebugRayRecordLoc()
        {
            return View();
        }

        public ActionResult RefreshLayerCell()
        {
            return View();
        }

        public ActionResult RefreshLayerCellCover()
        {
            return View();
        }

        public ActionResult RefreshLayerAreaCover()
        {
            return View();
        }

        public ActionResult RefreshLayerVDT()
        {
            return View();
        }

        public ActionResult RefreshLayerAreaCoverDefect()
        {
            return View();
        }

        public ActionResult RefreshLayerInterference()
        {
            return View();
        }

        public ActionResult Calibration()
        {
            return View();
        }

        public ActionResult Grid3DStrengthQuery()
        {
            return View();
        }

    }
}
