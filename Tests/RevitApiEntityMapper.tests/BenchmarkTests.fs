namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq
open System.IO
open GetterBuilder
open SetterBuilder

[<TestFixture>]
type BenchmarkTests () = 

    member val writer = new StreamWriter(Path.Combine(location,@"..\..\..\..\..\..\","Benchmark.txt"),false)

    [<SetUp>]
    member this.SetUp() = 
        setUp()

    member this.WriteMeasure (m) =
        this.writer.Write(m.ToString()+"|")

    member this.GetStr(e:Entity) =
        e.Get<string>("Str")

    member this.WriteSequence (sequence) =
        sequence |> Seq.iter (fun (s:string) -> s.PadRight(16) |> this.writer.Write
                                                this.writer.Write("|")) 

    member this.InitGet () = 
        this.WriteSequence(seq{"method";"runtime";"firstRun";"compiled"})
        this.WriteSequence(seq{"get"})

    member this.HandWrited (repeat,getEntity:unit->Entity) = 
        repeat 
         |> List.iter
            (fun _ -> let e = getEntity()
                      let mapper = BenchmarkMapper()
                      mapper.Str <- this.GetStr e
                      let inc = Included()
                      inc.Str <- e.Get<Entity>("Entity") |>this.GetStr
                      let inc2 = e.Get<IList<Entity>>("List") |> Seq.map (fun e -> let i = Included2()
                                                                                   i.Str <- this.GetStr e
                                                                                   i)
                      let inc3Creator e = let i = Included3()
                                          i.Str <- this.GetStr e
                                          i
                      let inc3 = Dictionary<string,Included3>()
                      e.Get<IDictionary<string,Entity>>("Dict") |> Seq.iter (fun p->inc3.Add(p.Key,p.Value|>inc3Creator))  
                      mapper.Dict <- inc3
                      mapper.Entity <- inc
                      mapper.List <- inc2.ToList())

    [<Test>]
    member this.GetterTests()=
        let getter = getterBuilder (Dictionary<Type,obj>())
        let setter = setterBuilder (Dictionary())
        let func =  getter typeof<BenchmarkMapper> 
        let wall = getWall ()
        let getEntity () = wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid))
        let factory = func :?> Entity -> BenchmarkMapper
        execInTransaction (fun () -> setter typeof<BenchmarkMapper> :?> BenchmarkMapper-> Entity <| BenchmarkMapper.CreateDefault() |> wall.SetEntity |> ignore)
        let repeatTimes = seq{0..100} |> Seq.toList
        measure (fun () -> repeatTimes 
                           |> List.map (fun _ -> () |> getEntity |> factory)
                           |>ignore) |> this.WriteMeasure
        
        measure ((fun () -> this.HandWrited(repeatTimes,getEntity))) |> this.WriteMeasure
        
        this.Finalize()

    member this.Finalize () =
        this.writer.Dispose()