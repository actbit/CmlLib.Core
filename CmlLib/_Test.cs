﻿using CmlLib.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmlLib
{
    public class _Test
    {
        public static string tstr = "462,31";

        [MethodTimer.Time]
        public static void testTimer()
        {
            System.Diagnostics.Trace.WriteLine("Trace");
            Console.WriteLine("Hello");
        }
    }
}
