using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixelbyte.IO;

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

            Assert.AreEqual("suit", p.Get("Case", "My", ""));
            Assert.AreEqual("suit", p["Case", "My"]);
        }

        [TestMethod]
        public void IntegerSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Integer", "My", 45);

            Assert.AreEqual(45, p.Get("Integer", "My", 0));
        }

        [TestMethod]
        public void FloatSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Floater", "My", 560.896f);

            Assert.AreEqual(560.896f, p.Get<float>("Floater", "My"));
        }

        [TestMethod]
        public void BoolSetGet()
        {
            Pixini p = new Pixini();

            p.Set("Booler", "My", true);

            Assert.AreEqual(true, p.Get<bool>("Booler", "My"));
        }

        [TestMethod]
        public void CaseInsensitivity()
        {
            Pixini p = new Pixini();

            //Set using the index property
            p["CaseTest", "My"] = "suit";

            //Test Section Case insensitivity
            Assert.AreEqual("suit", p["Casetest", "my"]);

            //Test key case insensitivity
            Assert.AreEqual("suit", p["casetest", "my"]);

            //Test section and key case insensitivity
            Assert.AreEqual("suit", p["Casetest", "My"]);
        }

        [TestMethod]
        public void Defaults()
        {
            Pixini p = new Pixini();

            Assert.AreEqual("chunky", p.Get("Monkey", "animals", "chunky"));
            Assert.AreEqual(string.Empty, p.Get("Monkey", "animals", string.Empty));

            //Test Int
            Assert.AreEqual(783, p.Get("MonkeyAge", "animals", 783));
            Assert.AreEqual(int.MinValue, p.Get("MonkeyAge", "animals", int.MinValue));

            //Test float
            Assert.AreEqual(65.91f, p.Get("MonkeyInjection", "animals", 65.91f));
            Assert.AreEqual(float.NaN, p.Get("MonkeyInjection", "animals", float.NaN));

            //test bool
            Assert.AreEqual(true, p.Get("MonkeyGood", "animals", true));
            Assert.AreEqual(false, p.Get<bool>("MonkeyGood", "animals"));
        }

        [TestMethod]
        public void TestKeyValueDelete()
        {
            Pixini p = new Pixini();

            p.Set("Case", "My", "suit");
            p.Set("ShirtColor", "My", "red");

            Assert.AreEqual("red", p.Get<string>("ShirtColor", "My"));
            Assert.AreEqual("suit", p["Case", "My"]);

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

            //Verify the arrya is of the correct length
            Assert.AreEqual(p.GetArr<string>("ShirtColors").Length, 5);

            Assert.IsTrue(p.IsArray("ShirtColors"));

            //Compare the returned collection with expected
            CollectionAssert.AreEqual(new string[] { "red", "green", "blue", "brown", "beige" }, p.GetArr<string>("shirtcolors"));
        }

        [TestMethod]
        public void SetFloatArray()
        {
            string iniString = @"Temperatures=84.5,63.3,92.1,71.1,64.4";

            var p = Pixini.LoadFromString(iniString);

            //Verify the arrya is of the correct length
            Assert.AreEqual(p.GetArr<float>("Temperatures").Length, 5);

            var fArray = p.GetArr<float>("Temperatures");

            //Change some of the values, re-set the array
            fArray[0] = 46.3f;
            fArray[3] = 110.1f;

            p.SetA("temperatures", fArray);

            //Compare the returned collection with expected
            CollectionAssert.AreEqual(new float[] { 46.3f, 63.3f, 92.1f, 110.1f, 64.4f }, p.GetArr<float>("Temperatures"));
        }

        [TestMethod]
        public void ToArrayAndBack()
        {
            string iniString = @"ShirtColors=red,green,blue,brown,beige";
            var p = Pixini.LoadFromString(iniString);

            Assert.IsTrue(p.IsArray("ShirtColors"));

            //Ok, now let's set the shirt color to a single
            p["shirtcolors"] = "striped";

            Assert.IsFalse(p.IsArray("ShirtColors"));

            Assert.AreEqual("striped", p["shirtcolors"]);

            //Now set it to something that is a CSV
            p["shirtcolors"] = "checkered, black, blue";
            Assert.IsTrue(p.IsArray("ShirtColors"));

            CollectionAssert.AreEqual(new string[] { "checkered", "black", "blue" }, p.GetArr<string>("ShirtColors"));
        }

        [TestMethod]
        public void DoubleQuotes()
        {
            string iniString = "cars=\"German, American, Japanese\"";
            var p = Pixini.LoadFromString(iniString);

            //Since the above is double quoted, it should NOT have been converted to an array
            Assert.IsNull(p.GetArr<string>("cars"));

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
            Assert.IsNull(p.GetArr<string>("cars"));

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

            Assert.AreEqual<bool>(true, p.Get("switch2", "main", false));
            Assert.AreEqual<bool>(false, p.Get("switch0", "main", true));
            Assert.AreEqual<string>("CHANGEME", p.Get("playername", "main", "no"));

            Assert.AreEqual<float>(6.022f, p.Get("avagadro", "anothersection", 1.0f));
        }
    }
}
