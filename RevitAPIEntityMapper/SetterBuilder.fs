module SetterBuilder
open Abstractions
open ExprHelpers
open System.Collections.Generic
open System
open Autodesk.Revit.DB.ExtensibleStorage
open Autodesk.Revit.DB
open FSharp.Quotations

let setterInit (factories:Dictionary<Type,obj>) def =
    match factories.TryGetValue def.entityType with
    |(true,factory) -> factory |> Success |> Complited
    |(false,_) -> let t = typeof<Entity>
                  let constructor = [typeof<Guid>] |> List.toArray |> t.GetConstructor
                  let obj = Var ("obj", def.entityType)
                  let ent = Var ("e",typeofEntity)
                  let newObj = (constructor,[Val def.guid]) |> Expr.NewObject
                  let lmd body = Expr.Lambda(obj,Expr.Let(ent,newObj,body)) 
                  let ctx = { 
                    input = Expr.Var obj
                    bindings = []
                    lambdaExpr = lmd
                    output = Expr.Var ent
                    defaultUT = readUnit def.entityType
                  }
                  NeedsCreate(ctx)

let miSet = 
    let e = Entity ()
    <@ e.Set("",obj()) @> |> genericMethodInfo

let miutSet = 
    let e = Entity ()
    <@ e.Set("",obj(),DisplayUnitType.DUT_1_RATIO) @> |> genericMethodInfo

let fetch ctx _ =
    Expr.PropertyGet(ctx.stepState.input ,ctx.info)

let response ctx (body:Expr) = 
    fetchUnitType 
     (fun optUT -> 
        match optUT with
        |Some(ut) -> Call miutSet >> MakeGen[body.Type] >> With [Val ctx.info.Name; body; Val ut] <| () 
                     |> createNewCtx ctx.stepState
        |Option.None -> Call miSet >> MakeGen[body.Type] >> With [Val ctx.info.Name; body] <| ()
                        |> createNewCtx ctx.stepState)
     ctx

let setterBody = exprBody fetch response (fun def -> (def.entityType,typeofEntity)) 

let setterBuilder factories = visitorBuilder (setterInit factories) setterBody (finallize factories)
