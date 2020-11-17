module Abstractions
open System.Reflection
open System
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.Mapper
open System.ComponentModel.DataAnnotations.Schema
open System.Reflection
open FSharp.Quotations

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

let log s subs = String.Format(s, List.toArray subs) 
let typeofEntity = typeof<Entity>
let getProps entity = entity.entityType.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                        |> List.ofArray 
                        |> List.filter (fun p-> not << isNull <|(p.GetCustomAttribute<NotMappedAttribute>()))
let getMethodInfo (e : Expr<'T>) : MethodInfo =
  match e with
  | Patterns.Call (_, mi, _) -> mi
  | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo = let typedInfo = getMethodInfo e
                                                    typedInfo.GetGenericMethodDefinition ()

let pipeline ctor predicate = function 
    |None(t) when predicate(t) -> t|> ctor 
    | some -> some

let dict = typedefof<IDictionary<obj,obj>>
let list = typedefof<IList<obj>>
let simple = HashSet( [typeof<int>])
let availableKeys = HashSet<Type>()

let getGenerecDef (t:Type) = t.GetGenericArguments()

let entityCtor  _ (t:Type) = 
    let schema =  t.GetCustomAttribute<SchemaAttribute>()
    match schema with
        | null -> Error(log "{0} has no schema attribute" [t])
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

type 'k Result = 
    |Success of 'k
    |Failure of string

type InitResult<'a,'b> = 
    |Complited of 'a
    |NeedsCreate of 'b * PropertyInfo list

let continueSuccess cont = function
    | Success(s) -> cont s
    | Failure(s) -> Failure(s)

let getPropType (prop:PropertyInfo) = prop.PropertyType

let visitorBuilder init body finallize =
    let rec visitor entity =
        let rec iter  = 
            function
                |(state,[]) -> Success(state)
                |(state,h::tail) -> let next some =
                                        some |> body visitor state
                                             |> continueSuccess (fun s-> iter (s,tail))
                                    
                                    match h |> getPropType |> None |> getType with
                                        | Error(s) -> Failure(s)
                                        | Entity(_) | Map(_) | Array(_) | Simple(_) as t -> (t,h) |> next
                                        | _ -> Failure("Unhandled")
                                       
        match entity |> init with 
            | NeedsCreate(s,props) -> iter (s,props) |> continueSuccess finallize
            | Complited(cs) -> cs
    visitor


let higthLevelVisitorBuilder visitor =
    let hlVisitor t =
        match t |> None |> isEntity  with
             | Error(s) -> Failure(s)
             | Entity(e) -> e |> visitor 
             | _ -> Failure("Unhandled")
    hlVisitor

