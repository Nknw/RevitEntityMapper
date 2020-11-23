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
    
    member this.writer = new StreamWriter(Path.Combine(location,@"..\..\..\..\..\..\","Benchmark.txt"),false)

    [<SetUp>]
    member this.SetUp() = 
        setUp()

    member this.WriteMeasure (m) =
        this.writer.Write(m.ToString()+"|")

    [<Test>]
    member this.GetterTests()=
        let getter = getterBuilder (Dictionary<Type,obj>())
        match getter typeof<BenchmarkMapper> with
        |Failure(s) -> failwith s
        |Success(func) ->
                        let factory = func :?> Entity -> BenchmarkMapper
                        let wall = getWall ()
                        execInTransaction (fun () -> wall.SetEntity(BenchmarkExtensions.CreateDefault()) |> ignore)
                        let repeatTimes = seq{0..100} |> Seq.toList
                        measure (fun () -> repeatTimes 
                                           |> List.map (fun _ -> wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid)) 
                                                                  |> factory)
                                           |>ignore) |> this.WriteMeasure
                        measure ( fun () -> repeatTimes 
                                            |> List.map (fun _ -> wall.GetEntity<BenchmarkExtensions>()) 
                                                                  |> ignore) |> this.WriteMeasure

                        repeatTimes
                        |>List.map (fun _ ->let e =  wall.GetEntity(Schema.Lookup("231970cc-4909-44ca-9efd-9fca9d016f8b"|>Guid)) 
                                            match getterBuilder (Dictionary<Type,obj>()) typeof<BenchmarkMapper> with
                                            |Failure(s) -> failwith s
                                            |Success(func) -> let factory = func :?> Entity -> BenchmarkMapper
                                                              e |> factory) |> ignore

    override this.Finalize () =
        this.writer.Dispose()