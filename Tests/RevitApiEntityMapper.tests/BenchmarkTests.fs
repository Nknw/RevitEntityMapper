namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq
open VCExtensibleStorageExtension.ElementExtensions
open VCExtensibleStorageExtension;
open System.Diagnostics
open System.IO
open GetterBuilder
open Abstractions

[<TestFixture>]
type BenchmarkTests () = 
    
    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.GetterTests()=
        let getter = getterBuilder (Dictionary<Type,obj>())
        match getter typeof<BenchmarkClass2> with
        |Failure(s) -> failwith s
        |Success(func) ->
                        let factory = func :?> Entity -> BenchmarkClass2
                        let wall = getWall ()
                        execInTransaction (fun () -> wall.SetEntity(BenchmarkClass.CreateDefault()) |> ignore)
                        let stopWacth1 = Stopwatch()
                        let repeatTimes = seq{0..100} |> Seq.toList
                        let e = wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid))
                        stopWacth1.Start()
                        repeatTimes|> List.map (fun i -> wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid)) |> factory |> ignore) |> ignore
                        stopWacth1.Stop()
                        let stopWatch2 = Stopwatch()
                        stopWatch2.Start()
                        repeatTimes |> List.map (fun i -> wall.GetEntity<BenchmarkClass>()|> ignore) |> ignore
                        stopWatch2.Stop()
                        let stopWatch3 = Stopwatch()
                        stopWatch3.Start()
                        repeatTimes|>List.map (fun _ ->let e =  wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid)) 
                                                       match getterBuilder (Dictionary<Type,obj>()) typeof<BenchmarkClass2> with
                                                       |Failure(s) -> failwith s
                                                       |Success(func) -> let factory = func :?> Entity -> BenchmarkClass2
                                                                         e |> factory |> ignore) |> ignore
                        stopWatch3.Stop()
                        let stopWatch4 = Stopwatch()
                        stopWatch4.Start()
                        repeatTimes |> List.map(fun _ ->e |> factory |>ignore ) |> ignore
                        stopWatch4.Stop()
                        use writer = new StreamWriter(Path.Combine(location,@"..\..\..\..\..\..\","Benchmark.txt"),false)
                        writer.WriteLine(stopWacth1.Elapsed)
                        writer.WriteLine(stopWatch2.Elapsed)
                        writer.WriteLine(stopWatch3.Elapsed)
                        writer.WriteLine(stopWatch4.Elapsed)
                        writer.WriteLine(stopWatch2.ElapsedMilliseconds/stopWacth1.ElapsedMilliseconds)