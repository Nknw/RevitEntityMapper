namespace RevitApiEntityMapper
open Autodesk.Revit.DB
open System.Runtime.InteropServices
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open System
open GetterBuilder
open SetterBuilder
open Visitor

type Mapper () as self =

    member val getFactories = Dictionary<Type,obj>() 
    member val getter = getterBuilder self.getFactories

    member val setFactories = Dictionary<Type,obj>()
    member val setter = setterBuilder self.setFactories


    member this.TryGetEntity<'a when 'a:(new : unit->'a)> (e:Element,[<Out>]result: 'a byref) = 
        let def = getEntityDefenition typeof<'a>
        let cast (factory:obj) = factory :?> Entity->'a
        match Schema.Lookup(def.guid) with
        |null -> false
        | s -> let entity = e.GetEntity(s)
               match entity.IsValidObject with
               |false -> false
               |true ->  match self.getFactories.TryGetValue def.entityType with
                         |(true,factory) -> result <- factory |> cast <| entity
                                            true
                         |(false,_) -> result <- self.getter def |> cast <| entity 
                                       true
