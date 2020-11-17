module ExpressionBuilder
open System
open Abstractions
open Autodesk.Revit.Mapper
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open FSharp.Quotations
open FSharp.Quotations.Evaluator
open System.Reflection
open Autodesk.Revit.DB
open System.Linq

type ExpressionContext = {
    letExpr: Expr -> Expr
    bindings: Expr list
    obj: Expr
    ent: Var
    defaultUT : Option<DisplayUnitType>
    }

let getInfo () = 
    let e = Entity ()
    <@ e.Get "" @> |> genericMethodInfo

let getutInfo () =
    let e = Entity()
    <@ e.Get ("", DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo

let factoryInfo factory =
    let e = Entity ()
    <@ factory e @> |> genericMethodInfo


let lst = List<string> ()
let miGet = getInfo()
let miutGet = getutInfo()
let mapInfo = <@Seq.map (fun c->c) (seq{"1"}) @> |> genericMethodInfo
let toListInfo = <@ lst.ToList ()@> |>genericMethodInfo

let gen = mapInfo.MakeGenericMethod ([typeof<int>]|>List.toArray)
let havingUnitType = [typeof<float>;typeof<XYZ>;typeof<double>;typeof<UV>] 

let readMeta (info:MemberInfo) =
    match info.GetCustomAttribute<UnitAttibute>() with
        |null -> Option.None
        |attr -> Some(attr.DisplayType)

let makemiGeneric t = miGet.MakeGenericMethod (t|> List.toArray)

let listquote (name:Expr<string>) = <@fun (e:Entity)-> let ents = e.Get<IList<Entity>> %name 
                                                       ents@>

let exprInit (exprs:Dictionary<Type,Entity->obj>) entity = 
    match exprs.TryGetValue entity.entityType with
        |(true,factory) -> factory |> Success |> Complited
        |(false,_) -> let t = entity.entityType
                      let constructor = [] |> List.toArray |> t.GetConstructor
                      let letExpr var bind body = Expr.Let (var,bind,body)
                      let var = Var ("obj", t)
                      let bind = (constructor,[]) |> Expr.NewObject
                      let ctx = { 
                        obj = Expr.Var var
                        bindings = []
                        letExpr = letExpr var bind
                        ent = Var ("e",typeofEntity)
                        defaultUT = readMeta entity.entityType
                      }
                      NeedsCreate(ctx, getProps entity)

let finallize ctx = 
    let bindings = ctx.bindings |> List.reduce (fun p n -> Expr.Sequential(p,n))
    let cast = Expr.Coerce(ctx.obj, typeof<obj>)
    let body = Expr.Sequential(bindings,cast) |> ctx.letExpr
    Expr.Lambda(ctx.ent, body).CompileUntyped() :?> Entity -> obj
    

let callBuilder e (info:PropertyInfo) defaultUT t = 
    let tp = List.toArray [t]
    let object = Expr.Var e
    let callWithDP dp = Expr.Call(object,miutGet.MakeGenericMethod tp,[Expr.Value info.Name;Expr.Value dp])
    match List.contains info.PropertyType havingUnitType with
        |false -> Expr.Call(object,miGet.MakeGenericMethod tp,[Expr.Value info.Name])
        |true -> match info|>readMeta with
                    |Some(dp) -> callWithDP dp
                    |Option.None -> match defaultUT with
                                        | Some(dp) -> callWithDP dp
                                        | Option.None -> failwith ""
   


let expressionBuilder visitor exprCtx (eType,info) = 
    let createNewCtx expr = {exprCtx with bindings = expr :: exprCtx.bindings}  |> Success
    let set body = Expr.PropertySet (exprCtx.obj,info,body)
    let fetchEntity = callBuilder exprCtx.ent info exprCtx.defaultUT
    let response t = t|> fetchEntity |> set |> createNewCtx
    let handleIncludedType t simpleHandler entityHandler = 
        match t with
            |ValueType(t) -> t|> simpleHandler |> response
            |EntityType(def) -> entityHandler def
    match eType with
        |Simple(t) -> response t
        |Entity(def) -> let includedEnt =  fetchEntity typeofEntity
                        let factory = visitor def |> factoryInfo
                        Expr.Call(factory,[includedEnt])|> set |> createNewCtx
        |Array(t) -> handleIncludedType t (fun tp->list.MakeGenericType([tp]|>List.toArray)) 
                        (fun def -> let listEntities = fetchEntity (list.MakeGenericType ([typeofEntity]|>List.toArray))
                                    let factory = visitor def |> Expr.Value 
                                    let entities = Expr.Call(mapInfo,[factory;listEntities])
                                    Expr.Call(entities,toListInfo,[]) |> set |> createNewCtx)
        |Map(key,value) -> Success(exprCtx)//handleIncludedType value (fun t-> dict.MakeGenericType([key,t]|>List.toArray))
                               // (fun def -> let dictEntities = fetchEntity (dict.MakeGenericType([key,typeofEntity]|>List.toArray))
                               //             let factory = visitor def |> Expr.Value
                               //             Success<|exprCtx)

