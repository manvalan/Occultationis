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
    class Program
    {
        static void Main(string[] args)
        { /*
            String progID;

            progID = Telescope.Choose("ScopeSim.Telescope");
            Telescope T = new Telescope(progID);
            
             T.SetupDialog();
            T.Connected = true;
            TelescopeControl tc = new TelescopeControl( progID, T );
           
            tc.Goto(5, 88 );
            Console.Read();
            T.Connected = false;
            T.Dispose();
             * */


            JANClient astrometry = new JANClient();
            CalibrationData calibrationData = astrometry.imageCalibration("C:\\Users\\Michele\\documents\\visual studio 2013\\Projects\\ConsoleApplication6\\Occultationis\\Occultationis\\NGC7209.jpg");

            Console.WriteLine("RA : " + calibrationData.ra);
            Console.WriteLine("DEC: " + calibrationData.dec);


            String progID = Camera.Choose("CCDSimulator.Camera");
            //CameraControl cc = new CameraControl();
            //cc.infoCamera( progID );

            Console.Read();
        }
    }
}
