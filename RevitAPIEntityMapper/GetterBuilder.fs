module GetterBuilder
open TypeResolver
open Visitor
open ExprHelpers
open ExpressionVisitor
open System
open System.Collections.Generic
open FSharp.Quotations
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB

let miGet = 
    let e = Entity ()
    <@ e.Get "" @> |> genericMethodInfo

let miutGet =
    let e = Entity()
    <@ e.Get ("", DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo
   
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
                    defaultUT = readUnit entity.entityType
                  }
                  NeedsCreate(ctx)

let response ctx body = 
    let state = ctx.stepState
    SetProp (ctx.info) >> On (state.output) >> To body <| () |> createNewCtx state

let getterBody = 
    let fetch ctx t = 
        fetchUnitType
         (fun optUT-> 
            match optUT with
            |Some(ut) -> Call miutGet >> MakeGen [t] >> On ctx.stepState.input >> With [Val ctx.info.Name;Val ut] <| ()
            |None -> Call miGet >> MakeGen [t] >> On ctx.stepState.input >> WithConst [ctx.info.Name] <| ())
         ctx
    exprBody fetch response (fun def -> (typeofEntity,def.entityType))

let getterBuilder factories = 
    visitorBuilder (getterInit factories) getterBody (fun ctx ->let factory =  finallize ctx
                                                                factories.Add(ctx.output.Type,factory)
                                                                factory |> Success) |> higthLevelVisitorBuilder
