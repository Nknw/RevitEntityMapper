module Abstractions
open System.Reflection
open System
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.Mapper

type EntityDef = {
    entityType: Type
    guid:Guid
    name:string
    }

type SimpleType =
    |EntityType of EntityDef
    |ValueType of Type


type EntityType = 
    |Array of SimpleType
    |Map of Type * SimpleType
    |Entity of EntityDef
    |Simple of Type
    |None of Type
    |Error of string


let isnull o= Object.ReferenceEquals(null,o)
let log s subs = String.Format(s, List.toArray subs) 

let pipeline ctor predicate = function 
    |None(t) when predicate(t) -> t|> ctor 
    | some -> some

let dict = Dictionary().GetType().GetGenericTypeDefinition()
let list = List().GetType().GetGenericTypeDefinition()
let simple = HashSet( [typeof<int>])
let availableKeys = HashSet<Type>()

let getGenerecDef (t:Type) = t.GetGenericArguments()

let entityCtor  _ (t:Type) = 
    let schema =  t.GetCustomAttribute<SchemaAttribute>()
    match schema with
        | attr when isnull attr -> Error(log "{0} has no schema attribute" [t])
        | attr -> Entity({entityType=t;guid=attr.Guid;name = attr.Name})

let simpleCtor _ t = Simple(t)

let isEntity = pipeline (entityCtor (fun t-> t)) (fun t-> true)
let isSimple = pipeline  (isEntity |> simpleCtor) (fun t->simple.Contains t)

let tailHandler ctor = function
    | Simple(t) -> t |> ValueType |>ctor
    | Entity(def) -> def|> EntityType |> ctor 
    | Error(_) as e -> e
    | _ -> Error("Unhandled error")

let mapCtor cont t =
    let gTypes = getGenerecDef t
    let key = gTypes.[0]
    let value = gTypes.[1]
    match availableKeys.Contains key with
        |false -> Error(log "Unallowed dictionary key {0}" [key])
        |true -> value|> None |> cont |> tailHandler (fun e->Map(key,e)) 

let arrCtor cont t = 
    let gType = (getGenerecDef t).[0]
    gType|> None |> cont |> tailHandler (fun e->Array(e)) 

let isMap = pipeline (mapCtor isSimple) (fun t-> t.IsInterface && t = dict)
let isArray = pipeline (arrCtor isSimple) (fun t-> t.IsInterface && t = list)

let getType = isMap >> isArray >> isSimple >> isEntity

type NotTypedResult =
    |Suc
    |Fail of string

type 'k Result = 
    |Success of 'k
    |Failure of string
    static member Unhandled = Failure("Unhandled error")

let rec entityVisitor init toType basicHandler higthLevelVisitor entity =
    let this = higthLevelVisitor
    let rec createSomeInner tuple = 
        match tuple with
            |(state,[]) -> Success(state)
            |(state,h::tail) -> let next some =
                                    match some |> basicHandler this state with
                                        |Success(s) -> createSomeInner (s,tail)
                                        |Failure(_) as f -> f
                                
                                match h |> toType |> None |> getType with
                                    | Error(s) -> Failure(s)
                                    | Entity(_) | Map(_) | Array(_) | Simple(_) as t -> (t,h) |> next
                                    | _ -> Failure("Unhandled")
                                   
    entity |> init |> createSomeInner 


let rec visitorEntryPoint visitor finallize t =
    let this = visitorEntryPoint visitor finallize
    let result = match t |> None |> isEntity  with
                   | Error(s) -> Failure(s)
                   | Entity(e) -> e |> visitor this
                   | _ -> Failure("Unhandled")
    match result with
        | Failure(s) -> Fail(s)
        | Success(r) -> r |> finallize
                        Suc

