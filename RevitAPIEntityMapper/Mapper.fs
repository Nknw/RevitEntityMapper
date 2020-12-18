namespace Autodesk.Revit.Mapper
open Autodesk.Revit.DB
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open System
open FSharp.Core
open GetterBuilder
open SetterBuilder
open Visitor
open Creator
open TypeResolver

type IMapper = 
    abstract member Get<'obj when 'obj:(new : unit->'obj) and 'obj:null> : Element -> 'obj
    abstract member Set<'obj when 'obj:(new : unit->'obj) and 'obj:null> : Element * 'obj -> unit

type IMapper<'obj when 'obj:(new : unit->'obj) and 'obj:null> =
    abstract member GetEntity : Element -> 'obj
    abstract member SetEntity : Element * 'obj -> unit

type internal CachingMapper () =

    let getFactories = Dictionary<Type,obj>() 
    let getter = getterBuilder getFactories
    
    let setFactories = Dictionary<Type,obj>()
    let setter = setterBuilder setFactories
    
    let creator = creator

    member this.GetGetterFor(t) =
        match getFactories.TryGetValue t with
         |(true,factory) -> Some(factory)
         |_ -> None

    member this.GetSetterFor(t) =
        match setFactories.TryGetValue t with
         |(true,factory) -> Some(factory)
         |_ -> None

    interface IMapper with 
        member this.Get<'obj when 'obj:(new : unit->'obj) and 'obj:null> (e:Element) = 
            let def = getEntityDefenition typeof<'obj>
            let cast (factory:obj) = factory :?> Entity -> 'obj
            match Schema.Lookup def.guid with
            |null -> null
            | s -> let entity = e.GetEntity s
                   match getFactories.TryGetValue def.entityType with
                   |(true,factory) -> entity |> cast  factory
                   |(false,_) -> entity |> cast (getter def)

        member this.Set<'obj when 'obj:(new : unit->'obj) and 'obj:null> (e:Element,obj:'obj) =
            let def = getEntityDefenition typeof<'obj>
            let set (factory:obj) = factory :?> 'obj -> Entity <| obj |> e.SetEntity |> ignore

            creator def |> ignore
            match setFactories.TryGetValue def.entityType with
            |(true,factory) -> factory |> set
            |(false,_) -> setter def |> set

type internal AdHocMapper<'obj when 'obj:(new : unit->'obj) and 'obj:null> 
    (mgetter:Option<obj>,msetter:Option<obj>,def:EntityDef) =

    let getter = 
        match mgetter with
        |Some(factory) -> factory :?> Entity -> 'obj
        |None -> getterBuilder (Dictionary()) def :?> Entity -> 'obj
     
    let setter = 
        match msetter with
        |Some(factory) -> factory :?> 'obj -> Entity
        |None -> setterBuilder (Dictionary()) def :?> 'obj -> Entity

    let schema = creator def
    
    interface IMapper<'obj> with
        member this.GetEntity (e:Element) = 
            e.GetEntity schema |> getter

        member this.SetEntity (e:Element,entity:'obj) =
            setter entity |> e.SetEntity
            

[<AbstractClass;Sealed>]
type Mapper private () =
    
    static let mappers = HashSet<WeakReference> ()

    static let eraseReferences () = mappers.RemoveWhere(fun m ->not m.IsAlive) |> ignore

    static member GetGuid<'obj when 'obj:(new : unit->'obj) and 'obj:null> () =
        let def = getEntityDefenition typeof<'obj>
        def.guid

    static member CreateNew () = 
        let mapper =  CachingMapper() 
        WeakReference(mapper) |> mappers.Add |> ignore
        mapper :> IMapper

    static member CreateAdHoc<'obj when 'obj:(new : unit->'obj) and 'obj:null> () = 
        eraseReferences ()
        let def = getEntityDefenition typeof<'obj>
        let getter = mappers |> Seq.tryPick 
                                 (fun wr -> let mapper = wr.Target :?> CachingMapper
                                            mapper.GetGetterFor def.entityType)

        let setter =  mappers |> Seq.tryPick 
                                 (fun wr -> let mapper = wr.Target :?> CachingMapper
                                            mapper.GetSetterFor def.entityType)

        AdHocMapper<'obj>(setter,getter,def) :> IMapper<'obj>       
