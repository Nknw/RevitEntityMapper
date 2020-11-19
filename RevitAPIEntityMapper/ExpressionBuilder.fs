module ExpressionBuilder
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
open System.Collections
open FSharp.Linq.RuntimeHelpers

type ExpressionContext = {
    lambdaExpr: Expr -> Expr
    bindings: Expr list
    output : Expr
    input: Expr
    defaultUT : Option<DisplayUnitType>
    }

let makeGenType types (m:Type) = m.MakeGenericType (types|>List.toArray)

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
let toListInfo = <@ lst.ToList ()@> |>genericMethodInfo

let havingUnitType = [typeof<float>;typeof<XYZ>;typeof<double>;typeof<UV>] 

let readMeta (info:MemberInfo) =
    match info.GetCustomAttribute<UnitAttribute>() with
        |null -> Option.None
        |attr -> Some(attr.DisplayType)

let exprInit (factories:Dictionary<Type,obj>) entity = 
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
                      NeedsCreate(ctx, getProps entity)

let finallize ctx = 
    let bindings = ctx.bindings |> List.reduce (fun p n -> Expr.Sequential(p,n))
    let lambda = Expr.Sequential(bindings,ctx.output) |> ctx.lambdaExpr
    lambda |> LeafExpressionConverter.EvaluateQuotation |> Success
    

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
        |ValueType(t) -> t|> simpleHandler 
        |EntityType(def) -> entityHandler def

let arrayEntityHandler response entities factory def = 
    () 
     |> (Call mapInfo >> MakeGen [typeofEntity; def.entityType] >> With [factory;entities] ) 
     |> (fun mapExpr -> Call toListInfo >> MakeGen [def.entityType] >> With [mapExpr] <| ()) 
     |> response

let mapEntityHandler response entities factory def =
    entities |> response
    
    

let expressionBuilder visitor exprCtx (eType,info) = 
    let createNewCtx expr = {exprCtx with bindings = expr :: exprCtx.bindings}  |> Success
    let set body = SetProp info >> On exprCtx.output >> To body <| ()
    let fetchEntity = callBuilder exprCtx.input info exprCtx.defaultUT
    let response t = t |> set |> createNewCtx
    let fetchList t = t|> genList |> fetchEntity  
    let fetchDict t = t|> genDict |> fetchEntity
    let fetchFactory cont def = 
        visitor def |> continueSuccess (fun f -> castFactory def.entityType<| f |> cont)
    match eType with
        |Simple(t) -> t |> fetchEntity |>response

        |Entity(def) -> let includedEnt =  fetchEntity typeofEntity
                        def |> fetchFactory
                         (fun f-> Call pipeRInfo >> MakeGen [typeofEntity;def.entityType]
                                   >> With [includedEnt; f ] <| () |> response)

        |Array(t) -> handleIncludedType t (fun tp-> tp |> fetchList |> response )
                      (fun def -> def |> fetchFactory 
                                   (fun f-> (typeofEntity |> fetchList, f, def) 
                                              |||> arrayEntityHandler response))

        |Map(key,value) -> handleIncludedType value (fun t -> [key;t] |> fetchDict |> response)
                            (fun def -> def |> fetchFactory 
                                         (fun f-> ([key;typeofEntity] |> fetchDict, f ,def)
                                                   |||> mapEntityHandler response ))

