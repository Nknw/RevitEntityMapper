using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Autodesk.Revit.Mapper;
using System.IO;
using RTF.Applications;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using ReflectedClasses;

namespace Benchmark.tests
{
    [TestFixture]
    public class BenchmarkTests
    {
        private readonly StreamWriter writer;
        private readonly Mapper mapper = new Mapper();
        private Element wall;
        private readonly int testCount = 100;

        public BenchmarkTests()
        {
            var asmLocation = typeof(BenchmarkTests).Assembly.Location;
            var location = Path.GetDirectoryName(asmLocation);
            SetUp(location);
            writer = new StreamWriter(Path.Combine(location, @"..\..\..\..\..\", "Benchmark.txt"), true);
        }

        public void WriteLine(params object[] strings)
        {
            writer.Write("|");
            foreach (var s in strings)
                writer.Write(s.ToString().PadRight(20) + "|");
            writer.WriteLine();
        }

        public void SetUp(string loc)
        {
            var prj = Path.Combine(loc, "prj.rvt");
            var app = RevitTestExecutive.CommandData.Application;
            app.OpenAndActivateDocument(prj);
            var doc = app.ActiveUIDocument.Document;
            wall = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .FirstElement();
        }

        public TimeSpan Measure<T>(Func<T> action)
        {
            var countTimes = Enumerable.Range(0, testCount);
            var avar = (long)countTimes.Select(_ =>
            {
                var sw = new Stopwatch();
                sw.Start();
                action();
                sw.Stop();
                return sw.ElapsedTicks;
            }).Average();
            return new TimeSpan(avar);
        }

        public string GetStr(Entity entity)
        {
            return entity.Get<string>("Str");
        }

        public BenchmarkClass CompiledGet()
        {
            var sh = Schema.Lookup(new Guid("231970cc-4909-44ca-9efd-9fca9d016f8b"));
            var entity = wall.GetEntity(sh);
            return new BenchmarkClass
            {
                Str = GetStr(entity),
                List = entity.Get<IList<Entity>>("List")
                    .Select(e => new Included2() { Str = GetStr(e) })
                    .ToList(),
                Entity = new Included()
                {
                    Str = GetStr(entity.Get<Entity>("Entity"))
                },
                Dict = entity.Get<IDictionary<string, Entity>>("Dict")
                    .ToDictionary(p => p.Key, p => new Included3() { Str = GetStr(p.Value) })
            };
        }

        public Entity CompiledSet(BenchmarkClass mapped)
        {
            var main = new Entity(new Guid("231970cc-4909-44ca-9efd-9fca9d016f8b"));
            var inc = new Entity(new Guid("1b9cd3cf-9c57-4834-a64b-7bed19683223"));
            inc.Set("Str", mapped.Str);
            var inc2 = mapped.List.Select(i =>
            {
                var e = new Entity(new Guid("749f8b14-80a1-480c-9a88-524bc1ece775"));
                e.Set("Str", i.Str);
                return e;
            }).ToList();
            var inc3 = mapped.Dict.ToDictionary(p => p.Key, p =>
            {
                var e = new Entity(new Guid("f01b140d-da57-4891-9aab-1800710dbc48"));
                e.Set("Str", p.Value.Str);
                return e;
            });
            main.Set("Entity", inc);
            main.Set<IList<Entity>>("List", inc2);
            main.Set<IDictionary<string, Entity>>("Dict", inc3);
            using (var tr = new Transaction(wall.Document, "test"))
            {
                tr.Start();
                wall.SetEntity(main);
                tr.Commit();
            }
            return main;
        }

        [Test]
        public void GetTest()
        {
            mapper.SetEntity(wall, BenchmarkClass.CreateDefault());
            CompiledGet();
            var compiled = Measure(CompiledGet);
            mapper.TryGetEntity<BenchmarkClass>(wall,out var r);
            var runtime = Measure(() =>
            {
                mapper.TryGetEntity<BenchmarkClass>(wall, out var res);
                return res;
            });
            var fstRun = Measure(() =>
            {
                var mp = new Mapper();
                mp.TryGetEntity<BenchmarkClass>(wall, out var res);
                return res;
            });
            WriteLine(new object[] { "Get", compiled, runtime, fstRun });
            writer.Dispose();
            Assert.AreEqual(1, 1);
        }

        [Test]
        public void SetTest()
        {
            mapper.SetEntity(wall, BenchmarkClass.CreateDefault());
            var mapped = BenchmarkClass.CreateDefault();
            CompiledSet(mapped);
            var fstRun = Measure(() =>
            {
                var mp = new Mapper();
                mp.SetEntity(wall, mapped);
                return 1;
            });
            var compiled = Measure(() => CompiledSet(mapped));
            var runtime = Measure(() =>
            {
                mapper.SetEntity(wall, mapped);
                return 1;
            });
            WriteLine(new object[] { "Set", compiled, runtime, fstRun });
            writer.Dispose();
            Assert.AreEqual(1, 1);
        }
    }
}
