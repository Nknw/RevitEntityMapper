﻿module TypeResolver
open System.Reflection
open System
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB
open System.Linq
open FSharp.Quotations
open Revit.EntityMapper

do
    AppDomain.CurrentDomain.add_AssemblyResolve
         (ResolveEventHandler(fun _ arg ->
             match arg.Name.Contains("4.4.3.0") && arg.Name.Contains("FSharp.Core") with
              |false -> null
              |true -> AppDomain.CurrentDomain.GetAssemblies() |> Array.find (fun asm -> asm.GetName().Name.Contains("FSharp.Core")))) |> ignore

//type helpers
let typeofEntity = typeof<Entity>
let dict = typedefof<IDictionary<obj,obj>>
let genDict tps = dict.MakeGenericType (tps|>List.toArray)
let list = typedefof<IList<obj>>
let genList t = list.MakeGenericType ([t]|>List.toArray) 
let simple = HashSet( [typeof<int>;
                       typeof<bool>;
                       typeof<byte>;
                       typeof<int16>;
                       typeof<float>;
                       typeof<double>;
                       typeof<Guid>;
                       typeof<string>;
                       typeof<UV>;
                       typeof<ElementId>;
                       typeof<XYZ>])
let availableKeys = HashSet([typeof<int>;
                             typeof<bool>;
                             typeof<byte>;
                             typeof<int16>;
                             typeof<Guid>;
                             typeof<string>;
                             typeof<ElementId>])

let getMethodInfo (e : Expr<'T>) : MethodInfo =
  match e with
  | Patterns.Call (_, mi, _) -> mi
  | _ -> failwithf "Expression has the wrong shape, expected Call (_, _, _) instead got: %A" e

let genericMethodInfo (e : Expr<'T>) : MethodInfo = let typedInfo = getMethodInfo e
                                                    typedInfo.GetGenericMethodDefinition ()

let makeGenType types (m:Type) = m.MakeGenericType (types|>List.toArray)

let havingUnitType = [typeof<float>;typeof<XYZ>;typeof<double>;typeof<UV>] 

let fsFuncType = typedefof<int->int>
let csFuncType = typedefof<Func<int,int>>
let kvPairType = typedefof<KeyValuePair<obj,obj>>
let lst = List<string> ()
let mapInfo = <@Seq.map (fun c->c) (seq{"1"}) @> |> genericMethodInfo
let toListInfo = <@ lst.ToList ()@> |> genericMethodInfo
let toDictionaryInfo = <@lst.ToDictionary
                          (Func<string,string>(fun s->s),Func<string,string>(fun s->s))@> |> genericMethodInfo

let keyInfo (t:Type) = t.GetProperty("Key")

let valueInfo (t:Type) = t.GetProperty("Value")


//type resolver
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
    |Init of Type

let pipeline ctor predicate = function 
    |Init(t) when predicate(t) -> t|> ctor 
    | some -> some

let getProps entity = entity.entityType.GetProperties(BindingFlags.Public|||BindingFlags.Instance)
                       |> List.ofArray 
                       |> List.filter (fun p -> isNull (p.GetCustomAttribute<ExcludeAttribute>()))
                       |> List.filter (fun p -> p.CanRead && p.CanWrite)

let fetchAttribute<'attr when 'attr:>Attribute and 'attr:null> handler (mi:MemberInfo) =
    match mi.GetCustomAttribute<'attr>() with
    |null -> ignore ()
    |attr -> attr|> handler 

let getGenerecDef (t:Type) = t.GetGenericArguments()

let entityCtor  _ (t:Type) = 
    let schema =  t.GetCustomAttribute<SchemaAttribute>()
    match schema with
    | null -> Init(t)
    | attr -> Entity({entityType=t;guid=attr.Guid;name = attr.Name})

let simpleCtor _ t = Simple(t)

let isEntity = pipeline (entityCtor (fun t-> t)) (fun t-> true)
let isValue = pipeline  (isEntity |> simpleCtor) (fun t->simple.Contains t)
let isSimple = isValue >> isEntity

let tailHandler ctor = function
    | Simple(t) -> t |> ValueType |>ctor
    | Entity(def) -> def|> EntityType |> ctor 
    | _ -> raise (new MapperException("Input type not defined"))

let mapCtor cont t =
    let gTypes = getGenerecDef t
    let key = gTypes.[0]
    let value = gTypes.[1]
    match availableKeys.Contains key with
    |false -> raise( new MapperException ("Unallowed dictionary key {0}", [key]))
    |true -> value|> Init |> cont |> tailHandler (fun e->Map(key,e)) 

let arrCtor cont t = 
    let gType = (getGenerecDef t).[0]
    gType|> Init |> cont |> tailHandler (fun e->Array(e)) 

let isMap = pipeline (mapCtor isSimple) (fun t-> t.IsInterface && t.GetGenericTypeDefinition() = dict)
let isArray = pipeline (arrCtor isSimple) (fun t-> t.IsInterface && t.GetGenericTypeDefinition() = list)

let getType = isMap >> isArray >> isValue >> isEntity
