﻿using Pixelbyte.IO;
using System;

namespace PixiniTest
{
    class PixiniExample
    {
        static void Main(string[] args)
        {
            //var pix = Pixini.Load(@"E:\Dload\_games\SHARECART1000\dat\o_o.ini");
            //var pix = Pixini.Load(@"E:\untitled.txt");
            //pix.Set("Mixel", "Testing", "straight");
            //pix.SetI("Mixel", "Interval", 56);
            //pix.SetF("Main", "Timer", 79.43f);
            //pix.SetB("Main", "FlagDown", true);
            //Console.WriteLine(pix["startrek","Secondary Hull"] );

            //var actors = pix.ArrGet("starTrek", "Secondary Hull");
            //actors[2] = "Spiner";

            // pix.ArrSet<int>("starTrek", "Secondary Hull", 34, 56, 76);
            //Console.WriteLine(pix.ArrGetI("starTrek", "Secondary Hull").Length);

            //Console.WriteLine(pix.ToString());

            string iniString = @"MapX=0
                                MapY=234
                                Misc0=0
                                Misc1=23
                                Misc2=
                                Misc3=0
                                PlayerName=CHANGEME
                                Switch0=FALSE
                                Switch1=false
                                Switch2=TRUE
                                Switch3=FALSE
                                Switch4= 67.3,54.2,1

                                [AnotherSection]
                                avagadro=6.022
                                thearr=34,67,89,92,1
                                ";
            var p = Pixini.LoadFromString(iniString);

            Console.WriteLine(p["Switch4"]);

            var farr = p.GetArr<float>("Switch4");

            if (farr.Length == 3) Console.WriteLine("Perhaps a Vector3?");
            //if (p["misc2", "main"] == null)
            //    Console.WriteLine("Good: was null");

            //p.SetI("misc2", "main", 410);
            //Console.WriteLine(p.ToString());

            //pix.Save(@"F:\test.ini");
        }
    }
}
