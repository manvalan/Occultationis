using System;
using System.Text;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Xml;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using ASCOM;
using ASCOM.Controls;
using ASCOM.Utilities.Video;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using ASCOM.Interface;
using ASCOM.Helper;
using ComputerBeacon.Json;
using CodeScales.Http;
using CodeScales.Http.Methods;
using CodeScales.Http.Entity;
using CodeScales.Http.Entity.Mime;
using CodeScales.Http.Common;
using Occultationis;

namespace Occultationis
{
    class TelescopeControl
    {
        string progID;
        Telescope myTel;
        public bool debug = true;

        public TelescopeControl()
        {
            progID = Telescope.Choose("ScopeSim.Telescope");
            myTel = new Telescope(progID);
            myTel.SetupDialog();
            myTel.Connected = true;
        }

        public TelescopeControl(string progIDL, Telescope T)
        {
            progID = progIDL;
            myTel = T;
        }

        public bool Goto(double ra, double dec)
        {
            bool bRet = false;

            myTel.SlewToCoordinates(ra, dec);
            if (debug)
            {
                Console.Write("RA = " + myTel.RightAscension);
                Console.WriteLine("    DEC = " + myTel.Declination);
            }
            return bRet;
        }
    }
}
