module GetterBuilder
open System
open Abstractions
open ExprHelpers
open Autodesk.Revit.Mapper
open System.Collections.Generic
open Autodesk.Revit.DB.ExtensibleStorage
open FSharp.Quotations
open System.Reflection
open Autodesk.Revit.DB
open System.Linq

let getInfo () = 
    let e = Entity ()
    <@ e.Get "" @> |> genericMethodInfo

let getutInfo () =
    let e = Entity()
    <@ e.Get ("", DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo


let castFactory t f =
    let factoryType = fsFuncType |> makeGenType [typeofEntity;t]
    Expr.Coerce(Val f,factoryType)
   
let lst = List<string> ()
let miGet = getInfo()
let miutGet = getutInfo()
let mapInfo = <@Seq.map (fun c->c) (seq{"1"}) @> |> genericMethodInfo
let toListInfo = <@ lst.ToList ()@> |> genericMethodInfo
let toDictionaryInfo = <@lst.ToDictionary
                          (Func<string,string>(fun s->s),Func<string,string>(fun s->s))@> |> genericMethodInfo

let havingUnitType = [typeof<float>;typeof<XYZ>;typeof<double>;typeof<UV>] 

let readMeta (info:MemberInfo) =
    match info.GetCustomAttribute<UnitAttribute>() with
    |null -> Option.None
    |attr -> Some(attr.DisplayType)

let getterInit (factories:Dictionary<Type,obj>) entity = 
    match factories.TryGetValue entity.entityType with
    |(true,factory) -> factory |> Success |> Complited
    |(false,_) -> let t = entity.entityType
                  let constructor = [] |> List.toArray |> t.GetConstructor
                  let obj = Var ("obj", t)
                  let ent = Var ("e",typeofEntity)
                  let newObj = (constructor,[]) |> Expr.NewObject
                  let lmd body = Expr.Lambda(ent,Expr.Let(obj,newObj,body)) 
                  let ctx = { 
                    input = Expr.Var ent
                    bindings = []
                    lambdaExpr = lmd
                    output = Expr.Var obj
                    defaultUT = readMeta entity.entityType
                  }
                  NeedsCreate(ctx)
    
let callBuilder e (info:PropertyInfo) defaultUT t = 
    let callWithDP dp = Call miutGet >> MakeGen [t] >> On e >> With [Val info.Name;Val dp] <| ()
    match List.contains info.PropertyType havingUnitType with
    |false -> Call miGet >> MakeGen [t] >> On e >> WithConst [info.Name] <| ()
    |true -> match info |> readMeta with
             |Some(dp) -> callWithDP dp
             |Option.None -> match defaultUT with
                             | Some(dp) -> callWithDP dp
                             | Option.None -> failwith ""
   

let handleIncludedType t simpleHandler entityHandler = 
    match t with
    |ValueType(t) -> t |> simpleHandler 
    |EntityType(def) -> def |> entityHandler

let arrayEntityHandler entities factory def = 
    () 
     |>( Call mapInfo >> MakeGen [typeofEntity; def.entityType] >> With [factory;entities]) 
     |> (fun mapExpr -> Call toListInfo >> MakeGen [def.entityType] >> With [mapExpr] <| ()) 

let mapEntityHandler pairs factory (def,key) = 
    let pair = makeGenType [key;typeofEntity] kvPairType
    let input = Var("i",pair)
    let keySelector = Expr.NewDelegate(makeGenType [pair;key] csFuncType,[input],Expr.PropertyGet(Expr.Var input , keyInfo pair)) 
    let valueSelector = 
        Expr.NewDelegate(makeGenType [pair;def.entityType] csFuncType,
                            [input], 
                            Expr.Application(factory,Expr.PropertyGet(Expr.Var input,valueInfo pair)))
    Call toDictionaryInfo >> MakeGen [pair;key;def.entityType] >> With [pairs;keySelector;valueSelector] <| ()

let createNewCtx exprCtx expr = {exprCtx with bindings = expr :: exprCtx.bindings}  |> Success
let response ctx body = 
    let state =ctx.stepState
    SetProp (ctx.info) >> On (state.output) >> To body <| () |> createNewCtx state

let getterBody ctx = 
    let fetchEntity = callBuilder ctx.stepState.input ctx.info ctx.stepState.defaultUT
    let fetchList t = t |> genList |> fetchEntity  
    let fetchDict t = t |> genDict |> fetchEntity
    let continueFromFactory cont def = 
        ctx.visitor def |> continueSuccess (fun f -> castFactory def.entityType<| f |> cont)
    match ctx.eType with
    |Simple(t) -> t |> fetchEntity |> response ctx

    |Entity(def) -> let includedEnt =  fetchEntity typeofEntity
                    def |> continueFromFactory
                     (fun f-> Expr.Application(f,includedEnt) |> response ctx)

    |Array(t) -> handleIncludedType t (fun tp-> tp |> fetchList |> response ctx)
                  (fun def -> def |> continueFromFactory 
                               (fun f-> (typeofEntity |> fetchList, f, def) 
                                          |||> arrayEntityHandler |> response ctx))

    |Map(key,value) -> handleIncludedType value (fun t -> [key;t] |> fetchDict |> response ctx)
                        (fun def -> def |> continueFromFactory 
                                     (fun f-> ([key;typeofEntity] |> fetchDict, f ,(def,key))
                                               |||> mapEntityHandler |> response ctx))

let getterBuilder funcs = visitorBuilder (getterInit funcs) getterBody (finallize funcs) |> higthLevelVisitorBuilder

