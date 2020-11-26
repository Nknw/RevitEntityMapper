﻿namespace RevitApiEntityMapper.tests
open NUnit.Framework
open ReflectedClasses
open Helpers
open System
open Autodesk.Revit.DB.ExtensibleStorage
open System.Collections.Generic
open System.Linq

[<TestFixture>]
type SetterTests ()=
    
    [<SetUp>]
    member this.SetUp() = setUp()

    [<Test>]
    member this.ShouldSetValue () = 
        typeof<Bool> 
           |> testSetterWith 
              (fun (func,schema) -> let factory = func :?> Bool -> Entity 
                                    let obj = Bool()
                                    obj.Some <- true
                                    let e = factory obj
                                    e.Get<bool>("Some") |> assertThat true)
    
    [<Test>]
    member this.ShouldSetWithIncludedEntity () =
        typeof<IncludedEntity> 
        |> testSetterWith 
           (fun (func,schema) -> let factory = func :?> IncludedEntity -> Entity
                                 let obj = IncludedEntity ()
                                 let included = Bool()
                                 included.Some <- true
                                 obj.Some <- included
                                 let e = factory obj
                                 e.Get<Entity>("Some").Get<bool>("Some") |> assertThat true)
    
    [<Test>]
    member this.ShouldSetListValues () =
        typeof<Lst> 
        |> testSetterWith 
           (fun (func,schema) -> let factory = func :?> Lst -> Entity
                                 let obj = Lst()
                                 let included = List<bool>(seq{true})
                                 obj.Some <- included
                                 let e = factory obj
                                 e.Get<IList<bool>>("Some").First() |> assertThat true)

    [<Test>]
    member this.ShouldSetListWithIncludedEntities () =
           typeof<IncludedEntityList> 
           |> testSetterWith 
              (fun (func,schema) -> let factory = func :?> IncludedEntityList -> Entity
                                    let obj = IncludedEntityList()
                                    let included = List<Bool>(seq{Bool()})
                                    obj.Some <- included
                                    let e = factory obj
                                    e.Get<IList<Entity>>("Some").First().Get<bool>("Some") |> assertThat false)

    [<Test>]
    member this.ShouldSetDictWithValues () =
        typeof<Dict> 
        |> testSetterWith 
           (fun (func,schema) -> let factory = func :?> Dict -> Entity
                                 let obj = Dict()
                                 let included = Dictionary<string,bool>()
                                 included.Add("key",true)
                                 obj.Some <- included
                                 let e = factory obj
                                 let first = e.Get<IDictionary<string,bool>>("Some").First()
                                 first.Value |> assertThat true
                                 first.Key |> assertThat "key")

    [<Test>]
    member this.ShouldSetDictWithIncludedEntities () =
        typeof<IncludedEntityInDictionary> 
        |> testSetterWith 
           (fun (func,schema) -> let factory = func :?> IncludedEntityInDictionary -> Entity
                                 let obj = IncludedEntityInDictionary()
                                 let included = Dictionary<string,Bool>()
                                 let bl = Bool()
                                 bl.Some <- true
                                 included.Add("key",bl)
                                 obj.Some <- included
                                 let e = factory obj
                                 let first = e.Get<IDictionary<string,Entity>>("Some").First()
                                 first.Value.Get<bool>("Some") |> assertThat true
                                 first.Key |> assertThat "key")