﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixelbyte;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixiniUnitTest
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void StringSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Case","My", "suit");

            Assert.AreEqual<string>("suit", p.Get("Case", "My"));
            Assert.AreEqual<string>("suit", p["Case", "My"]);
        }

        [TestMethod]
        public void IntegerSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Integer", "My", 45);

            Assert.AreEqual<int>(45, p.GetI("Integer", "My"));
        }

        [TestMethod]
        public void FloatSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Floater", "My", 560.896f);

            Assert.AreEqual<float>(560.896f, p.GetF("Floater", "My"));
        }

        [TestMethod]
        public void BoolSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Booler", "My", true);

            Assert.AreEqual<bool>(true, p.GetB("Booler", "My"));
        }

        [TestMethod]
        public void CaseInsensitivity()
        {
            Pixini p = new Pixini();

            //Set using the index property
            p["CaseTest", "My"] = "suit";

            //Test Section Case insensitivity
            Assert.AreEqual<string>("suit", p["Casetest", "my"]);

            //Test key case insensitivity
            Assert.AreEqual<string>("suit", p["casetest", "my"]);

            //Test section and key case insensitivity
            Assert.AreEqual<string>("suit", p["Casetest", "My"]);
        }

        [TestMethod]
        public void Defaults()
        {
            Pixini p = new Pixini();

            Assert.AreEqual<string>("chunky", p.Get("Monkey", "animals", "chunky"));
            Assert.AreEqual<string>(null, p.Get("Monkey", "animals"));

            //Test Int
            Assert.AreEqual<int>(783, p.GetI("MonkeyAge", "animals", 783));
            Assert.AreEqual<int>(int.MinValue, p.GetI("MonkeyAge", "animals"));

            //Test float
            Assert.AreEqual<float>(65.91f, p.GetF("MonkeyInjection", "animals", 65.91f));
            Assert.AreEqual<float>(float.NaN, p.GetF("MonkeyInjection", "animals"));

            //test bool
            Assert.AreEqual<bool>(true, p.GetB("MonkeyGood", "animals", true));
            Assert.AreEqual<bool>(false, p.GetB("MonkeyGood", "animals"));
        }

        [TestMethod]
        public void TestKeyValueDelete()
        {
            Pixini p = new Pixini();

            p.Set("Case", "My", "suit");
            p.Set("ShirtColor", "My", "red");

            Assert.AreEqual<string>("red", p.Get("ShirtColor", "My"));
            Assert.AreEqual<string>("suit", p["Case", "My"]);

            p.Delete("ShirtColor", "my");
            Assert.AreEqual<string>(null, p["ShirtColor", "My"]);

            //Test a failed delete
            Assert.AreEqual<bool>(false, p.Delete("NonExitentKey", "My"));
        }

        [TestMethod]
        public void GetStringArray()
        {
            string iniString = @"ShirtColors=red,green,blue,brown,beige";

            var p = Pixini.LoadFromString(iniString);

            //The single value should be null since this is now a list
            Assert.IsNull(p.Get("ShirtColors"));

            //Compare the returned collection with expected
            CollectionAssert.AreEqual(new string[] { "red", "green", "blue", "brown", "beige" }, p.ArrGet("shirtcolors"));
        }

        [TestMethod]
        public void SetFloatArray()
        {
            string iniString = @"Temperatures=84.5,63.3,92.1,71.1,64.4";

            var p = Pixini.LoadFromString(iniString);

            //The single value should be null since this is a list
            Assert.IsNull(p.Get("Temperatures"));

            var fArray = p.ArrGetF("Temperatures");

            //Change some of the values, re-set the array
            fArray[0] = 46.3f;
            fArray[3] = 110.1f;

            p.ArrSet<float>("temperatures", fArray);

            //Compare the returned collection with expected
            CollectionAssert.AreEqual(new float[] { 46.3f, 63.3f, 92.1f, 110.1f, 64.4f }, p.ArrGetF("Temperatures"));
        }

        [TestMethod]
        public void ToArrayAndBack()
        {
            string iniString = @"ShirtColors=red,green,blue,brown,beige";
            var p = Pixini.LoadFromString(iniString);

            //At this point, the value shouild be null since we should have picekd up the array
            Assert.IsNull(p["shirtcolors"]);

            //Ok, now let's set the shirt color to a single
            p["shirtcolors"] = "striped";

            //Now the array should return null
            Assert.IsNull(p.ArrGet("shirtcolors"));

            Assert.AreEqual("striped", p["shirtcolors"]);

            //Now set it to something that is a CSV
            p["shirtcolors"] = "checkered, black, blue";
            Assert.IsNull(p["shirtcolors"]);

            CollectionAssert.AreEqual(new string[] { "checkered", "black", "blue" }, p.ArrGet("ShirtColors"));
        }

        [TestMethod]
        public void DoubleQuotes()
        {
            string iniString = "cars=\"German, American, Japanese\"";
            var p = Pixini.LoadFromString(iniString);

            //Since the above is double quoted, it should NOT have been converted to an array
            Assert.IsNull(p.ArrGet("cars"));

            //When we pull the value back out, it should NOT have the quotes around it
            Assert.AreEqual("German, American, Japanese", p["Cars"]);

            //But when we convert it to a string, the quotes SHOULD be there
            Assert.AreEqual("cars=\"German, American, Japanese\"", p.ToString().TrimEnd('\n','\r'));
        }

        [TestMethod]
        public void SingleQuotes()
        {
            string iniString = "cars=\'German, American, Japanese\'";
            var p = Pixini.LoadFromString(iniString);

            //Since the above is double quoted, it should NOT have been converted to an array
            Assert.IsNull(p.ArrGet("cars"));

            //When we pull the value back out, it should NOT have the quotes around it
            Assert.AreEqual("German, American, Japanese", p["Cars"]);

            //But when we convert it to a string, the quotes SHOULD be there
            Assert.AreEqual("cars=\'German, American, Japanese\'", p.ToString().TrimEnd('\n', '\r'));
        }

        [TestMethod]
        public void TestLoadFromString()
        {
            string iniString = @"[Main]
MapX=0
MapY=0
Misc0=0
Misc1=0
Misc2=0
Misc3=0
PlayerName=CHANGEME
Switch0=FALSE
Switch1=FALSE
Switch2=TRUE
Switch3=FALSE
Switch4=true

[AnotherSection]
avagadro=6.022
";
            var p = Pixini.LoadFromString(iniString);

            Assert.AreEqual<bool>(true, p.GetB("switch2", "main", false));
            Assert.AreEqual<bool>(false, p.GetB("switch0", "main", true));
            Assert.AreEqual<string>("CHANGEME", p.Get("playername", "main", "no"));

            Assert.AreEqual<float>(6.022f, p.GetF("avagadro", "anothersection", 1.0f));
        }
    }
}
