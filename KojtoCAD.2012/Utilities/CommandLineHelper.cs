﻿using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Castle.Core.Logging;
using System.Reflection;

#if !bcad
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = Autodesk.AutoCAD.Runtime.Exception;
#else
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Bricscad.EditorInput;
using Application = Bricscad.ApplicationServices.Application;
using Exception = Teigha.Runtime.Exception;
#endif

namespace KojtoCAD.Utilities
{
    [SuppressUnmanagedCodeSecurity]
    public static class CommandLineHelper
    {
        private const string ACAD_EXE = "acad.exe";

        private const short RTSTR = 5005;

        private const short RTNORM = 5100;

        private const short RTNONE = 5000;

        private const short RTREAL = 5001;

        private const short RT3DPOINT = 5009;

        private const short RTLONG = 5010;

        private const short RTSHORT = 5003;

        private const short RTENAME = 5006;

        private const short RTPOINT = 5002; /*2D point X and Y only */

        private static Dictionary<Type, short> resTypes = new Dictionary<Type, short>();

        private static ILogger _logger = NullLogger.Instance;

        public static ILogger Logger
        {
            get
            {
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        static CommandLineHelper()
        {
            resTypes[typeof(string)] = RTSTR;
            resTypes[typeof(double)] = RTREAL;
            resTypes[typeof(Point3d)] = RT3DPOINT;
            resTypes[typeof(ObjectId)] = RTENAME;
            resTypes[typeof(Int32)] = RTLONG;
            resTypes[typeof(Int16)] = RTSHORT;
            resTypes[typeof(Point2d)] = RTPOINT;
        }

        private static TypedValue TypedValueFromObject(Object val)
        {
            if (val == null) throw new ArgumentException("null not permitted as command argument");
            short code = -1;

            if (resTypes.TryGetValue(val.GetType(), out code) && code > 0)
            {
                return new TypedValue(code, val);
            }
            throw new InvalidOperationException("Unsupported type in Command() method");
        }

        public static int Command(params object[] args)
        {
#if !bcad
            if (Application.DocumentManager.IsApplicationContext) throw new InvalidCastException("Invalid execution context");
#endif
            int stat = 0;
            int cnt = 0;
            using (ResultBuffer buffer = new ResultBuffer())
            {
                foreach (object o in args)
                {
                    buffer.Add(TypedValueFromObject(o));
                    ++cnt;
                }
                if (cnt > 0)
                {
#if acad2012
                    stat = acedCmd2012(buffer.UnmanagedObject);
#endif
#if acad2013
                    stat = acedCmd2013( buffer.UnmanagedObject );
#endif
#if bcad
                    Bricscad.Global.Editor.Command(buffer);

#endif
                }
            }
            return stat;
        }


        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acedSetCurrentVPort@@YA?AW4ErrorStatus@Acad@@PBVAcDbViewport@@@Z")]
        //extern static private int acedCmd2013(IntPtr acDbVport);

        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acedSetCurrentVPort@@YA?AW4ErrorStatus@Acad@@PBVAcDbViewport@@@Z")]
        //extern static private int acedCmd2008(IntPtr acDbVport);

        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?acedSetCurrentVPort@@YA?AW4ErrorStatus@Acad@@PEBVAcDbViewport@@@Z")]
        //extern static int acedCmd2011(IntPtr resbuf);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("accore.dll", EntryPoint = "acedCmd", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Auto)]
        private static extern int acedCmd2013(IntPtr resbuf);

        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedCmd")]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("acad.exe", EntryPoint = "acedCmd", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Auto)]
        private static extern int acedCmd2012(IntPtr resbuf);

        public static void ExecuteStringOverInvoke(string command)
        {
            try
            {
#if acad2012
                object activeDocument = Application.DocumentManager.MdiActiveDocument.AcadDocument;
#endif

#if acad2013
                object activeDocument = Application.DocumentManager.MdiActiveDocument.GetAcadDocument();
#endif
#if bcad
                object activeDocument = Application.DocumentManager.MdiActiveDocument.AcadDocument;
#endif
                object[] data = { command };
                activeDocument.GetType()
                              .InvokeMember(
                                  "SendCommand", BindingFlags.InvokeMethod, null, activeDocument, data);
            }
            catch (Exception exception)
            {
                Logger.Error("Command line class error.", exception);
            }
        }

        static MethodInfo runCommand = typeof(Editor).GetMethod(
            "RunCommand", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static PromptStatus Command(this Editor ed, params object[] args)
        {
#if !bcad
            if (Application.DocumentManager.IsApplicationContext)
            {
                throw new InvalidOperationException("Invalid execution context for Command()");
            }
#endif
            if (ed.Document != Application.DocumentManager.MdiActiveDocument)
            {
                throw new InvalidOperationException("Document is not active");
            }
            return (PromptStatus)runCommand.Invoke(ed, new object[] { args });
        }
    }

    public class CommandLineShortCuts
    {
        // Sample member functions that use the Command() method.

        public static int ZoomExtents()
        {
            return CommandLineHelper.Command("._ZOOM", "_E");
        }

        public static int ZoomCenter(Point3d center, double height)
        {
            return CommandLineHelper.Command("._ZOOM", "_C", center, height);
        }

        public static int ZoomWindow(Point3d corner1, Point3d corner2)
        {
            return CommandLineHelper.Command("._ZOOM", "_W", corner1, corner2);
        }

        public static int SetFilletRadius(double filletRadius)
        {
            return CommandLineHelper.Command("._FILLET", "_R", filletRadius);
        }

        public static int FilletPolyline(ObjectId polylineId)
        {
            return CommandLineHelper.Command("._FILLET", "_P", polylineId);
        }
    }

}

