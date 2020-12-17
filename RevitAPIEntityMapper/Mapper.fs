namespace Autodesk.Revit.Mapper
open Autodesk.Revit.DB
open System.Runtime.InteropServices
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open System
open GetterBuilder
open SetterBuilder
open Visitor
open Creator

[<Sealed>]
type Mapper () =

    let getFactories = Dictionary<Type,obj>() 
    let getter = getterBuilder getFactories

    let setFactories = Dictionary<Type,obj>()
    let setter = setterBuilder setFactories

    let creator = creator

    member private this.execInTransaction(doc,change) =
        use tr = new Transaction(doc,"entity set")
        tr.Start() |> ignore
        change ()
        tr.Commit() |> ignore

    member this.TryGetEntity<'a when 'a:(new : unit->'a)> (e:Element,[<Out>]result: 'a byref) = 
        let def = getEntityDefenition typeof<'a>
        let cast (factory:obj) = factory :?> Entity->'a
        match Schema.Lookup def.guid with
        |null -> false
        | s -> let entity = e.GetEntity s
               match entity.IsValid() with
               |false -> false
               |true ->  match getFactories.TryGetValue def.entityType with
                         |(true,factory) -> result <- factory |> cast <| entity
                                            true
                         |(false,_) -> result <- getter def |> cast <| entity 
                                       true

    member this.GetEntity<'a when 'a:(new : unit->'a)> (e:Element) = 
        let def = getEntityDefenition typeof<'a>
        let cast (factory:obj) = factory :?> Entity->'a
        match Schema.Lookup def.guid with
        |null -> raise (new MapperException("Entity {0} not in memory",[def.entityType]))
        | s -> let entity = e.GetEntity s
               match entity.IsValid() with
               |false -> raise (new MapperException("Entity {0} is invalid",[def.entityType]))
               |true ->  match getFactories.TryGetValue def.entityType with
                         |(true,factory) -> factory |> cast <| entity
                         |(false,_) -> getter def |> cast <| entity 

    member this.SetEntity<'a when 'a:(new : unit->'a)> (e:Element,entity:'a) = 
        let def = getEntityDefenition typeof<'a>
        let set (factory:obj) = this.execInTransaction (e.Document,
                                 (fun () -> factory :?> 'a -> Entity <| entity |> e.SetEntity)) |> ignore
        creator def |> ignore
        match setFactories.TryGetValue def.entityType with
        |(true,factory) -> factory |> set
        |(false,_) -> setter def |> set 

    member this.GetGuid<'a when 'a:(new : unit->'a)> () =
        let def = getEntityDefenition typeof<'a>
        def.guid
